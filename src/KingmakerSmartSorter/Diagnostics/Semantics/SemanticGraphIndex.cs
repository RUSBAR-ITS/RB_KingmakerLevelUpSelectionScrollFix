using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticGraphIndex
    {
        private readonly Dictionary<string, JObject> m_Objects =
            new Dictionary<string, JObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, JObject> m_BlueprintsByGuid =
            new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> m_LocalizedNamesByGuid =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> m_AmbiguousNameGuids =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> m_LocalizedNamesByAlias =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> m_AmbiguousNameAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal SemanticGraphIndex(JArray blueprintGraph)
        {
            IndexToken(blueprintGraph);
        }

        internal JObject ResolveBlueprint(JObject reference)
        {
            if (reference == null)
            {
                return null;
            }

            string id = (string)reference["$ref"] ?? (string)reference["NodeId"];
            JObject result;
            return !string.IsNullOrEmpty(id) && m_Objects.TryGetValue(id, out result)
                ? result
                : null;
        }

        internal List<JObject> CollectMechanicalObjects(JObject blueprint)
        {
            List<JObject> result = new List<JObject>();
            HashSet<string> visitedObjects = new HashSet<string>(StringComparer.Ordinal);
            CollectMechanicalObjects(blueprint, result, visitedObjects);
            return result;
        }

        internal List<SemanticBlueprintLink> CollectBlueprintLinks(JObject value)
        {
            List<SemanticBlueprintLink> result = new List<SemanticBlueprintLink>();
            HashSet<string> visitedObjects = new HashSet<string>(StringComparer.Ordinal);
            CollectBlueprintLinks(value, string.Empty, result, visitedObjects);
            return result;
        }

        internal List<SemanticBlueprintLink> CollectDirectBlueprintLinks(JObject blueprint)
        {
            List<SemanticBlueprintLink> result = new List<SemanticBlueprintLink>();
            JArray fields = blueprint == null ? null : blueprint["Fields"] as JArray;
            if (fields == null)
            {
                return result;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                JObject field = fields[i] as JObject;
                string name = field == null ? string.Empty : (string)field["Name"];
                if (string.IsNullOrEmpty(name) || name == "Components")
                {
                    continue;
                }

                CollectBlueprintLinks(
                    field["Value"],
                    name,
                    result,
                    new HashSet<string>(StringComparer.Ordinal));
            }

            return result;
        }

        internal JObject ResolveObjectReference(JObject value)
        {
            if (value == null || value.Count != 1 || value["$ref"] == null)
            {
                return value;
            }

            JObject resolved;
            string id = (string)value["$ref"];
            if (!string.IsNullOrEmpty(id)
                && id.StartsWith("blueprint:", StringComparison.Ordinal))
            {
                return value;
            }

            return !string.IsNullOrEmpty(id) && m_Objects.TryGetValue(id, out resolved)
                ? resolved
                : value;
        }

        internal bool TryResolveRelatedLocalizedName(
            JObject reference,
            out string localizedName)
        {
            localizedName = string.Empty;
            if (reference == null)
            {
                return false;
            }

            string guid = (string)reference["Guid"] ?? string.Empty;
            if (!string.IsNullOrEmpty(guid)
                && !m_AmbiguousNameGuids.Contains(guid)
                && m_LocalizedNamesByGuid.TryGetValue(guid, out localizedName))
            {
                return true;
            }

            JObject blueprint;
            if (!string.IsNullOrEmpty(guid)
                && m_BlueprintsByGuid.TryGetValue(guid, out blueprint))
            {
                localizedName = (string)blueprint["ResolvedName"] ?? string.Empty;
                if (!string.IsNullOrEmpty(localizedName))
                {
                    return true;
                }
            }

            List<string> aliases = SemanticReferenceNameBuilder.BuildAliases(
                (string)reference["InternalName"] ?? string.Empty);
            List<string> groups = GetPreferredNameGroups(
                (string)reference["ShortType"] ?? string.Empty);
            for (int i = 0; i < aliases.Count; i++)
            {
                string alias = aliases[i];
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    if (TryGetLocalizedAlias(
                        BuildGroupedAlias(groups[groupIndex], alias),
                        out localizedName))
                    {
                        return true;
                    }
                }

                if (TryGetLocalizedAlias(alias, out localizedName))
                {
                    return true;
                }
            }

            localizedName = string.Empty;
            return false;
        }

        private void IndexToken(JToken token)
        {
            if (token == null)
            {
                return;
            }

            JObject obj = token as JObject;
            if (obj != null)
            {
                string id = (string)obj["$id"];
                if (!string.IsNullOrEmpty(id) && !m_Objects.ContainsKey(id))
                {
                    m_Objects.Add(id, obj);
                }

                if ((string)obj["Kind"] == "Blueprint")
                {
                    IndexBlueprintName(obj);
                }
                else if ((string)obj["Kind"] == "BlueprintReference")
                {
                    IndexReferenceName(obj);
                }

                foreach (JProperty property in obj.Properties())
                {
                    IndexToken(property.Value);
                }

                return;
            }

            JArray array = token as JArray;
            if (array == null)
            {
                return;
            }

            for (int i = 0; i < array.Count; i++)
            {
                IndexToken(array[i]);
            }
        }

        private void IndexBlueprintName(JObject blueprint)
        {
            string guid = (string)blueprint["Guid"] ?? string.Empty;
            if (!string.IsNullOrEmpty(guid) && !m_BlueprintsByGuid.ContainsKey(guid))
            {
                m_BlueprintsByGuid.Add(guid, blueprint);
            }

            string localizedName = (string)blueprint["ResolvedName"] ?? string.Empty;
            IndexLocalizedName(
                guid,
                (string)blueprint["InternalName"] ?? string.Empty,
                (string)blueprint["ShortType"] ?? string.Empty,
                localizedName);
        }

        private void IndexReferenceName(JObject reference)
        {
            IndexLocalizedName(
                (string)reference["Guid"] ?? string.Empty,
                (string)reference["InternalName"] ?? string.Empty,
                (string)reference["ShortType"] ?? string.Empty,
                (string)reference["ResolvedName"] ?? string.Empty);
        }

        private void IndexLocalizedName(
            string guid,
            string internalName,
            string blueprintType,
            string localizedName)
        {
            if (string.IsNullOrEmpty(localizedName))
            {
                return;
            }

            AddLocalizedGuid(guid, localizedName);
            List<string> aliases = SemanticReferenceNameBuilder.BuildAliases(internalName);
            string group = GetNameGroup(blueprintType);
            for (int i = 0; i < aliases.Count; i++)
            {
                AddLocalizedAlias(BuildGroupedAlias(group, aliases[i]), localizedName);
                AddLocalizedAlias(aliases[i], localizedName);
            }
        }

        private void AddLocalizedGuid(string guid, string localizedName)
        {
            if (string.IsNullOrEmpty(guid)
                || string.IsNullOrEmpty(localizedName)
                || m_AmbiguousNameGuids.Contains(guid))
            {
                return;
            }

            string existing;
            if (!m_LocalizedNamesByGuid.TryGetValue(guid, out existing))
            {
                m_LocalizedNamesByGuid.Add(guid, localizedName);
                return;
            }

            if (!string.Equals(existing, localizedName, StringComparison.Ordinal))
            {
                m_LocalizedNamesByGuid.Remove(guid);
                m_AmbiguousNameGuids.Add(guid);
            }
        }

        private bool TryGetLocalizedAlias(string alias, out string localizedName)
        {
            localizedName = string.Empty;
            return !string.IsNullOrEmpty(alias)
                && !m_AmbiguousNameAliases.Contains(alias)
                && m_LocalizedNamesByAlias.TryGetValue(alias, out localizedName);
        }

        private static string BuildGroupedAlias(string group, string alias)
        {
            return string.IsNullOrEmpty(group) || string.IsNullOrEmpty(alias)
                ? string.Empty
                : group + "|" + alias;
        }

        private static List<string> GetPreferredNameGroups(string blueprintType)
        {
            string ownGroup = GetNameGroup(blueprintType);
            List<string> result = new List<string> { ownGroup };
            if (ownGroup == "Mechanic")
            {
                result.Add("Item");
            }

            return result;
        }

        private static string GetNameGroup(string blueprintType)
        {
            string type = blueprintType ?? string.Empty;
            if (type.IndexOf("Item", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Item";
            }

            if (type.IndexOf("Buff", StringComparison.OrdinalIgnoreCase) >= 0
                || type.IndexOf("Feature", StringComparison.OrdinalIgnoreCase) >= 0
                || type.IndexOf("Ability", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Mechanic";
            }

            if (type.IndexOf("Unit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Unit";
            }

            return "Other";
        }

        private void AddLocalizedAlias(string alias, string localizedName)
        {
            if (string.IsNullOrEmpty(alias)
                || string.IsNullOrEmpty(localizedName)
                || m_AmbiguousNameAliases.Contains(alias))
            {
                return;
            }

            string existing;
            if (!m_LocalizedNamesByAlias.TryGetValue(alias, out existing))
            {
                m_LocalizedNamesByAlias.Add(alias, localizedName);
                return;
            }

            if (!string.Equals(existing, localizedName, StringComparison.Ordinal))
            {
                m_LocalizedNamesByAlias.Remove(alias);
                m_AmbiguousNameAliases.Add(alias);
            }
        }

        private void CollectMechanicalObjects(
            JToken token,
            List<JObject> result,
            HashSet<string> visitedObjects)
        {
            if (token == null)
            {
                return;
            }

            JObject obj = token as JObject;
            if (obj != null)
            {
                obj = ResolveObjectReference(obj);
                string kind = (string)obj["Kind"] ?? string.Empty;
                if (kind == "BlueprintReference")
                {
                    return;
                }

                string id = (string)obj["$id"];
                if (!string.IsNullOrEmpty(id) && !visitedObjects.Add(id))
                {
                    return;
                }

                if (kind == "BlueprintComponent" || kind == "Element")
                {
                    result.Add(obj);
                }

                foreach (JProperty property in obj.Properties())
                {
                    if (property.Name == "$id" || property.Name == "$ref")
                    {
                        continue;
                    }

                    CollectMechanicalObjects(property.Value, result, visitedObjects);
                }

                return;
            }

            JArray array = token as JArray;
            if (array == null)
            {
                return;
            }

            for (int i = 0; i < array.Count; i++)
            {
                CollectMechanicalObjects(array[i], result, visitedObjects);
            }
        }

        private void CollectBlueprintLinks(
            JToken token,
            string fieldName,
            List<SemanticBlueprintLink> result,
            HashSet<string> visitedObjects)
        {
            if (token == null)
            {
                return;
            }

            JObject obj = token as JObject;
            if (obj != null)
            {
                string directReference = obj.Count == 1 ? (string)obj["$ref"] : null;
                if (!string.IsNullOrEmpty(directReference)
                    && directReference.StartsWith("blueprint:", StringComparison.Ordinal))
                {
                    result.Add(new SemanticBlueprintLink
                    {
                        FieldName = fieldName ?? string.Empty,
                        Reference = obj
                    });
                    return;
                }

                obj = ResolveObjectReference(obj);
                if ((string)obj["Kind"] == "BlueprintReference")
                {
                    result.Add(new SemanticBlueprintLink
                    {
                        FieldName = fieldName ?? string.Empty,
                        Reference = obj
                    });
                    return;
                }

                string id = (string)obj["$id"];
                if (!string.IsNullOrEmpty(id) && !visitedObjects.Add(id))
                {
                    return;
                }

                JArray fields = obj["Fields"] as JArray;
                if (fields != null)
                {
                    for (int i = 0; i < fields.Count; i++)
                    {
                        JObject field = fields[i] as JObject;
                        if (field == null)
                        {
                            continue;
                        }

                        CollectBlueprintLinks(
                            field["Value"],
                            (string)field["Name"] ?? fieldName,
                            result,
                            visitedObjects);
                    }
                }

                foreach (JProperty property in obj.Properties())
                {
                    if (property.Name == "Fields"
                        || property.Name == "$id"
                        || property.Name == "$ref")
                    {
                        continue;
                    }

                    CollectBlueprintLinks(
                        property.Value,
                        property.Name,
                        result,
                        visitedObjects);
                }

                return;
            }

            JArray array = token as JArray;
            if (array == null)
            {
                return;
            }

            for (int i = 0; i < array.Count; i++)
            {
                CollectBlueprintLinks(array[i], fieldName, result, visitedObjects);
            }
        }
    }
}
