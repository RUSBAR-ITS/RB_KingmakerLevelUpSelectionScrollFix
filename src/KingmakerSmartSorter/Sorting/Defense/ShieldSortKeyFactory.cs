using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Items;

namespace KingmakerSmartSorter
{
    internal static class ShieldSortKeyFactory
    {
        internal static DefenseSortKey Create(
            ItemEntity item,
            BlueprintItemShield blueprint,
            bool isIdentified)
        {
            BlueprintShieldType shieldType = blueprint != null ? blueprint.Type : null;
            ArmorProficiencyGroup proficiencyGroup = shieldType != null
                ? shieldType.ProficiencyGroup
                : (ArmorProficiencyGroup)(-1);

            ItemEntityShield shield = item as ItemEntityShield;
            int defensiveEnchantmentValue = 0;
            int weaponEnchantmentValue = 0;

            if (isIdentified && shield != null)
            {
                defensiveEnchantmentValue = PermanentEnchantmentValueCalculator.Calculate(shield.ArmorComponent);
                weaponEnchantmentValue = PermanentEnchantmentValueCalculator.Calculate(shield.WeaponComponent);
            }

            return new DefenseSortKey(
                true,
                MapCategory(proficiencyGroup),
                proficiencyGroup,
                shieldType != null ? shieldType.ArmorBonus : 0,
                shieldType != null && blueprint != null ? blueprint.SubtypeName : string.Empty,
                shieldType != null ? shieldType.AssetGuid : string.Empty,
                defensiveEnchantmentValue,
                weaponEnchantmentValue);
        }

        private static DefenseCategory MapCategory(ArmorProficiencyGroup proficiencyGroup)
        {
            switch (proficiencyGroup)
            {
                case ArmorProficiencyGroup.Buckler:
                    return DefenseCategory.Buckler;
                case ArmorProficiencyGroup.LightShield:
                    return DefenseCategory.LightShield;
                case ArmorProficiencyGroup.HeavyShield:
                    return DefenseCategory.HeavyShield;
                case ArmorProficiencyGroup.TowerShield:
                    return DefenseCategory.TowerShield;
                default:
                    return DefenseCategory.OtherShield;
            }
        }
    }
}
