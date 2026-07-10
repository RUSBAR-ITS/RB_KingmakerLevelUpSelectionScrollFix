using System.Collections.Generic;
using Kingmaker.UnitLogic.Commands;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueExecutionState
    {
        internal string QueueName = string.Empty;
        internal QueueExecutionMode Mode = QueueExecutionMode.Full;
        internal List<ResolvedCastTask> Tasks = new List<ResolvedCastTask>();
        internal int NextTaskIndex;
        internal UnitUseAbility CurrentCommand;
        internal ResolvedCastTask CurrentTask;
        internal SpellCatalogEntry CurrentEntry;
        internal List<Kingmaker.EntitySystem.Entities.UnitEntityData> CurrentExpectedRecipients;
        internal PartyGatherController CurrentGather;
        internal ResolvedCastTask PendingGatherTask;
        internal SpellCatalogEntry PendingGatherEntry;
        internal List<Kingmaker.EntitySystem.Entities.UnitEntityData> PendingGatherRecipients;
        internal float CurrentCommandTime;
        internal float DelayRemaining;
        internal int CastCount;
        internal int SkipCount;
        internal int FailCount;
        internal string LastMessage = string.Empty;

        internal int TotalTasks
        {
            get { return Tasks != null ? Tasks.Count : 0; }
        }

        internal int CompletedTasks
        {
            get { return System.Math.Min(NextTaskIndex, TotalTasks); }
        }
    }
}
