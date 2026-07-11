using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueExecutionPlanner
    {
        internal static List<ResolvedCastTask> BuildTasks(QueueFile file)
        {
            List<ResolvedCastTask> tasks = new List<ResolvedCastTask>();
            if (file == null || file.Queue == null || file.Queue.Actions == null)
            {
                return tasks;
            }

            foreach (BuffQueueAction action in file.Queue.Actions)
            {
                if (action == null)
                {
                    continue;
                }

                foreach (BuffQueueAction expanded in ExpandAction(action))
                {
                    tasks.Add(new ResolvedCastTask(expanded));
                }
            }

            return tasks;
        }

        private static IEnumerable<BuffQueueAction> ExpandAction(BuffQueueAction action)
        {
            if (action == null)
            {
                yield break;
            }

            if (IsSelfOrAreaAction(action) || action.CastTargetIds == null || action.CastTargetIds.Count <= 1)
            {
                BuffQueueAction clone = QueueActionCloner.Clone(action);
                if ((string.IsNullOrEmpty(clone.CastTargetId) || string.IsNullOrEmpty(clone.CastTargetName))
                    && clone.CastTargetIds != null
                    && clone.CastTargetIds.Count == 1)
                {
                    clone.CastTargetId = clone.CastTargetIds[0];
                    clone.CastTargetName = clone.CastTargetNames != null && clone.CastTargetNames.Count > 0
                        ? clone.CastTargetNames[0]
                        : string.Empty;
                }

                yield return clone;
                yield break;
            }

            for (int i = 0; i < action.CastTargetIds.Count; i++)
            {
                BuffQueueAction clone = QueueActionCloner.Clone(action);
                clone.CastTargetId = action.CastTargetIds[i];
                clone.CastTargetName = action.CastTargetNames != null && i < action.CastTargetNames.Count
                    ? action.CastTargetNames[i]
                    : string.Empty;
                clone.CastTargetIds = new List<string> { clone.CastTargetId };
                clone.CastTargetNames = new List<string> { clone.CastTargetName };
                clone.RecipientIds = new List<string> { clone.CastTargetId };
                clone.RecipientNames = new List<string> { clone.CastTargetName };
                yield return clone;
            }
        }

        private static bool IsSelfOrAreaAction(BuffQueueAction action)
        {
            if (action.TargetKind == TargetKind.Self || action.DeliveryKind == BuffDeliveryKind.Personal)
            {
                return true;
            }

            return action.DeliveryKind == BuffDeliveryKind.CasterCenteredArea
                || action.DeliveryKind == BuffDeliveryKind.PointCenteredArea
                || action.DeliveryKind == BuffDeliveryKind.SelectedUnitCenteredArea
                || action.DeliveryKind == BuffDeliveryKind.WholeParty;
        }

    }
}
