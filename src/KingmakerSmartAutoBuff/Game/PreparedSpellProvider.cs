using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerSmartAutoBuff
{
    internal static class PreparedSpellProvider
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

                IEnumerable<SpellSlot> slots = spellbook.GetMemorizedSpellSlots(level);
                if (slots == null)
                {
                    continue;
                }

                foreach (SpellSlot slot in slots)
                {
                    if (slot == null || slot.Spell == null || slot.Spell.Blueprint == null || !slot.Available)
                    {
                        continue;
                    }

                    foreach (AbilityData castableAbility in SpellVariantExpander.GetCastableVariants(slot.Spell))
                    {
                        SpellCatalogEntry entry = SpellEntryFactory.CreateEntry(caster, spellbook, castableAbility, level, 1);
                        string key = SpellEntryFactory.BuildEntryKey(entry);
                        SpellCatalogEntry existing;
                        if (unique.TryGetValue(key, out existing))
                        {
                            existing.AvailableCasts++;
                        }
                        else
                        {
                            unique[key] = entry;
                        }
                    }
                }
            }

            result.AddRange(unique.Values);
        }
    }
}
