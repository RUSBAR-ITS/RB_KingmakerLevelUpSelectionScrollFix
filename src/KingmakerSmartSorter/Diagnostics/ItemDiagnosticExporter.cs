using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Kingmaker;
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
        private const int SchemaVersion = 1;

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
                JArray entities = BuildEntities(filter, graph);

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

                result.Success = true;
                result.OutputPath = outputPath;
                result.ItemCount = entities.Count;
                result.BlueprintCount = graph.BlueprintCount;
                result.FileSize = fileSize;

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
                    + ", errors="
                    + graph.ErrorCount
                    + ", bytes="
                    + result.FileSize
                    + ", path="
                    + outputPath
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
                BlueprintRoot root = BlueprintRoot.Instance;
                if (root != null
                    && root.LocalizedTexts != null
                    && root.LocalizedTexts.ItemsFilter != null)
                {
                    localizedFilter = root.LocalizedTexts.ItemsFilter.GetText(filter);
                }
            }
            catch (Exception ex)
            {
                graph.RecordError("metadata/filter", "ResolveFilterName", ex);
            }

            JArray blueprintGraph = graph.BuildBlueprintGraph();
            JArray localizationIndex = graph.BuildLocalizationIndex();
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
                    ["Locale"] = localization.CurrentLocale
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
                    ["BlueprintCount"] = blueprintGraph.Count,
                    ["GraphNodeCount"] = graph.TotalNodeCount,
                    ["LocalizationEntryCount"] = localizationIndex.Count,
                    ["ErrorCount"] = errors.Count
                },
                ["Entities"] = entities,
                ["BlueprintGraph"] = blueprintGraph,
                ["Localization"] = localizationIndex,
                ["Errors"] = errors
            };
        }
    }
}
