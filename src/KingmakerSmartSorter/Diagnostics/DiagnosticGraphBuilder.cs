using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Localization;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        private const int EmergencyMaxDepth = 256;
        private const int EmergencyMaxNodes = 250000;
        private const int EmergencyMaxCollectionItems = 100000;
        private const int EmergencyMaxErrors = 5000;

        private static readonly Dictionary<Type, FieldInfo[]> s_FieldCache =
            new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, PropertyInfo[]> s_PropertyCache =
            new Dictionary<Type, PropertyInfo[]>();

        private readonly GameLocalizationResolver m_Localization;
        private readonly Dictionary<string, JObject> m_BlueprintNodes =
            new Dictionary<string, JObject>(StringComparer.Ordinal);
        private readonly Dictionary<object, string> m_BlueprintIds =
            new Dictionary<object, string>(ReferenceIdentityComparer.Instance);
        private readonly Dictionary<object, string> m_ObjectIds =
            new Dictionary<object, string>(ReferenceIdentityComparer.Instance);
        private readonly HashSet<string> m_BlueprintsBeingBuilt =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, LocalizationAccumulator> m_LocalizationEntries =
            new Dictionary<string, LocalizationAccumulator>(StringComparer.Ordinal);
        private readonly List<JObject> m_Errors = new List<JObject>();

        private int m_NextAnonymousBlueprintId;
        private int m_NextObjectId;
        private int m_TotalNodeCount;
        private int m_SuppressedErrorCount;

        internal DiagnosticGraphBuilder(GameLocalizationResolver localization)
        {
            m_Localization = localization ?? new GameLocalizationResolver();
        }

        internal int BlueprintCount
        {
            get { return m_BlueprintNodes.Count; }
        }

        internal int TotalNodeCount
        {
            get { return m_TotalNodeCount; }
        }

        internal int ErrorCount
        {
            get { return m_Errors.Count; }
        }

        internal int SuppressedErrorCount
        {
            get { return m_SuppressedErrorCount; }
        }

        internal JToken ReferenceBlueprint(BlueprintScriptableObject blueprint, string path)
        {
            if (blueprint == null)
            {
                return JValue.CreateNull();
            }

            string id = GetBlueprintId(blueprint);
            string shallowReason;
            bool expand = ShouldExpandBlueprint(blueprint, path, out shallowReason);
            if (expand)
            {
                EnsureBlueprintNode(blueprint, id, path);
            }

            TrackBlueprintReference(blueprint.GetType(), expand, shallowReason);
            return CreateBlueprintReference(
                blueprint,
                id,
                expand,
                shallowReason);
        }

        internal JObject CreateLocalizedValue(
            string resolved,
            LocalizedString source,
            string path)
        {
            string key = source == null ? string.Empty : source.Key ?? string.Empty;
            string sourceResolved = GameLocalizationResolver.Resolve(source);
            string effectiveResolved = !string.IsNullOrEmpty(resolved)
                ? resolved
                : sourceResolved;
            string status = !string.IsNullOrEmpty(effectiveResolved)
                ? "Resolved"
                : !string.IsNullOrEmpty(key) ? "Unresolved" : "MissingKey";

            if (!string.IsNullOrEmpty(key))
            {
                RegisterLocalization(key, effectiveResolved, path);
            }

            TrackLocalizedValue(status, key);

            return new JObject
            {
                ["Raw"] = resolved ?? string.Empty,
                ["Localized"] = effectiveResolved,
                ["LocalizationKey"] = key,
                ["ResolutionSource"] = source == null
                    ? "ComputedGameProperty"
                    : "LocalizedString",
                ["ResolutionStatus"] = status
            };
        }

        internal LocalizedString FindLocalizedString(object owner, params string[] fieldNames)
        {
            if (owner == null || fieldNames == null)
            {
                return null;
            }

            Type type = owner.GetType();
            FieldInfo[] fields = GetFields(type);
            for (int i = 0; i < fieldNames.Length; i++)
            {
                for (int j = 0; j < fields.Length; j++)
                {
                    if (!string.Equals(
                        fields[j].Name,
                        fieldNames[i],
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        return fields[j].GetValue(owner) as LocalizedString;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        internal JArray BuildBlueprintGraph()
        {
            List<string> ids = new List<string>(m_BlueprintNodes.Keys);
            ids.Sort(StringComparer.Ordinal);
            JArray result = new JArray();
            for (int i = 0; i < ids.Count; i++)
            {
                result.Add(m_BlueprintNodes[ids[i]]);
            }

            return result;
        }

        internal JArray BuildLocalizationIndex()
        {
            List<string> keys = new List<string>(m_LocalizationEntries.Keys);
            keys.Sort(StringComparer.Ordinal);
            JArray result = new JArray();
            for (int i = 0; i < keys.Count; i++)
            {
                LocalizationAccumulator entry = m_LocalizationEntries[keys[i]];
                List<string> paths = new List<string>(entry.Paths);
                paths.Sort(StringComparer.Ordinal);
                result.Add(new JObject
                {
                    ["Key"] = keys[i],
                    ["Resolved"] = entry.Resolved,
                    ["Paths"] = new JArray(paths)
                });
            }

            return result;
        }

        internal JArray BuildErrors()
        {
            return new JArray(m_Errors);
        }

        internal void RecordError(string path, string operation, Exception exception)
        {
            AddError(
                path,
                operation,
                exception == null ? string.Empty : exception.GetType().FullName,
                exception == null ? string.Empty : exception.Message);
        }

        private void EnsureBlueprintNode(
            BlueprintScriptableObject blueprint,
            string id,
            string path)
        {
            if (m_BlueprintNodes.ContainsKey(id)
                || m_BlueprintsBeingBuilt.Contains(id))
            {
                return;
            }

            if (!TryReserveNode(path, "blueprint"))
            {
                return;
            }

            m_BlueprintsBeingBuilt.Add(id);
            JObject node = new JObject
            {
                ["$id"] = id,
                ["Kind"] = "Blueprint",
                ["Type"] = blueprint.GetType().FullName,
                ["ShortType"] = blueprint.GetType().Name,
                ["Guid"] = SafeBlueprintGuid(blueprint),
                ["InternalName"] = SafeUnityName(blueprint),
                ["ResolvedName"] = ReadStringProperty(blueprint, "Name"),
                ["ResolvedDescription"] = ReadStringProperty(blueprint, "Description"),
                ["FirstSourcePath"] = CompactDiagnosticPath(path)
            };
            m_BlueprintNodes[id] = node;

            try
            {
                node["Fields"] = SerializeFields(blueprint, path + "/fields", 1);
                node["Properties"] = SerializeProperties(
                    blueprint,
                    path + "/properties",
                    1);
            }
            catch (Exception ex)
            {
                RecordError(path, "BuildBlueprintNode", ex);
                node["Fields"] = new JArray();
            }
            finally
            {
                m_BlueprintsBeingBuilt.Remove(id);
            }
        }

        private string GetBlueprintId(BlueprintScriptableObject blueprint)
        {
            string existing;
            if (m_BlueprintIds.TryGetValue(blueprint, out existing))
            {
                return existing;
            }

            string guid = SafeBlueprintGuid(blueprint);
            string id = !string.IsNullOrEmpty(guid)
                ? "blueprint:" + guid
                : "blueprint-anonymous:"
                    + (++m_NextAnonymousBlueprintId).ToString("D6");
            m_BlueprintIds.Add(blueprint, id);
            return id;
        }

        private void RegisterLocalization(string key, string resolved, string path)
        {
            LocalizationAccumulator entry;
            if (!m_LocalizationEntries.TryGetValue(key, out entry))
            {
                entry = new LocalizationAccumulator(resolved);
                m_LocalizationEntries.Add(key, entry);
            }
            else if (string.IsNullOrEmpty(entry.Resolved) && !string.IsNullOrEmpty(resolved))
            {
                entry.Resolved = resolved;
            }

            if (!string.IsNullOrEmpty(path))
            {
                entry.Paths.Add(CompactDiagnosticPath(path));
            }
        }

        private bool TryReserveNode(string path, string kind)
        {
            if (m_TotalNodeCount >= EmergencyMaxNodes)
            {
                TrackTruncation("EmergencyNodeLimit");
                AddError(
                    path,
                    "EmergencyNodeLimit",
                    kind,
                    "Maximum node count " + EmergencyMaxNodes + " was reached.");
                return false;
            }

            m_TotalNodeCount++;
            return true;
        }

        private void AddError(
            string path,
            string operation,
            string errorType,
            string message)
        {
            if (m_Errors.Count >= EmergencyMaxErrors)
            {
                m_SuppressedErrorCount++;
                return;
            }

            m_Errors.Add(new JObject
            {
                ["Path"] = CompactDiagnosticPath(path),
                ["Operation"] = operation ?? string.Empty,
                ["ErrorType"] = errorType ?? string.Empty,
                ["Message"] = message ?? string.Empty
            });
        }

        private static JObject CreateReference(string id)
        {
            return new JObject { ["$ref"] = id };
        }

        private static JObject CreateBlueprintReference(
            BlueprintScriptableObject blueprint,
            string id,
            bool expanded,
            string shallowReason)
        {
            JObject result = new JObject
            {
                ["Kind"] = "BlueprintReference",
                ["NodeId"] = id,
                ["Expansion"] = expanded ? "Deep" : "Shallow",
                ["ShallowReason"] = expanded ? string.Empty : shallowReason ?? string.Empty,
                ["Type"] = blueprint.GetType().FullName,
                ["ShortType"] = blueprint.GetType().Name,
                ["Guid"] = SafeBlueprintGuid(blueprint),
                ["InternalName"] = SafeUnityName(blueprint),
                ["ResolvedName"] = ReadStringProperty(blueprint, "Name"),
                ["ResolvedDescriptionPreview"] = CreateTextPreview(
                    ReadStringProperty(blueprint, "Description"),
                    240)
            };
            if (expanded)
            {
                result["$ref"] = id;
            }

            return result;
        }

        private static string SafeBlueprintGuid(BlueprintScriptableObject blueprint)
        {
            try
            {
                return blueprint == null ? string.Empty : blueprint.AssetGuid ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeUnityName(UnityEngine.Object value)
        {
            try
            {
                return value == null ? string.Empty : value.name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ReadStringProperty(object owner, string propertyName)
        {
            if (owner == null)
            {
                return string.Empty;
            }

            try
            {
                PropertyInfo property = owner.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);
                return property == null || property.GetIndexParameters().Length != 0
                    ? string.Empty
                    : property.GetValue(owner, null) as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string CompactDiagnosticPath(string path)
        {
            const int maxLength = 2048;
            const int prefixLength = 768;
            const int suffixLength = 1024;
            string value = path ?? string.Empty;
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, prefixLength)
                + "/...<"
                + (value.Length - prefixLength - suffixLength)
                + " chars omitted>.../"
                + value.Substring(value.Length - suffixLength);
        }

        private static string CreateTextPreview(string value, int maxLength)
        {
            string text = value ?? string.Empty;
            if (maxLength < 1 || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        private sealed class LocalizationAccumulator
        {
            internal LocalizationAccumulator(string resolved)
            {
                Resolved = resolved ?? string.Empty;
                Paths = new HashSet<string>(StringComparer.Ordinal);
            }

            internal string Resolved { get; set; }

            internal HashSet<string> Paths { get; private set; }
        }
    }
}
