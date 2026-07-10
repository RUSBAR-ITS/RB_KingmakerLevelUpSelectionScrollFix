using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellQueueResolver
    {
        internal static SpellCatalogEntry FindCurrentEntry(BuffQueueAction action)
        {
            if (action == null)
            {
                return null;
            }

            UnitEntityData caster = PartyProvider.FindPartyUnit(action.CasterId, action.CasterName);
            if (caster == null)
            {
                return null;
            }

            return SpellCatalogBuilder.BuildSpellEntries(caster, -1).FirstOrDefault(entry => MatchesAction(entry, action));
        }

        private static bool MatchesAction(SpellCatalogEntry entry, BuffQueueAction action)
        {
            if (entry == null || action == null)
            {
                return false;
            }

            if (!string.Equals(entry.CasterId, action.CasterId, StringComparison.Ordinal)
                && !string.Equals(entry.CasterName, action.CasterName, StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(entry.SpellbookId, action.SpellbookId, StringComparison.Ordinal)
                && string.Equals(entry.SpellBlueprintId, action.SpellBlueprintId, StringComparison.Ordinal)
                && entry.SpellLevel == action.SpellLevel
                && SameMetamagic(entry.MetamagicNames, action.Metamagic);
        }

        private static bool SameMetamagic(List<string> left, List<string> right)
        {
            left = left ?? new List<string>();
            right = right ?? new List<string>();
            if (left.Count != right.Count)
            {
                return false;
            }

            return !left.Except(right).Any() && !right.Except(left).Any();
        }
    }
}
