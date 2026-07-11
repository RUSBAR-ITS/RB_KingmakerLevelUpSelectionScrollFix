using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueActionCloner
    {
        internal static BuffQueueAction Clone(BuffQueueAction action)
        {
            BuffQueueAction clone = new BuffQueueAction();
            if (action == null)
            {
                return clone;
            }

            clone.CasterId = action.CasterId;
            clone.CasterName = action.CasterName;
            clone.SpellbookId = action.SpellbookId;
            clone.SpellbookName = action.SpellbookName;
            clone.SpellBlueprintId = action.SpellBlueprintId;
            clone.SpellVariantId = action.SpellVariantId;
            clone.SpellLevel = action.SpellLevel;
            clone.SpellName = action.SpellName;
            clone.Metamagic = action.Metamagic != null ? new List<string>(action.Metamagic) : new List<string>();
            clone.TargetKind = action.TargetKind;
            clone.DeliveryKind = action.DeliveryKind;
            clone.CandidateCasters = CloneCandidates(action.CandidateCasters);
            clone.CastTargetIds = action.CastTargetIds != null ? new List<string>(action.CastTargetIds) : new List<string>();
            clone.CastTargetNames = action.CastTargetNames != null ? new List<string>(action.CastTargetNames) : new List<string>();
            clone.CastTargetId = action.CastTargetId;
            clone.CastTargetName = action.CastTargetName;
            clone.RecipientIds = action.RecipientIds != null ? new List<string>(action.RecipientIds) : new List<string>();
            clone.RecipientNames = action.RecipientNames != null ? new List<string>(action.RecipientNames) : new List<string>();
            return clone;
        }

        private static List<QueueCasterReference> CloneCandidates(List<QueueCasterReference> candidates)
        {
            List<QueueCasterReference> result = new List<QueueCasterReference>();
            if (candidates == null)
            {
                return result;
            }

            foreach (QueueCasterReference candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                result.Add(new QueueCasterReference
                {
                    CasterId = candidate.CasterId,
                    CasterName = candidate.CasterName,
                    SpellbookId = candidate.SpellbookId,
                    SpellbookName = candidate.SpellbookName
                });
            }

            return result;
        }
    }
}
