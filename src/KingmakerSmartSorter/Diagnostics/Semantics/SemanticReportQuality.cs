using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticReportQuality
    {
        internal static JObject Build(JArray items)
        {
            int itemWithoutEffectsCount = 0;
            int fallbackItemNameCount = 0;
            int missingDescriptionCount = 0;
            int genericSummaryCount = 0;
            int emptyParameterEffectCount = 0;
            int presenceBasedEffectCount = 0;
            int inactiveComponentCount = 0;
            int missingSourceCount = 0;
            int gameLocalizedReferenceCount = 0;
            int relatedGameLocalizedReferenceCount = 0;
            int modLocalizedReferenceCount = 0;
            int humanizedReferenceNameCount = 0;
            int maxEffectCount = 0;
            string maxEffectItemGuid = string.Empty;
            string maxEffectItemName = string.Empty;
            JArray itemsWithoutEffects = new JArray();
            JArray fallbackNames = new JArray();
            Dictionary<string, HumanizedReferenceInfo> humanizedReferences =
                new Dictionary<string, HumanizedReferenceInfo>(StringComparer.Ordinal);

            for (int i = 0; i < items.Count; i++)
            {
                JObject item = items[i] as JObject;
                if (item == null)
                {
                    continue;
                }

                JArray effects = item["Effects"] as JArray ?? new JArray();
                int activeEffectCount = (int?)item["ActiveEffectCount"] ?? effects.Count;
                if (activeEffectCount == 0)
                {
                    itemWithoutEffectsCount++;
                    itemsWithoutEffects.Add(BuildItemReference(item));
                }

                if ((string)item["NameSource"] != "GameLocalization")
                {
                    fallbackItemNameCount++;
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
                    bool isInactive = (bool?)effect["IsInactive"] == true;
                    if (isPresenceBased)
                    {
                        presenceBasedEffectCount++;
                    }

                    if (isInactive)
                    {
                        inactiveComponentCount++;
                    }

                    if (!isPresenceBased
                        && !isInactive
                        && (parameters == null || parameters.Count == 0))
                    {
                        emptyParameterEffectCount++;
                    }

                    CountReferenceNameSources(
                        effect,
                        ref gameLocalizedReferenceCount,
                        ref relatedGameLocalizedReferenceCount,
                        ref modLocalizedReferenceCount,
                        ref humanizedReferenceNameCount,
                        humanizedReferences,
                        (string)item["Name"] ?? string.Empty);

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
                ["FallbackItemNameCount"] = fallbackItemNameCount,
                ["MissingDescriptionCount"] = missingDescriptionCount,
                ["GenericSummaryCount"] = genericSummaryCount,
                ["EmptyParameterEffectCount"] = emptyParameterEffectCount,
                ["PresenceBasedEffectCount"] = presenceBasedEffectCount,
                ["InactiveComponentCount"] = inactiveComponentCount,
                ["MissingSourceCount"] = missingSourceCount,
                ["GameLocalizedReferenceCount"] = gameLocalizedReferenceCount,
                ["RelatedGameLocalizedReferenceCount"] = relatedGameLocalizedReferenceCount,
                ["ModLocalizedReferenceCount"] = modLocalizedReferenceCount,
                ["HumanizedReferenceNameCount"] = humanizedReferenceNameCount,
                ["MaximumEffectCountOnItem"] = maxEffectCount,
                ["MaximumEffectItemGuid"] = maxEffectItemGuid,
                ["MaximumEffectItemName"] = maxEffectItemName,
                ["ItemsWithoutEffects"] = itemsWithoutEffects,
                ["ItemsUsingFallbackNames"] = fallbackNames,
                ["HumanizedReferences"] = BuildHumanizedReferences(humanizedReferences)
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

        private static void CountReferenceNameSources(
            JToken value,
            ref int gameLocalizations,
            ref int relatedGameLocalizations,
            ref int modLocalizations,
            ref int humanizedNames,
            Dictionary<string, HumanizedReferenceInfo> humanizedReferences,
            string itemName)
        {
            if (value == null)
            {
                return;
            }

            JObject obj = value as JObject;
            if (obj != null)
            {
                string source = (string)obj["NameSource"] ?? string.Empty;
                if (source == "GameLocalization")
                {
                    gameLocalizations++;
                }
                else if (source == "RelatedGameLocalization")
                {
                    relatedGameLocalizations++;
                }
                else if (source == "ModLocalization")
                {
                    modLocalizations++;
                }
                else if (source == "HumanizedInternalName")
                {
                    humanizedNames++;
                    AddHumanizedReference(humanizedReferences, obj, itemName);
                }

                foreach (JProperty property in obj.Properties())
                {
                    CountReferenceNameSources(
                        property.Value,
                        ref gameLocalizations,
                        ref relatedGameLocalizations,
                        ref modLocalizations,
                        ref humanizedNames,
                        humanizedReferences,
                        itemName);
                }

                return;
            }

            JArray array = value as JArray;
            if (array == null)
            {
                return;
            }

            for (int i = 0; i < array.Count; i++)
            {
                CountReferenceNameSources(
                    array[i],
                    ref gameLocalizations,
                    ref relatedGameLocalizations,
                    ref modLocalizations,
                    ref humanizedNames,
                    humanizedReferences,
                    itemName);
            }
        }

        private static void AddHumanizedReference(
            Dictionary<string, HumanizedReferenceInfo> references,
            JObject value,
            string itemName)
        {
            string internalName = (string)value["InternalName"] ?? string.Empty;
            string type = (string)value["Type"]
                ?? (string)value["BlueprintType"]
                ?? string.Empty;
            string key = type + "|" + internalName;
            HumanizedReferenceInfo info;
            if (!references.TryGetValue(key, out info))
            {
                info = new HumanizedReferenceInfo
                {
                    Type = type,
                    InternalName = internalName,
                    DisplayName = (string)value["Name"] ?? string.Empty
                };
                references.Add(key, info);
            }

            info.Count++;
            if (!string.IsNullOrEmpty(itemName)
                && info.SampleItems.Count < 3
                && !info.SampleItems.Contains(itemName))
            {
                info.SampleItems.Add(itemName);
            }
        }

        private static JArray BuildHumanizedReferences(
            Dictionary<string, HumanizedReferenceInfo> references)
        {
            List<HumanizedReferenceInfo> values =
                new List<HumanizedReferenceInfo>(references.Values);
            values.Sort(delegate(
                HumanizedReferenceInfo left,
                HumanizedReferenceInfo right)
            {
                int count = right.Count.CompareTo(left.Count);
                return count != 0
                    ? count
                    : string.Compare(
                        left.InternalName,
                        right.InternalName,
                        StringComparison.Ordinal);
            });

            JArray result = new JArray();
            for (int i = 0; i < values.Count; i++)
            {
                result.Add(new JObject
                {
                    ["Count"] = values[i].Count,
                    ["Type"] = values[i].Type,
                    ["InternalName"] = values[i].InternalName,
                    ["DisplayName"] = values[i].DisplayName,
                    ["SampleItems"] = new JArray(values[i].SampleItems)
                });
            }

            return result;
        }

        private sealed class HumanizedReferenceInfo
        {
            internal int Count;
            internal string Type;
            internal string InternalName;
            internal string DisplayName;
            internal readonly List<string> SampleItems = new List<string>();
        }
    }
}
