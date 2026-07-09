using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerSpellbookFix
{
    [HarmonyPatch(typeof(SpellBookView), "MaxPageOnLevelIndex")]
    internal static class SpellbookPagePatch
    {
        private static int s_LogCount;

        internal static void ResetDiagnostics()
        {
            s_LogCount = 0;
        }

        private static void Postfix(SpellBookView __instance, int level, ref int __result)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch || __instance == null)
            {
                return;
            }

            try
            {
                int levelIndex = level;
                int favoriteBookmarkIndex = GetFavoriteBookmarkIndex(__instance);
                bool isFavoriteBookmark = levelIndex == favoriteBookmarkIndex;
                if (!isFavoriteBookmark && !settings.RecalculateUndercountedRegularPages)
                {
                    return;
                }

                if (isFavoriteBookmark && !settings.FixMetamagicBookmarkPages)
                {
                    return;
                }

                List<AbilityData> visibleSpells = GetVisibleSpellList(__instance);
                if (visibleSpells == null)
                {
                    LogCalculation(
                        settings,
                        "Spellbook page calculation skipped because m_ShowSpellsList is null. "
                        + "levelIndex=" + levelIndex
                        + ", favoriteBookmarkIndex=" + favoriteBookmarkIndex
                        + ", vanillaMaxPage=" + __result);
                    return;
                }

                int shortPageSlots = CountSpellItems(__instance, "m_SpellItemsShort");
                int longPageSlots = CountSpellItems(__instance, "m_SpellItemsLong");
                int patchedMaxPage = CalculateMaxPageIndex(
                    visibleSpells.Count,
                    shortPageSlots,
                    longPageSlots);

                if (patchedMaxPage > __result)
                {
                    LogCalculation(
                        settings,
                        "Spellbook max page corrected. "
                        + "levelIndex=" + levelIndex
                        + ", favoriteBookmarkIndex=" + favoriteBookmarkIndex
                        + ", isFavoriteBookmark=" + isFavoriteBookmark
                        + ", visibleSpells=" + visibleSpells.Count
                        + ", shortPageSlots=" + shortPageSlots
                        + ", longPageSlots=" + longPageSlots
                        + ", vanillaMaxPage=" + __result
                        + ", patchedMaxPage=" + patchedMaxPage);

                    __result = patchedMaxPage;
                    return;
                }

                LogCalculation(
                    settings,
                    "Spellbook max page left unchanged. "
                    + "levelIndex=" + levelIndex
                    + ", favoriteBookmarkIndex=" + favoriteBookmarkIndex
                    + ", isFavoriteBookmark=" + isFavoriteBookmark
                    + ", visibleSpells=" + visibleSpells.Count
                    + ", shortPageSlots=" + shortPageSlots
                    + ", longPageSlots=" + longPageSlots
                    + ", vanillaMaxPage=" + __result
                    + ", calculatedMaxPage=" + patchedMaxPage);
            }
            catch (Exception ex)
            {
                Logger.Exception("Spellbook max page patch failed.", ex);
            }
        }

        private static int GetFavoriteBookmarkIndex(SpellBookView spellBookView)
        {
            if (spellBookView.LevelBookSwitcher == null)
            {
                return -1;
            }

            return spellBookView.LevelBookSwitcher.FavotiveToggleIndex;
        }

        private static List<AbilityData> GetVisibleSpellList(SpellBookView spellBookView)
        {
            Traverse traverse = Traverse.Create(spellBookView);
            return traverse.Field("m_ShowSpellsList").GetValue<List<AbilityData>>();
        }

        private static int CountSpellItems(SpellBookView spellBookView, string fieldName)
        {
            Traverse traverse = Traverse.Create(spellBookView);
            List<SpellItem> spellItems = traverse.Field(fieldName).GetValue<List<SpellItem>>();
            return spellItems != null ? spellItems.Count : 0;
        }

        private static int CalculateMaxPageIndex(int spellCount, int shortPageSlots, int longPageSlots)
        {
            if (spellCount <= 0 || shortPageSlots <= 0)
            {
                return 0;
            }

            if (spellCount <= shortPageSlots)
            {
                return 0;
            }

            if (longPageSlots <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling((spellCount - shortPageSlots) / (double)longPageSlots);
        }

        private static void LogCalculation(Settings settings, string message)
        {
            if (settings == null || !settings.LogPageCalculations)
            {
                return;
            }

            if (s_LogCount >= settings.MaxPageCalculationLogs)
            {
                return;
            }

            s_LogCount++;
            Logger.Info(message);
        }
    }
}
