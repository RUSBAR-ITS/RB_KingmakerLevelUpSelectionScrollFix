using System.Collections.Generic;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Items;

namespace KingmakerSmartSorter
{
    internal static class PermanentEnchantmentValueCalculator
    {
        internal static int Calculate(ItemEntity item)
        {
            return item == null ? 0 : Calculate(item.Enchantments);
        }

        internal static int Calculate(IList<ItemEnchantment> enchantments)
        {
            if (enchantments == null)
            {
                return 0;
            }

            long total = 0;
            for (int i = 0; i < enchantments.Count; i++)
            {
                ItemEnchantment enchantment = enchantments[i];
                if (enchantment == null || enchantment.IsTemporary || enchantment.Blueprint == null)
                {
                    continue;
                }

                total += enchantment.Blueprint.EnchantmentCost;
                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }

                if (total <= int.MinValue)
                {
                    return int.MinValue;
                }
            }

            return (int)total;
        }
    }
}
