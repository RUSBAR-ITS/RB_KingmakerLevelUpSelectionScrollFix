using System;

namespace KingmakerSmartSorter
{
    internal static class DefenseSortComparer
    {
        internal static int Compare(ItemSortRecord left, ItemSortRecord right)
        {
            DefenseSortKey leftKey = left.DefenseKey;
            DefenseSortKey rightKey = right.DefenseKey;

            int result = leftKey.Category.CompareTo(rightKey.Category);
            if (result != 0)
            {
                return result;
            }

            result = leftKey.BaseArmorBonus.CompareTo(rightKey.BaseArmorBonus);
            if (result != 0)
            {
                return result;
            }

            result = LocalizedNameComparer.Compare(leftKey.BaseTypeName, rightKey.BaseTypeName);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(leftKey.BaseTypeGuid, rightKey.BaseTypeGuid, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            // Unidentified items stay together inside their known base type without
            // exposing hidden enchantments through their position.
            result = left.IsIdentified.CompareTo(right.IsIdentified);
            if (result != 0)
            {
                return result;
            }

            if (left.IsIdentified)
            {
                result = rightKey.PermanentEnchantmentValue.CompareTo(leftKey.PermanentEnchantmentValue);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }
}
