using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerSmartAutoBuff
{
    internal static class SpontaneousSpellProvider
    {
        internal static void AddEntries(
            List<SpellCatalogEntry> result,
            UnitEntityData caster,
            Spellbook spellbook,
            int levelFilter)
        {
            Dictionary<string, SpellCatalogEntry> unique = new Dictionary<string, SpellCatalogEntry>();
            int maxLevel = CasterCatalog.SafeMaxSpellLevel(spellbook);

            for (int level = 0; level <= maxLevel; level++)
            {
                if (levelFilter >= 0 && level != levelFilter)
                {
                    continue;
                }

                AddAbilityData(unique, caster, spellbook, level, spellbook.GetKnownSpells(level));
                AddAbilityData(unique, caster, spellbook, level, spellbook.GetCustomSpells(level));
            }

            result.AddRange(unique.Values);
        }

        private static void AddAbilityData(
            Dictionary<string, SpellCatalogEntry> unique,
            UnitEntityData caster,
            Spellbook spellbook,
            int level,
            IEnumerable<AbilityData> spells)
        {
            if (spells == null)
            {
                return;
            }

            foreach (AbilityData ability in spells)
            {
                if (ability == null || ability.Blueprint == null)
                {
                    continue;
                }

                int available = SpellEntryFactory.SafeAvailableCastCount(spellbook, ability);
                if (available <= 0)
                {
                    continue;
                }

                foreach (AbilityData castableAbility in SpellVariantExpander.GetCastableVariants(ability))
                {
                    SpellCatalogEntry entry = SpellEntryFactory.CreateEntry(caster, spellbook, castableAbility, level, available);
                    unique[SpellEntryFactory.BuildEntryKey(entry)] = entry;
                }
            }
        }
    }
}
