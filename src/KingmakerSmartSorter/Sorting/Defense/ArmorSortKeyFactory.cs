using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Items;

namespace KingmakerSmartSorter
{
    internal static class ArmorSortKeyFactory
    {
        internal static DefenseSortKey Create(
            ItemEntity item,
            BlueprintItemArmor blueprint,
            bool isIdentified)
        {
            BlueprintArmorType armorType = blueprint != null ? blueprint.Type : null;
            ArmorProficiencyGroup proficiencyGroup = armorType != null
                ? armorType.ProficiencyGroup
                : (ArmorProficiencyGroup)(-1);

            return new DefenseSortKey(
                false,
                MapCategory(proficiencyGroup),
                proficiencyGroup,
                armorType != null ? armorType.ArmorBonus : 0,
                armorType != null && blueprint != null ? blueprint.SubtypeName : string.Empty,
                armorType != null ? armorType.AssetGuid : string.Empty,
                isIdentified ? PermanentEnchantmentValueCalculator.Calculate(item) : 0,
                0);
        }

        private static DefenseCategory MapCategory(ArmorProficiencyGroup proficiencyGroup)
        {
            switch (proficiencyGroup)
            {
                case ArmorProficiencyGroup.None:
                    return DefenseCategory.Clothing;
                case ArmorProficiencyGroup.Light:
                    return DefenseCategory.LightArmor;
                case ArmorProficiencyGroup.Medium:
                    return DefenseCategory.MediumArmor;
                case ArmorProficiencyGroup.Heavy:
                    return DefenseCategory.HeavyArmor;
                default:
                    return DefenseCategory.OtherArmor;
            }
        }
    }
}
