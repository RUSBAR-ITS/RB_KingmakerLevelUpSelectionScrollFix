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
            int humanizedParameterReferenceCount = 0;
            int humanizedSourceNameCount = 0;
            int effectGroupCount = 0;
            int repeatedEffectGroupCount = 0;
            int collapsedEffectCount = 0;
            int maximumRepeatCount = 0;
            string maximumRepeatItemName = string.Empty;
            string maximumRepeatSummary = string.Empty;
            int summaryWithFallbackTextCount = 0;
            int maxEffectCount = 0;
            string maxEffectItemGuid = string.Empty;
            string maxEffectItemName = string.Empty;
            JArray itemsWithoutEffects = new JArray();
            JArray fallbackNames = new JArray();
            JArray itemsWithFallbackSummaries = new JArray();
            Dictionary<string, HumanizedReferenceInfo> humanizedReferences =
                new Dictionary<string, HumanizedReferenceInfo>(StringComparer.Ordinal);
            Dictionary<string, UnlocalizedComponentInfo> unlocalizedComponents =
                new Dictionary<string, UnlocalizedComponentInfo>(StringComparer.Ordinal);

            for (int i = 0; i < items.Count; i++)
            {
                JObject item = items[i] as JObject;
                if (item == null)
                {
                    continue;
                }

                JArray effects = item["Effects"] as JArray ?? new JArray();
                JArray effectGroups = item["EffectGroups"] as JArray ?? new JArray();
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

                bool itemHasFallbackSummary = false;
                effectGroupCount += effectGroups.Count;
                for (int groupIndex = 0; groupIndex < effectGroups.Count; groupIndex++)
                {
                    JObject group = effectGroups[groupIndex] as JObject;
                    int count = group == null ? 0 : (int?)group["Count"] ?? 0;
                    if (count <= 1)
                    {
                        continue;
                    }

                    repeatedEffectGroupCount++;
                    collapsedEffectCount += count - 1;
                    if (count > maximumRepeatCount)
                    {
                        maximumRepeatCount = count;
                        maximumRepeatItemName = (string)item["Name"] ?? string.Empty;
                        maximumRepeatSummary = (string)group["Summary"] ?? string.Empty;
                    }
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

                    string summary = (string)effect["Summary"] ?? string.Empty;
                    if (ContainsLatinWord(summary))
                    {
                        summaryWithFallbackTextCount++;
                        itemHasFallbackSummary = true;
                    }

                    string componentDisplay = (string)effect["ComponentDisplay"]
                        ?? string.Empty;
                    if (ContainsLatinWord(componentDisplay))
                    {
                        AddUnlocalizedComponent(
                            unlocalizedComponents,
                            effect,
                            (string)item["Name"] ?? string.Empty);
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
                        effect["Parameters"],
                        ref gameLocalizedReferenceCount,
                        ref relatedGameLocalizedReferenceCount,
                        ref modLocalizedReferenceCount,
                        ref humanizedReferenceNameCount,
                        ref humanizedParameterReferenceCount,
                        humanizedReferences,
                        (string)item["Name"] ?? string.Empty);

                    CountReferenceNameSources(
                        effect["Source"],
                        ref gameLocalizedReferenceCount,
                        ref relatedGameLocalizedReferenceCount,
                        ref modLocalizedReferenceCount,
                        ref humanizedReferenceNameCount,
                        ref humanizedSourceNameCount,
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

                if (itemHasFallbackSummary)
                {
                    itemsWithFallbackSummaries.Add(BuildItemReference(item));
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
                ["HumanizedParameterReferenceCount"] = humanizedParameterReferenceCount,
                ["HumanizedSourceNameCount"] = humanizedSourceNameCount,
                ["EffectGroupCount"] = effectGroupCount,
                ["RepeatedEffectGroupCount"] = repeatedEffectGroupCount,
                ["CollapsedEffectCount"] = collapsedEffectCount,
                ["MaximumRepeatCount"] = maximumRepeatCount,
                ["MaximumRepeatItemName"] = maximumRepeatItemName,
                ["MaximumRepeatSummary"] = maximumRepeatSummary,
                ["SummaryWithFallbackTextCount"] = summaryWithFallbackTextCount,
                ["MaximumEffectCountOnItem"] = maxEffectCount,
                ["MaximumEffectItemGuid"] = maxEffectItemGuid,
                ["MaximumEffectItemName"] = maxEffectItemName,
                ["ItemsWithoutEffects"] = itemsWithoutEffects,
                ["ItemsUsingFallbackNames"] = fallbackNames,
                ["ItemsWithFallbackTextSummaries"] = itemsWithFallbackSummaries,
                ["HumanizedReferences"] = BuildHumanizedReferences(humanizedReferences),
                ["UnlocalizedComponentTypes"] = BuildUnlocalizedComponents(
                    unlocalizedComponents)
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
            ref int areaHumanizedNames,
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
                    areaHumanizedNames++;
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
                        ref areaHumanizedNames,
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
                    ref areaHumanizedNames,
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

        private static void AddUnlocalizedComponent(
            Dictionary<string, UnlocalizedComponentInfo> components,
            JObject effect,
            string itemName)
        {
            string type = (string)effect["ComponentType"] ?? string.Empty;
            UnlocalizedComponentInfo info;
            if (!components.TryGetValue(type, out info))
            {
                info = new UnlocalizedComponentInfo
                {
                    ComponentType = type,
                    DisplayName = (string)effect["ComponentDisplay"] ?? string.Empty
                };
                components.Add(type, info);
            }

            info.Count++;
            if (!string.IsNullOrEmpty(itemName)
                && info.SampleItems.Count < 3
                && !info.SampleItems.Contains(itemName))
            {
                info.SampleItems.Add(itemName);
            }
        }

        private static JArray BuildUnlocalizedComponents(
            Dictionary<string, UnlocalizedComponentInfo> components)
        {
            List<UnlocalizedComponentInfo> values =
                new List<UnlocalizedComponentInfo>(components.Values);
            values.Sort(delegate(
                UnlocalizedComponentInfo left,
                UnlocalizedComponentInfo right)
            {
                int count = right.Count.CompareTo(left.Count);
                return count != 0
                    ? count
                    : string.Compare(
                        left.ComponentType,
                        right.ComponentType,
                        StringComparison.Ordinal);
            });

            JArray result = new JArray();
            for (int i = 0; i < values.Count; i++)
            {
                result.Add(new JObject
                {
                    ["Count"] = values[i].Count,
                    ["ComponentType"] = values[i].ComponentType,
                    ["DisplayName"] = values[i].DisplayName,
                    ["SampleItems"] = new JArray(values[i].SampleItems)
                });
            }

            return result;
        }

        private static bool ContainsLatinWord(string value)
        {
            int run = 0;
            for (int i = 0; i < (value ?? string.Empty).Length; i++)
            {
                char c = value[i];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    run++;
                }
                else
                {
                    if (run > 0 && !IsRomanNumeralRun(value, i - run, run))
                    {
                        if (run >= 3)
                        {
                            return true;
                        }
                    }

                    run = 0;
                }
            }

            return run >= 3 && !IsRomanNumeralRun(value, value.Length - run, run);
        }

        private static bool IsRomanNumeralRun(string value, int start, int length)
        {
            for (int i = start; i < start + length; i++)
            {
                char c = char.ToUpperInvariant(value[i]);
                if (c != 'I'
                    && c != 'V'
                    && c != 'X'
                    && c != 'L'
                    && c != 'C'
                    && c != 'D'
                    && c != 'M')
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class HumanizedReferenceInfo
        {
            internal int Count;
            internal string Type;
            internal string InternalName;
            internal string DisplayName;
            internal readonly List<string> SampleItems = new List<string>();
        }

        private sealed class UnlocalizedComponentInfo
        {
            internal int Count;
            internal string ComponentType;
            internal string DisplayName;
            internal readonly List<string> SampleItems = new List<string>();
        }
    }
}
