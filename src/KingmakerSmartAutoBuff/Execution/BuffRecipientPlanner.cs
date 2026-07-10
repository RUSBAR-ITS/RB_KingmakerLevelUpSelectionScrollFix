using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class BuffRecipientPlanner
    {
        internal static BuffRecipientPlan Plan(
            SpellCatalogEntry entry,
            AbilityBuffProfile profile,
            List<UnitEntityData> selectedRecipients,
            QueueExecutionMode mode)
        {
            BuffRecipientPlan plan = new BuffRecipientPlan();
            if (selectedRecipients == null)
            {
                return plan;
            }

            foreach (UnitEntityData recipient in selectedRecipients)
            {
                if (recipient == null)
                {
                    plan.RecipientsUnavailable.Add("<missing>");
                    continue;
                }

                if (mode == QueueExecutionMode.Smart)
                {
                    ActiveBuffInfo matchedBuff;
                    if (ActiveBuffHelper.HasAnyProfileBuff(recipient, profile, entry, out matchedBuff))
                    {
                        plan.RecipientsAlreadyBuffed.Add(recipient);
                        continue;
                    }
                }

                plan.RecipientsNeedingBuff.Add(recipient);
            }

            return plan;
        }
    }
}
