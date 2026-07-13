using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Root;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.UI.Common;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerSmartSorter
{
    internal static class ItemDiagnosticExporter
    {
        private const string DiagnosticsDirectoryName = "Diagnostics";
        private const int SchemaVersion = 3;

        internal static ItemDiagnosticExportResult Export(
            string modPath,
            ItemDiagnosticTab tab)
        {
            ItemDiagnosticExportResult result = new ItemDiagnosticExportResult();
            try
            {
                if (Game.Instance == null
                    || Game.Instance.Player == null
                    || Game.Instance.Player.Inventory == null)
                {
                    result.Error = "The game inventory is not available.";
                    return result;
                }

                ItemsFilter.FilterType filter = ItemDiagnosticTabInfo.GetFilter(tab);
                GameLocalizationResolver localization = new GameLocalizationResolver();
                DiagnosticGraphBuilder graph = new DiagnosticGraphBuilder(localization);
                JArray entities = tab == ItemDiagnosticTab.AllBlueprintItems
                    ? BuildBlueprintCatalog(graph)
                    : BuildEntities(filter, graph);

                JObject report = BuildReport(
                    tab,
                    filter,
                    localization,
                    graph,
                    entities);
                string outputPath = Path.Combine(
                    modPath ?? string.Empty,
                    DiagnosticsDirectoryName,
                    ItemDiagnosticTabInfo.GetFileName(tab));
                long fileSize = DiagnosticFileWriter.WriteVerified(outputPath, report);

                string additionalOutputPath = string.Empty;
                long additionalFileSize = 0;
                if (tab == ItemDiagnosticTab.Accessories)
                {
                    JObject semanticReport = AccessorySemanticReportBuilder.Build(report);
                    additionalOutputPath = Path.Combine(
                        modPath ?? string.Empty,
                        DiagnosticsDirectoryName,
                        "Items_Accessories_Semantic.json");
                    additionalFileSize = DiagnosticFileWriter.WriteVerified(
                        additionalOutputPath,
                        semanticReport);
                }

                result.Success = true;
                result.OutputPath = outputPath;
                result.AdditionalOutputPath = additionalOutputPath;
                result.ItemCount = entities.Count;
                result.BlueprintCount = graph.BlueprintCount;
                result.FileSize = fileSize;
                result.AdditionalFileSize = additionalFileSize;

                Logger.Info(
                    "Item diagnostics exported: tab="
                    + tab
                    + ", filter="
                    + filter
                    + ", entities="
                    + result.ItemCount
                    + ", blueprints="
                    + result.BlueprintCount
                    + ", graphNodes="
                    + graph.TotalNodeCount
                    + ", expandedComponents="
                    + graph.ExpandedComponentCount
                    + ", expandedElements="
                    + graph.ExpandedElementCount
                    + ", shallowBlueprintReferences="
                    + graph.ShallowBlueprintReferenceCount
                    + ", potentiallyMechanicalTerminals="
                    + graph.PotentialMechanicalTerminalCount
                    + ", truncations="
                    + graph.TruncationCount
                    + ", errors="
                    + graph.ErrorCount
                    + ", suppressedErrors="
                    + graph.SuppressedErrorCount
                    + ", bytes="
                    + result.FileSize
                    + ", semanticBytes="
                    + result.AdditionalFileSize
                    + ", path="
                    + outputPath
                    + (string.IsNullOrEmpty(result.AdditionalOutputPath)
                        ? string.Empty
                        : ", semanticPath=" + result.AdditionalOutputPath)
                    + ".");
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Logger.Exception("Failed to export item diagnostics for " + tab + ".", ex);
            }

            return result;
        }

        private static JArray BuildEntities(
            ItemsFilter.FilterType filter,
            DiagnosticGraphBuilder graph)
        {
            JArray result = new JArray();
            int inventoryIndex = 0;
            foreach (ItemEntity item in Game.Instance.Player.Inventory.Items)
            {
                string itemPath = "entities/inventory[" + inventoryIndex + "]";
                try
                {
                    if (item != null && ItemsFilter.ShouldShowItem(item, filter))
                    {
                        result.Add(BuildEntity(item, inventoryIndex, filter, graph, itemPath));
                    }
                }
                catch (Exception ex)
                {
                    graph.RecordError(itemPath, "BuildEntity", ex);
                    result.Add(new JObject
                    {
                        ["InventoryIndex"] = inventoryIndex,
                        ["Error"] = ex.Message
                    });
                }

                inventoryIndex++;
            }

            return result;
        }

        private static JArray BuildBlueprintCatalog(DiagnosticGraphBuilder graph)
        {
            List<BlueprintItem> blueprints =
                new List<BlueprintItem>(ResourcesLibrary.GetBlueprints<BlueprintItem>());
            blueprints.Sort(delegate(BlueprintItem left, BlueprintItem right)
            {
                return string.Compare(
                    left == null ? string.Empty : left.AssetGuid,
                    right == null ? string.Empty : right.AssetGuid,
                    StringComparison.Ordinal);
            });

            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);
            JArray result = new JArray();
            for (int i = 0; i < blueprints.Count; i++)
            {
                BlueprintItem blueprint = blueprints[i];
                if (blueprint == null)
                {
                    continue;
                }

                string guid = blueprint.AssetGuid ?? string.Empty;
                if (!string.IsNullOrEmpty(guid) && !seenGuids.Add(guid))
                {
                    continue;
                }

                string path = "catalog/blueprints[" + i + "]";
                try
                {
                    result.Add(BuildCatalogItem(blueprint, i, graph, path));
                }
                catch (Exception ex)
                {
                    graph.RecordError(path, "BuildCatalogItem", ex);
                    result.Add(new JObject
                    {
                        ["CatalogIndex"] = i,
                        ["BlueprintGuid"] = guid,
                        ["Error"] = ex.Message
                    });
                }
            }

            return result;
        }

        private static JObject BuildCatalogItem(
            BlueprintItem blueprint,
            int catalogIndex,
            DiagnosticGraphBuilder graph,
            string path)
        {
            LocalizedString nameSource = graph.FindLocalizedString(
                blueprint,
                "m_DisplayNameText");
            LocalizedString descriptionSource = graph.FindLocalizedString(
                blueprint,
                "m_DescriptionText");
            LocalizedString flavorSource = graph.FindLocalizedString(
                blueprint,
                "m_FlavorText");

            return new JObject
            {
                ["CatalogIndex"] = catalogIndex,
                ["EntryType"] = "BlueprintCatalogItem",
                ["Name"] = graph.CreateLocalizedValue(
                    blueprint.Name,
                    nameSource,
                    path + "/name"),
                ["Description"] = graph.CreateLocalizedValue(
                    blueprint.Description,
                    descriptionSource,
                    path + "/description"),
                ["FlavorText"] = graph.CreateLocalizedValue(
                    blueprint.FlavorText,
                    flavorSource,
                    path + "/flavorText"),
                ["Cost"] = blueprint.Cost,
                ["Weight"] = blueprint.Weight,
                ["IdentifyDC"] = blueprint.IdentifyDC,
                ["IsActuallyStackable"] = blueprint.IsActuallyStackable,
                ["Blueprint"] = graph.ReferenceBlueprint(blueprint, path + "/blueprint"),
                ["Enchantments"] = BuildBlueprintEnchantments(
                    blueprint,
                    graph,
                    path + "/enchantments"),
                ["BlueprintGuid"] = blueprint.AssetGuid ?? string.Empty,
                ["BlueprintType"] = blueprint.GetType().FullName,
                ["BlueprintItemType"] = graph.SerializeValue(
                    blueprint.ItemType,
                    path + "/blueprintItemType"),
                ["BlueprintIsNotable"] = blueprint.IsNotable,
                ["BlueprintMiscellaneousType"] = graph.SerializeValue(
                    blueprint.MiscellaneousType,
                    path + "/miscellaneousType")
            };
        }

        private static JObject BuildEntity(
            ItemEntity item,
            int inventoryIndex,
            ItemsFilter.FilterType filter,
            DiagnosticGraphBuilder graph,
            string path)
        {
            BlueprintItem blueprint = item.Blueprint;
            bool identified = item.IsIdentified;
            LocalizedString nameSource = graph.FindLocalizedString(
                blueprint,
                identified ? "m_DisplayNameText" : "m_NonIdentifiedNameText",
                "m_DisplayNameText");
            LocalizedString descriptionSource = graph.FindLocalizedString(
                blueprint,
                identified ? "m_DescriptionText" : "m_NonIdentifiedDescriptionText",
                "m_DescriptionText");
            LocalizedString flavorSource = graph.FindLocalizedString(
                blueprint,
                "m_FlavorText");

            JObject entity = new JObject
            {
                ["InventoryIndex"] = inventoryIndex,
                ["InventorySlotIndex"] = item.InventorySlotIndex,
                ["EntityType"] = item.GetType().FullName,
                ["Identified"] = identified,
                ["Count"] = item.Count,
                ["Charges"] = item.Charges,
                ["Cost"] = item.Cost,
                ["TotalCost"] = item.TotalCost,
                ["TotalWeight"] = item.TotalWeight,
                ["IsStackable"] = item.IsStackable,
                ["IsUsableFromInventory"] = item.IsUsableFromInventory,
                ["IsNonRemovable"] = item.IsNonRemovable,
                ["Name"] = graph.CreateLocalizedValue(item.Name, nameSource, path + "/name"),
                ["Description"] = graph.CreateLocalizedValue(
                    item.Description,
                    descriptionSource,
                    path + "/description"),
                ["FlavorText"] = graph.CreateLocalizedValue(
                    item.FlavorText,
                    flavorSource,
                    path + "/flavorText"),
                ["FilterItemType"] = graph.SerializeValue(
                    ItemsFilter.GetItemType(item, filter),
                    path + "/filterItemType"),
                ["Blueprint"] = graph.ReferenceBlueprint(blueprint, path + "/blueprint"),
                ["Enchantments"] = BuildEnchantments(item, graph, path + "/enchantments")
            };

            if (blueprint != null)
            {
                entity["BlueprintGuid"] = blueprint.AssetGuid ?? string.Empty;
                entity["BlueprintType"] = blueprint.GetType().FullName;
                entity["BlueprintItemType"] = graph.SerializeValue(
                    blueprint.ItemType,
                    path + "/blueprintItemType");
                entity["BlueprintIsNotable"] = blueprint.IsNotable;
                entity["BlueprintMiscellaneousType"] = graph.SerializeValue(
                    blueprint.MiscellaneousType,
                    path + "/miscellaneousType");
            }

            return entity;
        }

        private static JArray BuildEnchantments(
            ItemEntity item,
            DiagnosticGraphBuilder graph,
            string path)
        {
            JArray result = new JArray();
            IList<ItemEnchantment> enchantments = item.Enchantments;
            if (enchantments == null)
            {
                return result;
            }

            for (int i = 0; i < enchantments.Count; i++)
            {
                ItemEnchantment enchantment = enchantments[i];
                if (enchantment == null)
                {
                    result.Add(JValue.CreateNull());
                    continue;
                }

                string entryPath = path + "/[" + i + "]";
                result.Add(new JObject
                {
                    ["Index"] = i,
                    ["RuntimeType"] = enchantment.GetType().FullName,
                    ["Temporary"] = enchantment.IsTemporary,
                    ["Blueprint"] = graph.ReferenceBlueprint(
                        enchantment.Blueprint,
                        entryPath + "/blueprint")
                });
            }

            return result;
        }

        private static JArray BuildBlueprintEnchantments(
            BlueprintItem item,
            DiagnosticGraphBuilder graph,
            string path)
        {
            JArray result = new JArray();
            int index = 0;
            try
            {
                foreach (BlueprintItemEnchantment enchantment in item.Enchantments)
                {
                    result.Add(graph.ReferenceBlueprint(
                        enchantment,
                        path + "/[" + index + "]"));
                    index++;
                }
            }
            catch (Exception ex)
            {
                graph.RecordError(path, "CollectBlueprintEnchantments", ex);
            }

            return result;
        }

        private static JObject BuildReport(
            ItemDiagnosticTab tab,
            ItemsFilter.FilterType filter,
            GameLocalizationResolver localization,
            DiagnosticGraphBuilder graph,
            JArray entities)
        {
            string localizedFilter = string.Empty;
            try
            {
                if (tab == ItemDiagnosticTab.AllBlueprintItems)
                {
                    localizedFilter = "All BlueprintItem objects";
                }
                else
                {
                    BlueprintRoot root = BlueprintRoot.Instance;
                    if (root != null
                        && root.LocalizedTexts != null
                        && root.LocalizedTexts.ItemsFilter != null)
                    {
                        localizedFilter = root.LocalizedTexts.ItemsFilter.GetText(filter);
                    }
                }
            }
            catch (Exception ex)
            {
                graph.RecordError("metadata/filter", "ResolveFilterName", ex);
            }

            JArray blueprintGraph = graph.BuildBlueprintGraph();
            JArray localizationIndex = graph.BuildLocalizationIndex();
            JArray enumIndex = graph.BuildEnumIndex();
            JArray uniqueItems = BuildUniqueItems(entities);
            JObject coverage = graph.BuildCoverage();
            JArray errors = graph.BuildErrors();
            return new JObject
            {
                ["Metadata"] = new JObject
                {
                    ["SchemaVersion"] = SchemaVersion,
                    ["ModVersion"] = Main.ModVersion,
                    ["GameVersion"] = Application.version ?? string.Empty,
                    ["GeneratedUtc"] = DateTime.UtcNow.ToString(
                        "o",
                        CultureInfo.InvariantCulture),
                    ["Locale"] = localization.CurrentLocale,
                    ["SourceScope"] = tab == ItemDiagnosticTab.AllBlueprintItems
                        ? "ResourcesLibrary.GetBlueprints<BlueprintItem>"
                        : "CurrentPlayerInventory",
                    ["SourceDescription"] = tab == ItemDiagnosticTab.AllBlueprintItems
                        ? "Entries are unique BlueprintItem objects currently available from the loaded game resource library. UI tab membership is intentionally not inferred without a real ItemEntity."
                        : "Entities are exact matches from the current player inventory. UniqueItems deduplicates only this export and is not a claim that every BlueprintItem in the game is present."
                },
                ["Filter"] = new JObject
                {
                    ["DiagnosticTab"] = tab.ToString(),
                    ["GameFilterType"] = filter.ToString(),
                    ["GameFilterNumeric"] = (int)filter,
                    ["LocalizedName"] = localizedFilter
                },
                ["Statistics"] = new JObject
                {
                    ["EntityCount"] = entities.Count,
                    ["UniqueItemCount"] = uniqueItems.Count,
                    ["BlueprintCount"] = blueprintGraph.Count,
                    ["GraphNodeCount"] = graph.TotalNodeCount,
                    ["ExpandedBlueprintComponentCount"] = graph.ExpandedComponentCount,
                    ["ExpandedElementCount"] = graph.ExpandedElementCount,
                    ["ShallowBlueprintReferenceCount"] =
                        graph.ShallowBlueprintReferenceCount,
                    ["PotentiallyMechanicalTerminalCount"] =
                        graph.PotentialMechanicalTerminalCount,
                    ["TruncationCount"] = graph.TruncationCount,
                    ["LocalizationEntryCount"] = localizationIndex.Count,
                    ["EnumEntryCount"] = enumIndex.Count,
                    ["ErrorCount"] = errors.Count,
                    ["SuppressedErrorCount"] = graph.SuppressedErrorCount
                },
                ["Entities"] = entities,
                ["UniqueItems"] = uniqueItems,
                ["BlueprintGraph"] = blueprintGraph,
                ["Localization"] = localizationIndex,
                ["EnumIndex"] = enumIndex,
                ["Coverage"] = coverage,
                ["Errors"] = errors
            };
        }

        private static JArray BuildUniqueItems(JArray entities)
        {
            Dictionary<string, JObject> byGuid =
                new Dictionary<string, JObject>(StringComparer.Ordinal);
            for (int i = 0; i < entities.Count; i++)
            {
                JObject entity = entities[i] as JObject;
                if (entity == null)
                {
                    continue;
                }

                string guid = (string)entity["BlueprintGuid"] ?? string.Empty;
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                JObject summary;
                if (!byGuid.TryGetValue(guid, out summary))
                {
                    summary = new JObject
                    {
                        ["BlueprintGuid"] = guid,
                        ["BlueprintType"] = (string)entity["BlueprintType"] ?? string.Empty,
                        ["Name"] = CloneOrNull(entity["Name"]),
                        ["Description"] = CloneOrNull(entity["Description"]),
                        ["FlavorText"] = CloneOrNull(entity["FlavorText"]),
                        ["Cost"] = CloneOrNull(entity["Cost"]),
                        ["BlueprintItemType"] = CloneOrNull(entity["BlueprintItemType"]),
                        ["BlueprintIsNotable"] = CloneOrNull(entity["BlueprintIsNotable"]),
                        ["BlueprintMiscellaneousType"] = CloneOrNull(
                            entity["BlueprintMiscellaneousType"]),
                        ["Blueprint"] = CloneOrNull(entity["Blueprint"]),
                        ["OccurrenceCount"] = 0,
                        ["EntityIndexes"] = new JArray()
                    };
                    byGuid.Add(guid, summary);
                }

                summary["OccurrenceCount"] = (int)summary["OccurrenceCount"] + 1;
                ((JArray)summary["EntityIndexes"]).Add(i);
            }

            List<JObject> items = new List<JObject>(byGuid.Values);
            items.Sort(delegate(JObject left, JObject right)
            {
                string leftName = ReadLocalizedName(left);
                string rightName = ReadLocalizedName(right);
                int name = string.Compare(
                    leftName,
                    rightName,
                    StringComparison.CurrentCultureIgnoreCase);
                return name != 0
                    ? name
                    : string.Compare(
                        (string)left["BlueprintGuid"],
                        (string)right["BlueprintGuid"],
                        StringComparison.Ordinal);
            });

            return new JArray(items);
        }

        private static JToken CloneOrNull(JToken value)
        {
            return value == null ? JValue.CreateNull() : value.DeepClone();
        }

        private static string ReadLocalizedName(JObject item)
        {
            JObject name = item == null ? null : item["Name"] as JObject;
            return name == null ? string.Empty : (string)name["Localized"] ?? string.Empty;
        }
    }
}
