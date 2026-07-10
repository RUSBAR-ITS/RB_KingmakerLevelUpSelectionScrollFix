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

                if (action.TargetKind == TargetKind.Self
                    || action.TargetKind == TargetKind.NoTarget
                    || action.TargetIds == null
                    || action.TargetIds.Count == 0)
                {
                    tasks.Add(new ResolvedCastTask(CloneAction(action), string.Empty, string.Empty));
                    continue;
                }

                for (int i = 0; i < action.TargetIds.Count; i++)
                {
                    string targetId = action.TargetIds[i];
                    string targetName = action.TargetNames != null && i < action.TargetNames.Count
                        ? action.TargetNames[i]
                        : string.Empty;

                    tasks.Add(new ResolvedCastTask(CloneAction(action), targetId, targetName));
                }
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
            clone.TargetKind = action.TargetKind;
            clone.TargetIds = action.TargetIds != null
                ? new List<string>(action.TargetIds)
                : new List<string>();
            clone.TargetNames = action.TargetNames != null
                ? new List<string>(action.TargetNames)
                : new List<string>();
            return clone;
        }
    }
}
