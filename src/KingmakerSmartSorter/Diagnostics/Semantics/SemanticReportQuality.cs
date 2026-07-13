using System;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticReportQuality
    {
        internal static JObject Build(JArray items)
        {
            int itemWithoutEffectsCount = 0;
            int internalNameFallbackCount = 0;
            int missingDescriptionCount = 0;
            int genericSummaryCount = 0;
            int emptyParameterEffectCount = 0;
            int presenceBasedEffectCount = 0;
            int missingSourceCount = 0;
            int technicalReferenceNameFallbackCount = 0;
            int maxEffectCount = 0;
            string maxEffectItemGuid = string.Empty;
            string maxEffectItemName = string.Empty;
            JArray itemsWithoutEffects = new JArray();
            JArray fallbackNames = new JArray();

            for (int i = 0; i < items.Count; i++)
            {
                JObject item = items[i] as JObject;
                if (item == null)
                {
                    continue;
                }

                JArray effects = item["Effects"] as JArray ?? new JArray();
                if (effects.Count == 0)
                {
                    itemWithoutEffectsCount++;
                    itemsWithoutEffects.Add(BuildItemReference(item));
                }

                if ((string)item["NameSource"] == "InternalNameFallback")
                {
                    internalNameFallbackCount++;
                    fallbackNames.Add(BuildItemReference(item));
                }

                if (string.IsNullOrEmpty((string)item["Description"]))
                {
                    missingDescriptionCount++;
                }

                if (effects.Count > maxEffectCount)
                {
                    maxEffectCount = effects.Count;
                    maxEffectItemGuid = (string)item["BlueprintGuid"] ?? string.Empty;
                    maxEffectItemName = (string)item["Name"] ?? string.Empty;
                }

                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    JObject effect = effects[effectIndex] as JObject;
                    if (effect == null)
                    {
                        continue;
                    }

                    if (string.Equals(
                        (string)effect["Summary"] ?? string.Empty,
                        (string)effect["CategoryDisplay"] ?? string.Empty,
                        StringComparison.Ordinal))
                    {
                        genericSummaryCount++;
                    }

                    JObject parameters = effect["Parameters"] as JObject;
                    bool isPresenceBased = (bool?)effect["IsPresenceBased"] == true;
                    if (isPresenceBased)
                    {
                        presenceBasedEffectCount++;
                    }

                    if (!isPresenceBased
                        && (parameters == null || parameters.Count == 0))
                    {
                        emptyParameterEffectCount++;
                    }

                    technicalReferenceNameFallbackCount += CountNameFallbacks(parameters);

                    JObject source = effect["Source"] as JObject;
                    if (source == null
                        || string.IsNullOrEmpty((string)source["BlueprintGuid"])
                        || string.IsNullOrEmpty((string)source["Relationship"]))
                    {
                        missingSourceCount++;
                    }
                }
            }

            return new JObject
            {
                ["ItemWithoutEffectsCount"] = itemWithoutEffectsCount,
                ["InternalNameFallbackCount"] = internalNameFallbackCount,
                ["MissingDescriptionCount"] = missingDescriptionCount,
                ["GenericSummaryCount"] = genericSummaryCount,
                ["EmptyParameterEffectCount"] = emptyParameterEffectCount,
                ["PresenceBasedEffectCount"] = presenceBasedEffectCount,
                ["MissingSourceCount"] = missingSourceCount,
                ["TechnicalReferenceNameFallbackCount"] = technicalReferenceNameFallbackCount,
                ["MaximumEffectCountOnItem"] = maxEffectCount,
                ["MaximumEffectItemGuid"] = maxEffectItemGuid,
                ["MaximumEffectItemName"] = maxEffectItemName,
                ["ItemsWithoutEffects"] = itemsWithoutEffects,
                ["ItemsUsingInternalNameFallback"] = fallbackNames
            };
        }

        private static JObject BuildItemReference(JObject item)
        {
            JObject slot = item["Slot"] as JObject;
            return new JObject
            {
                ["BlueprintGuid"] = (string)item["BlueprintGuid"] ?? string.Empty,
                ["InternalName"] = (string)item["InternalName"] ?? string.Empty,
                ["Name"] = (string)item["Name"] ?? string.Empty,
                ["Slot"] = slot == null ? string.Empty : (string)slot["Raw"] ?? string.Empty,
                ["Cost"] = item["Cost"] == null ? 0 : item["Cost"].DeepClone()
            };
        }

        private static int CountNameFallbacks(JToken value)
        {
            if (value == null)
            {
                return 0;
            }

            int result = 0;
            JObject obj = value as JObject;
            if (obj != null)
            {
                if ((string)obj["NameSource"] == "InternalNameFallback")
                {
                    result++;
                }

                foreach (JProperty property in obj.Properties())
                {
                    result += CountNameFallbacks(property.Value);
                }

                return result;
            }

            JArray array = value as JArray;
            if (array == null)
            {
                return 0;
            }

            for (int i = 0; i < array.Count; i++)
            {
                result += CountNameFallbacks(array[i]);
            }

            return result;
        }
    }
}
