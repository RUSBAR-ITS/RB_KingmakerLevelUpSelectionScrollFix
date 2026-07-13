using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticValueNormalizer
    {
        private const int MaxDepth = 12;
        private const int MaxCollectionItems = 64;

        private static readonly HashSet<string> s_IgnoredFields =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "PrototypeLink",
                "m_AssetGuid",
                "m_Icon",
                "Icon",
                "FxOnStart",
                "FxOnRemove",
                "PrefabLink",
                "SourcePath"
            };

        private readonly SemanticGraphIndex m_Graph;
        private readonly SemanticEnumResolver m_Enums;

        internal SemanticValueNormalizer(
            SemanticGraphIndex graph,
            SemanticEnumResolver enums)
        {
            m_Graph = graph;
            m_Enums = enums;
        }

        internal JToken Normalize(JToken value)
        {
            return Normalize(value, 0, new HashSet<string>(StringComparer.Ordinal));
        }

        internal JObject NormalizeFields(JObject mechanicalObject)
        {
            JObject result = new JObject();
            JArray fields = mechanicalObject == null ? null : mechanicalObject["Fields"] as JArray;
            if (fields == null)
            {
                return result;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                JObject field = fields[i] as JObject;
                string name = field == null ? string.Empty : (string)field["Name"];
                if (string.IsNullOrEmpty(name) || s_IgnoredFields.Contains(name))
                {
                    continue;
                }

                JToken normalized = Normalize(field["Value"]);
                if (IsEmpty(normalized))
                {
                    continue;
                }

                AddWithoutLosingDuplicates(result, name, normalized);
            }

            return result;
        }

        internal static string ReadDisplay(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            JObject obj = value as JObject;
            if (obj != null)
            {
                return (string)obj["Display"]
                    ?? (string)obj["Name"]
                    ?? (string)obj["Raw"]
                    ?? (string)obj["InternalName"]
                    ?? string.Empty;
            }

            JValue primitive = value as JValue;
            return primitive == null || primitive.Value == null
                ? string.Empty
                : Convert.ToString(primitive.Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        internal string ResolveBlueprintName(JObject value, out string nameSource)
        {
            string internalName = value == null
                ? string.Empty
                : (string)value["InternalName"] ?? string.Empty;
            string name;
            if (SemanticLocalization.TryReference(internalName, out name))
            {
                nameSource = "ModLocalization";
                return name;
            }

            name = value == null ? string.Empty : (string)value["ResolvedName"];
            nameSource = "GameLocalization";
            if (string.IsNullOrEmpty(name)
                && m_Graph != null
                && m_Graph.TryResolveRelatedLocalizedName(value, out name))
            {
                nameSource = "RelatedGameLocalization";
            }

            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return SemanticReferenceNameBuilder.BuildFallback(
                internalName,
                value == null ? string.Empty : (string)value["ShortType"],
                out nameSource);
        }

        private JToken Normalize(
            JToken value,
            int depth,
            HashSet<string> visitedObjects)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                return JValue.CreateNull();
            }

            if (depth >= MaxDepth)
            {
                return new JObject { ["Truncated"] = true };
            }

            JValue primitive = value as JValue;
            if (primitive != null)
            {
                return primitive.DeepClone();
            }

            JArray array = value as JArray;
            if (array != null)
            {
                return NormalizeArray(array, depth, visitedObjects);
            }

            JObject obj = value as JObject;
            if (obj == null)
            {
                return value.DeepClone();
            }

            obj = m_Graph.ResolveObjectReference(obj);
            string id = (string)obj["$id"];
            if (!string.IsNullOrEmpty(id) && !visitedObjects.Add(id))
            {
                return new JObject { ["Reference"] = id };
            }

            string kind = (string)obj["Kind"] ?? string.Empty;
            switch (kind)
            {
                case "Enum":
                    return NormalizeEnum(obj);
                case "LocalizedString":
                    return new JObject
                    {
                        ["Display"] = (string)obj["Resolved"] ?? string.Empty,
                        ["LocalizationKey"] = (string)obj["Key"] ?? string.Empty
                    };
                case "BlueprintReference":
                    return NormalizeBlueprintReference(obj);
                case "UnityObjectTerminal":
                    return new JObject
                    {
                        ["Type"] = (string)obj["ShortType"] ?? string.Empty,
                        ["Name"] = (string)obj["Name"] ?? string.Empty
                    };
                case "Collection":
                    return NormalizeCollection(obj, depth, visitedObjects);
                case "Object":
                    return IsDisplayValueObject(obj)
                        ? NormalizeValueObject(obj, depth, visitedObjects)
                        : NormalizeObject(obj, depth, visitedObjects);
                case "Element":
                case "BlueprintComponent":
                    return NormalizeObject(obj, depth, visitedObjects);
                case "ValueObject":
                    return NormalizeValueObject(obj, depth, visitedObjects);
                default:
                    return NormalizeGenericObject(obj, depth, visitedObjects);
            }
        }

        private static JObject NormalizeEnum(JObject value)
        {
            string type = (string)value["Type"] ?? string.Empty;
            string raw = (string)value["Raw"] ?? string.Empty;
            string existing = (string)value["Localized"];
            if (string.IsNullOrEmpty(existing))
            {
                existing = (string)value["Display"] ?? string.Empty;
            }

            return new JObject
            {
                ["Raw"] = raw,
                ["Numeric"] = value["Numeric"] == null
                    ? JValue.CreateNull()
                    : value["Numeric"].DeepClone(),
                ["Display"] = SemanticLocalization.EnumValue(type, raw, existing)
            };
        }

        private JObject NormalizeBlueprintReference(JObject value)
        {
            string internalName = (string)value["InternalName"] ?? string.Empty;
            string blueprintType = (string)value["ShortType"] ?? string.Empty;
            string nameSource;
            string name = ResolveBlueprintName(value, out nameSource);

            return new JObject
            {
                ["Guid"] = (string)value["Guid"] ?? string.Empty,
                ["Type"] = blueprintType,
                ["InternalName"] = internalName,
                ["Name"] = name,
                ["NameSource"] = nameSource,
                ["DescriptionPreview"] = (string)value["ResolvedDescriptionPreview"] ?? string.Empty
            };
        }

        private JToken NormalizeCollection(
            JObject value,
            int depth,
            HashSet<string> visitedObjects)
        {
            JArray items = value["Items"] as JArray;
            JObject result = new JObject
            {
                ["Count"] = (int?)value["Count"] ?? (items == null ? 0 : items.Count),
                ["Items"] = items == null
                    ? new JArray()
                    : NormalizeArray(items, depth, visitedObjects)
            };

            if (items != null && items.Count > MaxCollectionItems)
            {
                result["OmittedCount"] = items.Count - MaxCollectionItems;
            }

            return result;
        }

        private JArray NormalizeArray(
            JArray values,
            int depth,
            HashSet<string> visitedObjects)
        {
            JArray result = new JArray();
            int count = Math.Min(values.Count, MaxCollectionItems);
            for (int i = 0; i < count; i++)
            {
                result.Add(Normalize(
                    values[i],
                    depth + 1,
                    new HashSet<string>(visitedObjects, StringComparer.Ordinal)));
            }

            return result;
        }

        private JToken NormalizeObject(
            JObject value,
            int depth,
            HashSet<string> visitedObjects)
        {
            JObject fields = new JObject();
            JArray sourceFields = value["Fields"] as JArray;
            if (sourceFields != null)
            {
                for (int i = 0; i < sourceFields.Count; i++)
                {
                    JObject field = sourceFields[i] as JObject;
                    string name = field == null ? string.Empty : (string)field["Name"];
                    if (string.IsNullOrEmpty(name) || s_IgnoredFields.Contains(name))
                    {
                        continue;
                    }

                    JToken normalized = Normalize(
                        field["Value"],
                        depth + 1,
                        new HashSet<string>(visitedObjects, StringComparer.Ordinal));
                    if (!IsEmpty(normalized))
                    {
                        AddWithoutLosingDuplicates(fields, name, normalized);
                    }
                }
            }

            JObject result = new JObject();
            string shortType = (string)value["ShortType"];
            if (string.IsNullOrEmpty(shortType))
            {
                shortType = GetShortType((string)value["Type"]);
            }

            if (!string.IsNullOrEmpty(shortType))
            {
                result["Type"] = shortType;
            }

            result["Fields"] = fields;
            return result;
        }

        private JToken NormalizeValueObject(
            JObject value,
            int depth,
            HashSet<string> visitedObjects)
        {
            string type = (string)value["Type"] ?? string.Empty;
            JObject normalized = NormalizeObject(value, depth, visitedObjects) as JObject
                ?? new JObject();
            JObject fields = normalized["Fields"] as JObject;

            if (type.EndsWith("SpellDescriptorWrapper", StringComparison.Ordinal)
                && fields != null
                && fields["m_IntValue"] != null)
            {
                long numeric = (long?)fields["m_IntValue"] ?? 0L;
                normalized["RawNumeric"] = numeric;
                normalized["Display"] = m_Enums == null
                    ? numeric.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : m_Enums.Resolve(
                        "Kingmaker.Blueprints.Classes.Spells.SpellDescriptor",
                        numeric);
            }
            else
            {
                string display = SemanticValueDisplayBuilder.Build(type, fields);
                if (!string.IsNullOrEmpty(display))
                {
                    normalized["Display"] = display;
                }
            }

            return normalized;
        }

        private static string GetShortType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return string.Empty;
            }

            int nested = type.LastIndexOf('+');
            int dotted = type.LastIndexOf('.');
            int separator = Math.Max(nested, dotted);
            return separator < 0 ? type : type.Substring(separator + 1);
        }

        private static bool IsDisplayValueObject(JObject value)
        {
            string type = (string)value["Type"] ?? string.Empty;
            return type.EndsWith("ContextValue", StringComparison.Ordinal)
                || type.EndsWith("ContextDiceValue", StringComparison.Ordinal)
                || type.EndsWith("ContextDurationValue", StringComparison.Ordinal)
                || type.EndsWith("DiceFormula", StringComparison.Ordinal)
                || type.EndsWith(".Feet", StringComparison.Ordinal);
        }

        private JToken NormalizeGenericObject(
            JObject value,
            int depth,
            HashSet<string> visitedObjects)
        {
            JObject result = new JObject();
            foreach (JProperty property in value.Properties())
            {
                if (property.Name == "$id"
                    || property.Name == "$ref"
                    || property.Name == "SourcePath"
                    || property.Name == "Properties")
                {
                    continue;
                }

                JToken normalized = Normalize(
                    property.Value,
                    depth + 1,
                    new HashSet<string>(visitedObjects, StringComparer.Ordinal));
                if (!IsEmpty(normalized))
                {
                    result[property.Name] = normalized;
                }
            }

            return result;
        }

        private static void AddWithoutLosingDuplicates(
            JObject target,
            string name,
            JToken value)
        {
            JToken current = target[name];
            if (current == null)
            {
                target[name] = value;
                return;
            }

            JArray duplicates = current as JArray;
            if (duplicates == null || duplicates.Annotation<DuplicateFieldMarker>() == null)
            {
                duplicates = new JArray(current, value);
                duplicates.AddAnnotation(new DuplicateFieldMarker());
                target[name] = duplicates;
                return;
            }

            duplicates.Add(value);
        }

        private static bool IsEmpty(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                return true;
            }

            JValue primitive = value as JValue;
            return primitive != null
                && primitive.Type == JTokenType.String
                && string.IsNullOrEmpty((string)primitive);
        }

        private sealed class DuplicateFieldMarker
        {
        }
    }
}
