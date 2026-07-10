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

                tasks.Add(new ResolvedCastTask(CloneAction(action)));
            }

            return tasks;
        }

        private static BuffQueueAction CloneAction(BuffQueueAction action)
        {
            BuffQueueAction clone = new BuffQueueAction();
            clone.CasterId = action.CasterId;
            clone.CasterName = action.CasterName;
            clone.SpellbookId = action.SpellbookId;
            clone.SpellbookName = action.SpellbookName;
            clone.SpellBlueprintId = action.SpellBlueprintId;
            clone.SpellLevel = action.SpellLevel;
            clone.SpellName = action.SpellName;
            clone.Metamagic = action.Metamagic != null
                ? new List<string>(action.Metamagic)
                : new List<string>();
            clone.DeliveryKind = action.DeliveryKind;
            clone.CastTargetId = action.CastTargetId;
            clone.CastTargetName = action.CastTargetName;
            clone.RecipientIds = action.RecipientIds != null
                ? new List<string>(action.RecipientIds)
                : new List<string>();
            clone.RecipientNames = action.RecipientNames != null
                ? new List<string>(action.RecipientNames)
                : new List<string>();
            return clone;
        }
    }
}
