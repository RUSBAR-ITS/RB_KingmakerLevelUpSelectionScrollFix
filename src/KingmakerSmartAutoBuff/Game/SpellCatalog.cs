using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellCatalog
    {
        internal static List<UnitEntityData> GetActiveParty()
        {
            return PartyProvider.GetActiveParty();
        }

        internal static List<CasterOption> GetCasters()
        {
            return CasterCatalog.GetCasters();
        }

        internal static List<SpellCatalogEntry> BuildSpellEntries(UnitEntityData caster, int levelFilter)
        {
            return SpellCatalogBuilder.BuildSpellEntries(caster, levelFilter);
        }

        internal static List<TargetOption> BuildTargetOptions(SpellCatalogEntry entry)
        {
            return TargetResolver.BuildTargetOptions(entry);
        }

        internal static SpellCatalogEntry FindCurrentEntry(BuffQueueAction action)
        {
            return SpellQueueResolver.FindCurrentEntry(action);
        }

        internal static List<SpellCatalogEntry> FindCurrentEntries(BuffQueueAction action)
        {
            return SpellQueueResolver.FindCurrentEntries(action);
        }

        internal static List<CasterCandidate> FindCandidateEntries(BuffQueueAction action)
        {
            return SpellQueueResolver.FindCandidateEntries(action);
        }

        internal static UnitEntityData FindPartyUnit(string id, string name)
        {
            return PartyProvider.FindPartyUnit(id, name);
        }

        internal static string GetUnitId(UnitEntityData unit)
        {
            return PartyProvider.GetUnitId(unit);
        }

        internal static string SafeUnitName(UnitEntityData unit)
        {
            return PartyProvider.SafeUnitName(unit);
        }
    }
}
