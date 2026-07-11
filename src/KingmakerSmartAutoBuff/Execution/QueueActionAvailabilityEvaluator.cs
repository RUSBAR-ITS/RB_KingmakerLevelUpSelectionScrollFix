using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueActionAvailabilityEvaluator
    {
        internal static QueueActionAvailability Evaluate(BuffQueueAction action)
        {
            QueueActionAvailability availability = new QueueActionAvailability();
            if (action == null)
            {
                availability.Reason = ModLocalization.T("Execution.Skip.EmptyAction");
                return availability;
            }

            List<CasterCandidate> candidates = SpellQueueResolver.FindCandidateEntries(action);
            foreach (CasterCandidate candidate in candidates)
            {
                if (candidate.IsAvailable && candidate.Entry != null)
                {
                    availability.AvailableCasters.Add(candidate);
                }
                else
                {
                    availability.UnavailableCasters.Add(candidate);
                }
            }

            availability.IsSpellAvailable = availability.AvailableCasters.Count > 0;
            availability.BestEntry = availability.AvailableCasters.Count > 0 ? availability.AvailableCasters[0].Entry : null;
            availability.BestCasterName = availability.BestEntry != null ? availability.BestEntry.CasterName : string.Empty;

            AddMissingUnits(action.CastTargetIds, action.CastTargetNames, availability.MissingTargets);
            AddMissingUnits(action.RecipientIds, action.RecipientNames, availability.MissingRecipients);

            availability.IsExecutable = availability.IsSpellAvailable
                && availability.MissingTargets.Count == 0
                && availability.MissingRecipients.Count == 0
                && HasRequiredTargets(action);

            if (!availability.IsSpellAvailable)
            {
                availability.Reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
            }
            else if (availability.MissingTargets.Count > 0 || availability.MissingRecipients.Count > 0)
            {
                availability.Reason = ModLocalization.T("Execution.Skip.TargetUnavailable");
            }
            else if (!HasRequiredTargets(action))
            {
                availability.Reason = ModLocalization.T("Execution.Skip.TargetUnavailable");
            }

            return availability;
        }

        internal static string FormatCandidateNames(IEnumerable<CasterCandidate> candidates)
        {
            List<string> names = new List<string>();
            foreach (CasterCandidate candidate in candidates ?? new List<CasterCandidate>())
            {
                if (!string.IsNullOrEmpty(candidate.DisplayName))
                {
                    names.Add(candidate.DisplayName);
                }
            }

            return UiHelpers.ListOrNone(names);
        }

        private static void AddMissingUnits(List<string> ids, List<string> names, List<string> missing)
        {
            if (ids == null)
            {
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                string name = names != null && i < names.Count ? names[i] : string.Empty;
                UnitEntityData unit = SpellCatalog.FindPartyUnit(id, name);
                if (unit == null)
                {
                    missing.Add(string.IsNullOrEmpty(name) ? id : name);
                }
            }
        }

        private static bool HasRequiredTargets(BuffQueueAction action)
        {
            if (action.TargetKind == TargetKind.Self
                || action.TargetKind == TargetKind.NoTarget
                || action.DeliveryKind == BuffDeliveryKind.Personal)
            {
                return true;
            }

            if (IsAreaDelivery(action.DeliveryKind))
            {
                return action.RecipientIds != null && action.RecipientIds.Count > 0;
            }

            return action.CastTargetIds != null && action.CastTargetIds.Count > 0;
        }

        private static bool IsAreaDelivery(BuffDeliveryKind deliveryKind)
        {
            return deliveryKind == BuffDeliveryKind.CasterCenteredArea
                || deliveryKind == BuffDeliveryKind.PointCenteredArea
                || deliveryKind == BuffDeliveryKind.SelectedUnitCenteredArea
                || deliveryKind == BuffDeliveryKind.WholeParty;
        }
    }
}
