using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueTestRunner
    {
        internal static void TestRun(QueueFile file)
        {
            if (file == null || file.Queue == null)
            {
                return;
            }

            Logger.Info("Test run started. queue=" + file.Queue.Name + ", actions=" + file.Queue.Actions.Count + ".");

            foreach (BuffQueueAction action in file.Queue.Actions)
            {
                QueueActionAvailability availability = QueueActionAvailabilityEvaluator.Evaluate(action);
                SpellCatalogEntry currentEntry = availability.BestEntry;
                if (currentEntry == null)
                {
                    Logger.Info(
                        "Would skip: spell is no longer available. caster="
                        + QueueActionAvailabilityEvaluator.FormatCandidateNames(availability.UnavailableCasters)
                        + ", spell="
                        + action.SpellName
                        + ", metamagic="
                        + MetamagicLocalization.ListOrNone(action.Metamagic)
                        + ".");
                    continue;
                }

                List<string> currentRecipients = ResolveCurrentRecipientNames(action);
                if (RequiresRecipients(currentEntry) && currentRecipients.Count == 0)
                {
                    Logger.Info(
                        "Would skip: no selected recipient is currently available. caster="
                        + currentEntry.CasterName
                        + ", spell="
                        + action.SpellName
                        + ".");
                    continue;
                }

                Logger.Info(
                    "Would cast: caster="
                    + currentEntry.CasterName
                    + ", candidates="
                    + QueueActionAvailabilityEvaluator.FormatCandidateNames(availability.AvailableCasters)
                    + ", spell="
                    + currentEntry.SpellName
                    + ", metamagic="
                    + currentEntry.MetamagicText
                    + ", delivery="
                    + currentEntry.BuffProfile.DeliveryKind
                    + ", recipients="
                    + UiHelpers.ListOrNone(currentRecipients)
                    + ".");
            }
        }

        private static bool RequiresRecipients(SpellCatalogEntry entry)
        {
            AbilityBuffProfile profile = entry != null ? entry.BuffProfile : null;
            return profile != null
                && profile.DeliveryKind != BuffDeliveryKind.Unsupported
                && profile.DeliveryKind != BuffDeliveryKind.Unknown;
        }

        private static List<string> ResolveCurrentRecipientNames(BuffQueueAction action)
        {
            List<string> result = new List<string>();

            if (action.RecipientIds == null || action.RecipientIds.Count == 0)
            {
                if (!string.IsNullOrEmpty(action.CastTargetName))
                {
                    result.Add(action.CastTargetName);
                }

                return result;
            }

            for (int i = 0; i < action.RecipientIds.Count; i++)
            {
                string recipientId = action.RecipientIds[i];
                string recipientName = i < action.RecipientNames.Count ? action.RecipientNames[i] : string.Empty;
                Kingmaker.EntitySystem.Entities.UnitEntityData unit = SpellCatalog.FindPartyUnit(recipientId, recipientName);
                if (unit != null)
                {
                    result.Add(SpellCatalog.SafeUnitName(unit));
                }
                else
                {
                    Logger.Info(
                        "Would skip recipient: recipient is no longer available. caster="
                        + action.CasterName
                        + ", spell="
                        + action.SpellName
                        + ", recipient="
                        + recipientName
                        + ".");
                }
            }

            return result;
        }
    }
}
