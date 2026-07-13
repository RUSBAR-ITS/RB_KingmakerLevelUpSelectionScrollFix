using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class AccessorySemanticReportBuilder
    {
        private const int SchemaVersion = 3;

        internal static JObject Build(JObject sourceReport)
        {
            if (sourceReport == null)
            {
                throw new ArgumentNullException("sourceReport");
            }

            JArray entities = sourceReport["Entities"] as JArray ?? new JArray();
            JArray uniqueItems = sourceReport["UniqueItems"] as JArray ?? new JArray();
            JArray blueprintGraph = sourceReport["BlueprintGraph"] as JArray ?? new JArray();

            SemanticGraphIndex graph = new SemanticGraphIndex(blueprintGraph);
            SemanticEnumResolver enums = new SemanticEnumResolver(
                sourceReport["EnumIndex"] as JArray);
            SemanticReportCoverage coverage = new SemanticReportCoverage();
            AccessorySemanticExtractor extractor =
                new AccessorySemanticExtractor(graph, enums, coverage);
            JArray items = new JArray();

            for (int i = 0; i < uniqueItems.Count; i++)
            {
                JObject uniqueItem = uniqueItems[i] as JObject;
                if (uniqueItem == null)
                {
                    continue;
                }

                JObject entity = FindFirstEntity(uniqueItem, entities);
                items.Add(extractor.Extract(uniqueItem, entity));
                coverage.ItemCount++;
            }

            JObject sourceMetadata = sourceReport["Metadata"] as JObject;
            return new JObject
            {
                ["Metadata"] = new JObject
                {
                    ["SchemaVersion"] = SchemaVersion,
                    ["ModVersion"] = Main.ModVersion,
                    ["SourceSchemaVersion"] = sourceMetadata == null
                        ? JValue.CreateNull()
                        : CloneOrNull(sourceMetadata["SchemaVersion"]),
                    ["GeneratedUtc"] = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                    ["SourceGeneratedUtc"] = sourceMetadata == null
                        ? string.Empty
                        : (string)sourceMetadata["GeneratedUtc"] ?? string.Empty,
                    ["GameLocale"] = sourceMetadata == null
                        ? string.Empty
                        : (string)sourceMetadata["Locale"] ?? string.Empty,
                    ["Scope"] = "Accessories",
                    ["Purpose"] = "Compact parameter-based accessory mechanics report. Descriptions are retained for verification and are not used to infer effects.",
                    ["ScopeDefinitions"] = new JObject
                    {
                        ["Direct"] = "A component attached directly to the item or one of its enchantments, plus the item's directly exposed ability.",
                        ["Granted"] = "A mechanic inside a feature or fact directly granted by the item.",
                        ["Nested"] = "An implementation detail reached through an ability, buff, variant, or a deeper reference chain."
                    }
                },
                ["Statistics"] = coverage.ToJson(),
                ["Quality"] = SemanticReportQuality.Build(items),
                ["Items"] = items
            };
        }

        private static JObject FindFirstEntity(JObject uniqueItem, JArray entities)
        {
            JArray indexes = uniqueItem["EntityIndexes"] as JArray;
            if (indexes != null && indexes.Count > 0)
            {
                int index = (int?)indexes[0] ?? -1;
                if (index >= 0 && index < entities.Count)
                {
                    return entities[index] as JObject;
                }
            }

            string guid = (string)uniqueItem["BlueprintGuid"] ?? string.Empty;
            for (int i = 0; i < entities.Count; i++)
            {
                JObject entity = entities[i] as JObject;
                if (entity != null
                    && string.Equals(
                        (string)entity["BlueprintGuid"] ?? string.Empty,
                        guid,
                        StringComparison.Ordinal))
                {
                    return entity;
                }
            }

            return null;
        }

        private static JToken CloneOrNull(JToken value)
        {
            return value == null ? JValue.CreateNull() : value.DeepClone();
        }
    }
}
