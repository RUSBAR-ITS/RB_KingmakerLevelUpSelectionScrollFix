using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UI.Vendor;

namespace KingmakerSmartSorter
{
    [HarmonyPatch(typeof(FilterController), "Initialize")]
    internal static class FilterControllerInitializePatch
    {
        private static void Postfix(FilterController __instance)
        {
            SmartSortController.AfterInitialize(__instance);
        }
    }

    [HarmonyPatch(typeof(FilterController), "LoadStates")]
    internal static class FilterControllerLoadStatesPatch
    {
        private static void Prefix(FilterController __instance)
        {
            SmartSortController.BeforeLoadStates(__instance);
        }

        private static void Postfix(FilterController __instance)
        {
            SmartSortController.AfterLoadStates(__instance);
        }
    }

    [HarmonyPatch(typeof(FilterController), "SortIternal")]
    internal static class FilterControllerSortInternalPatch
    {
        private static bool Prefix(FilterController __instance, int __0)
        {
            try
            {
                return SmartSortController.HandleSorterSelection(__instance, __0);
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to process a sorter menu selection.", ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ItemsFilter), "ItemSorter")]
    internal static class ItemsFilterItemSorterPatch
    {
        private static bool Prefix(
            ItemsFilter.SorterType __0,
            List<ItemEntity> __1,
            ItemsFilter.FilterType __2,
            ref List<ItemEntity> __result)
        {
            if (!SmartSortController.IsSmartSorter(__0))
            {
                return true;
            }

            try
            {
                __result = SmartItemSorter.Sort(__1, __2);
            }
            catch (Exception ex)
            {
                Logger.Exception("Smart sorting failed; falling back to the vanilla type sorter.", ex);
                __result = ItemsFilter.ItemSorter(ItemsFilter.SorterType.TypeUp, __1, __2);
            }

            return false;
        }
    }
}
