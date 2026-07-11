using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellQueueResolver
    {
        internal static SpellCatalogEntry FindCurrentEntry(BuffQueueAction action)
        {
            return FindCurrentEntries(action).FirstOrDefault();
        }

        internal static List<SpellCatalogEntry> FindCurrentEntries(BuffQueueAction action)
        {
            return FindCandidateEntries(action)
                .Where(candidate => candidate.IsAvailable && candidate.Entry != null)
                .Select(candidate => candidate.Entry)
                .ToList();
        }

        internal static List<CasterCandidate> FindCandidateEntries(BuffQueueAction action)
        {
            if (action == null)
            {
                return new List<CasterCandidate>();
            }

            List<CasterCandidate> result = new List<CasterCandidate>();
            foreach (QueueCasterReference reference in CandidateReferences(action))
            {
                CasterCandidate candidate = ResolveCandidate(action, reference);
                result.Add(candidate);
            }

            return CasterPriorityResolver.SortCandidates(result);
        }

        private static CasterCandidate ResolveCandidate(BuffQueueAction action, QueueCasterReference reference)
        {
            CasterCandidate candidate = new CasterCandidate();
            candidate.Reference = reference;

            UnitEntityData caster = PartyProvider.FindPartyUnit(reference.CasterId, reference.CasterName);
            if (caster == null)
            {
                candidate.Reason = ModLocalization.T("Execution.Skip.CasterUnavailable");
                return candidate;
            }

            SpellCatalogEntry entry = SpellCatalogBuilder.BuildSpellEntries(caster, -1).FirstOrDefault(item => MatchesAction(item, action, reference));
            if (entry == null)
            {
                candidate.Reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return candidate;
            }

            candidate.Entry = entry;
            candidate.IsAvailable = IsAbilityCurrentlyAvailable(entry.Ability, out candidate.Reason);
            CasterPriorityResolver.FillPriority(candidate);
            return candidate;
        }

        private static IEnumerable<QueueCasterReference> CandidateReferences(BuffQueueAction action)
        {
            if (action.CandidateCasters != null && action.CandidateCasters.Count > 0)
            {
                foreach (QueueCasterReference reference in action.CandidateCasters)
                {
                    if (reference != null)
                    {
                        yield return reference;
                    }
                }

                yield break;
            }

            yield return new QueueCasterReference
            {
                CasterId = action.CasterId,
                CasterName = action.CasterName,
                SpellbookId = action.SpellbookId,
                SpellbookName = action.SpellbookName
            };
        }

        private static bool MatchesAction(SpellCatalogEntry entry, BuffQueueAction action, QueueCasterReference reference)
        {
            if (entry == null || action == null)
            {
                return false;
            }

            if (reference != null
                && !string.Equals(entry.CasterId, reference.CasterId, StringComparison.Ordinal)
                && !string.Equals(entry.CasterName, reference.CasterName, StringComparison.Ordinal))
            {
                return false;
            }

            bool spellbookMatches = reference == null
                || string.IsNullOrEmpty(reference.SpellbookId)
                || string.Equals(entry.SpellbookId, reference.SpellbookId, StringComparison.Ordinal);

            return spellbookMatches
                && string.Equals(entry.SpellBlueprintId, action.SpellBlueprintId, StringComparison.Ordinal)
                && entry.SpellLevel == action.SpellLevel
                && SameMetamagic(entry.MetamagicNames, action.Metamagic);
        }

        private static bool IsAbilityCurrentlyAvailable(AbilityData ability, out string reason)
        {
            reason = string.Empty;
            if (ability == null)
            {
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            try
            {
                if (!ability.IsAvailable || !ability.IsAvailableForCast)
                {
                    reason = SafeUnavailableReason(ability);
                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to check candidate ability availability.", ex);
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            return true;
        }

        private static string SafeUnavailableReason(AbilityData ability)
        {
            try
            {
                return ability.GetUnavailableReason();
            }
            catch
            {
                return string.Empty;
            }
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
