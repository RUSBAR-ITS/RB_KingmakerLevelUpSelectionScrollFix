using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticGraphIndex
    {
        private readonly Dictionary<string, JObject> m_Objects =
            new Dictionary<string, JObject>(StringComparer.Ordinal);

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
