using System;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace KingmakerSmartSorter
{
    internal sealed class ItemSortRecord
    {
        private ItemSortRecord(ItemEntity item, int originalIndex)
        {
            Item = item;
            OriginalIndex = originalIndex;
            ItemTypeOrder = int.MaxValue;
            CategoryOrder = int.MaxValue;
            DisplayName = string.Empty;
            BlueprintGuid = string.Empty;
            InventorySlotIndex = int.MaxValue;
        }

        internal ItemEntity Item { get; private set; }

        internal int OriginalIndex { get; private set; }

        internal int ItemTypeOrder { get; private set; }

        internal bool IsWeapon { get; private set; }

        internal bool IsIdentified { get; private set; }

        internal WeaponCategory WeaponCategory { get; private set; }

        internal int CategoryOrder { get; private set; }

        internal int PermanentEnchantmentValue { get; private set; }

        internal bool IsMasterwork { get; private set; }

        internal DefenseSortKey DefenseKey { get; private set; }

        internal string DisplayName { get; private set; }

        internal string BlueprintGuid { get; private set; }

        internal int InventorySlotIndex { get; private set; }

        internal static ItemSortRecord Create(
            ItemEntity item,
            int originalIndex,
            ItemsFilter.FilterType filterType)
        {
            ItemSortRecord record = new ItemSortRecord(item, originalIndex);
            if (item == null)
            {
                return record;
            }

            try
            {
                record.ItemTypeOrder = (int)ItemsFilter.GetItemType(item, filterType);
                record.DisplayName = item.Name ?? string.Empty;
                record.InventorySlotIndex = item.InventorySlotIndex;
                record.IsIdentified = item.IsIdentified;

                if (item.Blueprint != null)
                {
                    record.BlueprintGuid = item.Blueprint.AssetGuid ?? string.Empty;
                }

                BlueprintItemWeapon weaponBlueprint = item.Blueprint as BlueprintItemWeapon;
                if (weaponBlueprint != null)
                {
                    record.IsWeapon = true;
                    record.WeaponCategory = weaponBlueprint.Category;
                    record.CategoryOrder = WeaponCategoryOrder.Get(weaponBlueprint.Category);

                    // Reading enchantments here would reveal hidden power through the order.
                    if (record.IsIdentified)
                    {
                        record.PermanentEnchantmentValue = PermanentEnchantmentValueCalculator.Calculate(item);
                        record.IsMasterwork = weaponBlueprint.IsMasterwork;
                    }

                    return record;
                }

                BlueprintItemShield shieldBlueprint = item.Blueprint as BlueprintItemShield;
                if (shieldBlueprint != null)
                {
                    record.DefenseKey = ShieldSortKeyFactory.Create(item, shieldBlueprint, record.IsIdentified);
                    return record;
                }

                BlueprintItemArmor armorBlueprint = item.Blueprint as BlueprintItemArmor;
                if (armorBlueprint != null)
                {
                    record.DefenseKey = ArmorSortKeyFactory.Create(item, armorBlueprint, record.IsIdentified);
                }
            }
            catch (Exception ex)
            {
                SortDiagnostics.ReportExtractionFailure(item, ex);
            }

            return record;
        }
    }
}
