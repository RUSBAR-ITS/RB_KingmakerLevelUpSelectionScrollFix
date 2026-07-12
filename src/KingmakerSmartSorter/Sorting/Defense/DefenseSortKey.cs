using Kingmaker.Blueprints.Items.Armors;

namespace KingmakerSmartSorter
{
    internal sealed class DefenseSortKey
    {
        internal DefenseSortKey(
            bool isShield,
            DefenseCategory category,
            ArmorProficiencyGroup proficiencyGroup,
            int baseArmorBonus,
            string baseTypeName,
            string baseTypeGuid,
            int permanentEnchantmentValue,
            int shieldWeaponEnchantmentValue)
        {
            IsShield = isShield;
            Category = category;
            ProficiencyGroup = proficiencyGroup;
            BaseArmorBonus = baseArmorBonus;
            BaseTypeName = baseTypeName ?? string.Empty;
            BaseTypeGuid = baseTypeGuid ?? string.Empty;
            PermanentEnchantmentValue = permanentEnchantmentValue;
            ShieldWeaponEnchantmentValue = shieldWeaponEnchantmentValue;
        }

        internal bool IsShield { get; private set; }

        internal DefenseCategory Category { get; private set; }

        internal ArmorProficiencyGroup ProficiencyGroup { get; private set; }

        internal int BaseArmorBonus { get; private set; }

        internal string BaseTypeName { get; private set; }

        internal string BaseTypeGuid { get; private set; }

        internal int PermanentEnchantmentValue { get; private set; }

        internal int ShieldWeaponEnchantmentValue { get; private set; }
    }
}
