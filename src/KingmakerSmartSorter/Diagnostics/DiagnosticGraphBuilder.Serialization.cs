using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Items;
using Kingmaker.Localization;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        internal JToken SerializeValue(object value, string path)
        {
            return SerializeValue(value, path, 0);
        }

        private JToken SerializeValue(object value, string path, int depth)
        {
            if (value == null)
            {
                return JValue.CreateNull();
            }

            if (depth > EmergencyMaxDepth)
            {
                TrackTruncation("EmergencyDepthLimit");
                AddError(
                    path,
                    "EmergencyDepthLimit",
                    value.GetType().FullName,
                    "Maximum depth " + EmergencyMaxDepth + " was reached.");
                return new JObject
                {
                    ["$truncated"] = "EmergencyDepthLimit",
                    ["Type"] = value.GetType().FullName
                };
            }

            Type type = value.GetType();
            if (IsScalar(type))
            {
                return CreateScalar(value, type);
            }

            Enum enumValue = value as Enum;
            if (enumValue != null)
            {
                return SerializeEnum(enumValue, path);
            }

            LocalizedString localizedString = value as LocalizedString;
            if (localizedString != null)
            {
                return SerializeLocalizedString(localizedString, path);
            }

            BlueprintScriptableObject blueprint = value as BlueprintScriptableObject;
            if (!ReferenceEquals(blueprint, null))
            {
                return ReferenceBlueprint(blueprint, path);
            }

            BlueprintComponent component = value as BlueprintComponent;
            if (!ReferenceEquals(component, null))
            {
                return SerializeBlueprintComponent(component, path, depth + 1);
            }

            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (!ReferenceEquals(unityObject, null))
            {
                return SerializeTerminalUnityObject(unityObject, path);
            }

            if (value is Type)
            {
                return new JObject
                {
                    ["Kind"] = "SystemType",
                    ["Name"] = ((Type)value).FullName
                };
            }

            if (value is Delegate)
            {
                return new JObject
                {
                    ["Kind"] = "Delegate",
                    ["Type"] = type.FullName,
                    ["Method"] = ((Delegate)value).Method.Name
                };
            }

            if (IsRuntimeExternal(type))
            {
                return new JObject
                {
                    ["Kind"] = "ExternalRuntimeObject",
                    ["Type"] = type.FullName,
                    ["Display"] = SafeToString(value)
                };
            }

            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                return SerializeDictionary(dictionary, path, depth + 1);
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null && !(value is string))
            {
                return SerializeEnumerable(enumerable, path, depth + 1);
            }

            return SerializeObject(value, path, depth + 1);
        }

        private JToken SerializeObject(object value, string path, int depth)
        {
            Type type = value.GetType();
            bool isReference = !type.IsValueType;
            string id = string.Empty;
            if (isReference)
            {
                if (m_ObjectIds.TryGetValue(value, out id))
                {
                    TrackObjectReference();
                    return CreateReference(id);
                }

                if (!TryReserveNode(path, "object"))
                {
                    return new JObject
                    {
                        ["$truncated"] = "EmergencyNodeLimit",
                        ["Type"] = type.FullName
                    };
                }

                id = "object:" + (++m_NextObjectId).ToString("D8");
                m_ObjectIds.Add(value, id);
            }

            JObject result = new JObject();
            if (isReference)
            {
                result["$id"] = id;
            }

            result["Kind"] = type.IsValueType ? "ValueObject" : "Object";
            result["Type"] = type.FullName;
            result["Fields"] = SerializeFields(value, path + "/fields", depth);
            return result;
        }

        private JArray SerializeFields(object owner, string path, int depth)
        {
            FieldInfo[] fields = GetFields(owner.GetType());
            JArray result = new JArray();
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                string fieldPath = path + "/" + field.DeclaringType.FullName + "." + field.Name;
                string ignoredReason;
                if (TryGetIgnoredFieldReason(field, out ignoredReason))
                {
                    TrackIgnoredField(field, ignoredReason);
                    continue;
                }

                JObject entry = new JObject
                {
                    ["Name"] = field.Name,
                    ["DeclaringType"] = field.DeclaringType.FullName,
                    ["ValueType"] = field.FieldType.FullName
                };
                try
                {
                    entry["Value"] = SerializeValue(
                        field.GetValue(owner),
                        fieldPath,
                        depth + 1);
                }
                catch (Exception ex)
                {
                    RecordError(fieldPath, "ReadField", ex);
                    entry["Value"] = new JObject
                    {
                        ["$error"] = ex.Message
                    };
                }

                result.Add(entry);
            }

            return result;
        }

        private JToken SerializeLocalizedString(LocalizedString value, string path)
        {
            string key = value.Key ?? string.Empty;
            string resolved = GameLocalizationResolver.Resolve(value);
            if (!string.IsNullOrEmpty(key))
            {
                RegisterLocalization(key, resolved, path);
            }

            string status = !string.IsNullOrEmpty(resolved)
                ? "Resolved"
                : !string.IsNullOrEmpty(key) ? "Unresolved" : "MissingKey";
            TrackLocalizedValue(status, key);

            return new JObject
            {
                ["Kind"] = "LocalizedString",
                ["Key"] = key,
                ["Resolved"] = resolved,
                ["ShouldProcess"] = value.ShouldProcess,
                ["ResolutionSource"] = "GameLocalization",
                ["ResolutionStatus"] = status
            };
        }

        private JToken SerializeDictionary(IDictionary dictionary, string path, int depth)
        {
            string id;
            JToken existingReference;
            if (!TryBeginReference(dictionary, path, "dictionary", out id, out existingReference))
            {
                return existingReference;
            }

            List<DictionaryEntry> entries = new List<DictionaryEntry>();
            try
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                RecordError(path, "EnumerateDictionary", ex);
            }

            entries.Sort(delegate(DictionaryEntry left, DictionaryEntry right)
            {
                return string.Compare(
                    SafeToString(left.Key),
                    SafeToString(right.Key),
                    StringComparison.Ordinal);
            });

            JArray values = new JArray();
            int count = Math.Min(entries.Count, EmergencyMaxCollectionItems);
            for (int i = 0; i < count; i++)
            {
                values.Add(new JObject
                {
                    ["Key"] = SerializeValue(entries[i].Key, path + "/key[" + i + "]", depth),
                    ["Value"] = SerializeValue(entries[i].Value, path + "/value[" + i + "]", depth)
                });
            }

            if (entries.Count > count)
            {
                TrackTruncation("EmergencyCollectionLimit");
                AddError(
                    path,
                    "EmergencyCollectionLimit",
                    dictionary.GetType().FullName,
                    "Dictionary contains " + entries.Count + " entries.");
            }

            return new JObject
            {
                ["$id"] = id,
                ["Kind"] = "Dictionary",
                ["Type"] = dictionary.GetType().FullName,
                ["Count"] = entries.Count,
                ["Entries"] = values
            };
        }

        private JToken SerializeEnumerable(IEnumerable enumerable, string path, int depth)
        {
            string id;
            JToken existingReference;
            if (!TryBeginReference(enumerable, path, "collection", out id, out existingReference))
            {
                return existingReference;
            }

            JArray values = new JArray();
            int index = 0;
            try
            {
                foreach (object entry in enumerable)
                {
                    if (index >= EmergencyMaxCollectionItems)
                    {
                        TrackTruncation("EmergencyCollectionLimit");
                        AddError(
                            path,
                            "EmergencyCollectionLimit",
                            enumerable.GetType().FullName,
                            "Collection exceeded " + EmergencyMaxCollectionItems + " entries.");
                        break;
                    }

                    values.Add(SerializeValue(entry, path + "/[" + index + "]", depth));
                    index++;
                }
            }
            catch (Exception ex)
            {
                RecordError(path, "EnumerateCollection", ex);
            }

            return new JObject
            {
                ["$id"] = id,
                ["Kind"] = "Collection",
                ["Type"] = enumerable.GetType().FullName,
                ["Count"] = index,
                ["Items"] = values
            };
        }

        private bool TryBeginReference(
            object value,
            string path,
            string kind,
            out string id,
            out JToken existingReference)
        {
            existingReference = null;
            if (m_ObjectIds.TryGetValue(value, out id))
            {
                TrackObjectReference();
                existingReference = CreateReference(id);
                return false;
            }

            if (!TryReserveNode(path, kind))
            {
                id = string.Empty;
                existingReference = new JObject
                {
                    ["$truncated"] = "EmergencyNodeLimit",
                    ["Type"] = value.GetType().FullName
                };
                return false;
            }

            id = "object:" + (++m_NextObjectId).ToString("D8");
            m_ObjectIds.Add(value, id);
            return true;
        }

        private static bool IsRuntimeExternal(Type type)
        {
            if (typeof(ItemEntity).IsAssignableFrom(type))
            {
                return true;
            }

            string fullName = type.FullName ?? string.Empty;
            return fullName.StartsWith("Kingmaker.EntitySystem.Entities.", StringComparison.Ordinal)
                || fullName.StartsWith("Kingmaker.Items.ItemsCollection", StringComparison.Ordinal)
                || fullName.StartsWith("Kingmaker.UnitLogic.UnitDescriptor", StringComparison.Ordinal)
                || fullName.StartsWith("Kingmaker.Game", StringComparison.Ordinal);
        }

        private static bool IsScalar(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid);
        }

        private static JToken CreateScalar(object value, Type type)
        {
            if (type == typeof(DateTime))
            {
                return new JValue(((DateTime)value).ToString("o", CultureInfo.InvariantCulture));
            }

            if (type == typeof(DateTimeOffset))
            {
                return new JValue(((DateTimeOffset)value).ToString("o", CultureInfo.InvariantCulture));
            }

            if (type == typeof(TimeSpan))
            {
                return new JValue(((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture));
            }

            if (type == typeof(Guid))
            {
                return new JValue(value.ToString());
            }

            return new JValue(value);
        }

        private static long GetEnumNumericValue(Enum value)
        {
            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static string SafeToString(object value)
        {
            try
            {
                return value == null ? string.Empty : value.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static FieldInfo[] GetFields(Type type)
        {
            FieldInfo[] cached;
            if (s_FieldCache.TryGetValue(type, out cached))
            {
                return cached;
            }

            List<FieldInfo> fields = new List<FieldInfo>();
            Type current = type;
            while (current != null && current != typeof(object))
            {
                FieldInfo[] declared = current.GetFields(
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);
                for (int i = 0; i < declared.Length; i++)
                {
                    if (!declared[i].IsStatic)
                    {
                        fields.Add(declared[i]);
                    }
                }

                current = current.BaseType;
            }

            fields.Sort(delegate(FieldInfo left, FieldInfo right)
            {
                int declaringType = string.Compare(
                    left.DeclaringType.FullName,
                    right.DeclaringType.FullName,
                    StringComparison.Ordinal);
                return declaringType != 0
                    ? declaringType
                    : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            });
            cached = fields.ToArray();
            s_FieldCache[type] = cached;
            return cached;
        }
    }
}
