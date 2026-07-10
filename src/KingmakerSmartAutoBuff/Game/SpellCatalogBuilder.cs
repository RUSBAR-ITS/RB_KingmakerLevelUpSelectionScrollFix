using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellCatalogBuilder
    {
        internal static List<SpellCatalogEntry> BuildSpellEntries(UnitEntityData caster, int levelFilter)
        {
            List<SpellCatalogEntry> result = new List<SpellCatalogEntry>();
            if (caster == null || caster.Descriptor == null || caster.Descriptor.Spellbooks == null)
            {
                return result;
            }

            foreach (Spellbook spellbook in caster.Descriptor.Spellbooks.Where(spellbook => spellbook != null))
            {
                try
                {
                    if (spellbook.Blueprint != null && spellbook.Blueprint.Spontaneous)
                    {
                        SpontaneousSpellProvider.AddEntries(result, caster, spellbook, levelFilter);
                    }
                    else
                    {
                        PreparedSpellProvider.AddEntries(result, caster, spellbook, levelFilter);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception("Failed to build spell entries for spellbook.", ex);
                }
            }

            return result
                .OrderBy(entry => entry.SpellLevel)
                .ThenBy(entry => entry.SpellName)
                .ThenBy(entry => entry.MetamagicText)
                .ToList();
        }
    }
}
