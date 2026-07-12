using Kingmaker.Enums;

namespace KingmakerSmartSorter
{
    internal static class WeaponCategoryOrder
    {
        internal static int Get(WeaponCategory category)
        {
            // Kingmaker's enum follows the same canonical category order used by
            // parametrized weapon feats such as Weapon Focus.
            return (int)category;
        }
    }
}
