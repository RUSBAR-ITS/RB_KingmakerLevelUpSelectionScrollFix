using System;
using System.Collections.Generic;
using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace KingmakerSmartSorter
{
    internal static class SortDiagnostics
    {
        private const int MaxExtractionFailures = 20;

        private static int s_SortRunCount;
        private static int s_ExtractionFailureCount;

        internal static void Reset()
        {
            s_SortRunCount = 0;
            s_ExtractionFailureCount = 0;
        }

        internal static void Report(
            IList<ItemSortRecord> sortedRecords,
            ItemsFilter.FilterType filterType)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.LogDiagnostics)
            {
                return;
            }

            s_SortRunCount++;
            if (s_SortRunCount > settings.MaxDiagnosticSortRuns)
            {
                return;
            }

            int weaponCount = 0;
            int unidentifiedWeaponCount = 0;
            int shieldCount = 0;
            int armorCount = 0;
            int unidentifiedDefenseCount = 0;
            for (int i = 0; i < sortedRecords.Count; i++)
            {
                ItemSortRecord record = sortedRecords[i];
                if (record != null && record.IsWeapon)
                {
                    weaponCount++;
                    if (!record.IsIdentified)
                    {
                        unidentifiedWeaponCount++;
                    }
                }

                if (record != null && record.DefenseKey != null)
                {
                    if (record.DefenseKey.IsShield)
                    {
                        shieldCount++;
                    }
                    else
                    {
                        armorCount++;
                    }

                    if (!record.IsIdentified)
                    {
                        unidentifiedDefenseCount++;
                    }
                }
            }

            Logger.Info(
                "Smart sort run "
                + s_SortRunCount
                + ": filter="
                + filterType
                + ", items="
                + sortedRecords.Count
                + ", weapons="
                + weaponCount
                + ", unidentifiedWeapons="
                + unidentifiedWeaponCount
                + ", shields="
                + shieldCount
                + ", armor="
                + armorCount
                + ", unidentifiedDefense="
                + unidentifiedDefenseCount
                + ".");

            bool logWeapons = filterType != ItemsFilter.FilterType.Armor;
            bool logDefense = filterType != ItemsFilter.FilterType.Weapon;
            int logged = 0;
            for (int i = 0; i < sortedRecords.Count && logged < settings.MaxDiagnosticItemsPerRun; i++)
            {
                ItemSortRecord record = sortedRecords[i];
                if (record == null)
                {
                    continue;
                }

                if (logWeapons && record.IsWeapon)
                {
                    LogWeapon(i, record);
                    logged++;
                    continue;
                }

                if (logDefense && record.DefenseKey != null)
                {
                    LogDefense(i, record);
                    logged++;
                }
            }
        }

        private static void LogWeapon(int sortedIndex, ItemSortRecord record)
        {
            Logger.Info(
                "  weapon["
                + sortedIndex
                + "] identified="
                + record.IsIdentified
                + ", category="
                + record.WeaponCategory
                + ", enchantmentValue="
                + (record.IsIdentified ? record.PermanentEnchantmentValue.ToString() : "<hidden>")
                + ", masterwork="
                + (record.IsIdentified ? record.IsMasterwork.ToString() : "<hidden>")
                + ", name='"
                + record.DisplayName
                + "', blueprint="
                + record.BlueprintGuid
                + ", previousSlot="
                + record.InventorySlotIndex
                + ".");
        }

        private static void LogDefense(int sortedIndex, ItemSortRecord record)
        {
            DefenseSortKey key = record.DefenseKey;
            Logger.Info(
                "  defense["
                + sortedIndex
                + "] kind="
                + (key.IsShield ? "shield" : "armor")
                + ", identified="
                + record.IsIdentified
                + ", category="
                + key.Category
                + ", proficiency="
                + key.ProficiencyGroup
                + ", baseType='"
                + key.BaseTypeName
                + "', baseArmorBonus="
                + key.BaseArmorBonus
                + ", defensiveEnchantmentValue="
                + (record.IsIdentified ? key.PermanentEnchantmentValue.ToString() : "<hidden>")
                + ", shieldWeaponEnchantmentValue="
                + (record.IsIdentified && key.IsShield ? key.ShieldWeaponEnchantmentValue.ToString() : "<not-used>")
                + ", name='"
                + record.DisplayName
                + "', baseTypeGuid="
                + key.BaseTypeGuid
                + ", blueprint="
                + record.BlueprintGuid
                + ", previousSlot="
                + record.InventorySlotIndex
                + ".");
        }

        internal static void ReportExtractionFailure(ItemEntity item, Exception exception)
        {
            s_ExtractionFailureCount++;
            if (s_ExtractionFailureCount > MaxExtractionFailures)
            {
                return;
            }

            string itemName = "<unknown>";
            try
            {
                if (item != null && !string.IsNullOrEmpty(item.Name))
                {
                    itemName = item.Name;
                }
            }
            catch
            {
                itemName = "<unreadable>";
            }

            Logger.Exception("Failed to build a sorting record for item '" + itemName + "'.", exception);
        }
    }
}
