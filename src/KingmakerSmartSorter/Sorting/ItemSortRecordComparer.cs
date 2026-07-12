using System;
using System.Collections.Generic;

namespace KingmakerSmartSorter
{
    internal sealed class ItemSortRecordComparer : IComparer<ItemSortRecord>
    {
        internal static readonly ItemSortRecordComparer Instance = new ItemSortRecordComparer();

        private ItemSortRecordComparer()
        {
        }

        public int Compare(ItemSortRecord left, ItemSortRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int result;
            if (left.DefenseKey != null && right.DefenseKey != null)
            {
                result = DefenseSortComparer.Compare(left, right);
                if (result != 0)
                {
                    return result;
                }
            }
            else
            {
                result = left.ItemTypeOrder.CompareTo(right.ItemTypeOrder);
                if (result != 0)
                {
                    return result;
                }
            }

            if (left.IsWeapon && right.IsWeapon)
            {
                result = left.IsIdentified.CompareTo(right.IsIdentified);
                if (result != 0)
                {
                    return result;
                }

                result = left.CategoryOrder.CompareTo(right.CategoryOrder);
                if (result != 0)
                {
                    return result;
                }

                if (left.IsIdentified)
                {
                    result = right.PermanentEnchantmentValue.CompareTo(left.PermanentEnchantmentValue);
                    if (result != 0)
                    {
                        return result;
                    }

                    result = right.IsMasterwork.CompareTo(left.IsMasterwork);
                    if (result != 0)
                    {
                        return result;
                    }
                }
            }

            result = LocalizedNameComparer.Compare(left.DisplayName, right.DisplayName);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(left.BlueprintGuid, right.BlueprintGuid, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = left.InventorySlotIndex.CompareTo(right.InventorySlotIndex);
            if (result != 0)
            {
                return result;
            }

            return left.OriginalIndex.CompareTo(right.OriginalIndex);
        }
    }
}
