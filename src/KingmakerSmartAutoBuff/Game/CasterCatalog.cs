using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace KingmakerSmartAutoBuff
{
    internal static class CasterCatalog
    {
        internal static List<CasterOption> GetCasters()
        {
            List<CasterOption> result = new List<CasterOption>();

            foreach (UnitEntityData unit in PartyProvider.GetActiveParty())
            {
                try
                {
                    UnitDescriptor descriptor = unit.Descriptor;
                    if (descriptor == null || descriptor.Spellbooks == null)
                    {
                        continue;
                    }

                    List<Spellbook> spellbooks = descriptor.Spellbooks.Where(spellbook => spellbook != null).ToList();
                    if (spellbooks.Count == 0)
                    {
                        continue;
                    }

                    CasterOption option = new CasterOption();
                    option.Unit = unit;
                    option.Id = PartyProvider.GetUnitId(unit);
                    option.Name = PartyProvider.SafeUnitName(unit);
                    option.MaxSpellLevel = spellbooks.Max(SafeMaxSpellLevel);
                    result.Add(option);
                }
                catch (Exception ex)
                {
                    Logger.Exception("Failed to read caster option.", ex);
                }
            }

            return result
                .OrderBy(caster => caster.Name)
                .ToList();
        }

        internal static int SafeMaxSpellLevel(Spellbook spellbook)
        {
            try
            {
                return spellbook != null ? Math.Max(0, spellbook.MaxSpellLevel) : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
