using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
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
        internal List<UnitEntityData> PendingGatherRecipients;
        internal SpellCatalogEntry PendingVerificationEntry;
        internal List<UnitEntityData> PendingVerificationRecipients;
        internal float PendingVerificationElapsed;
        internal float PendingVerificationNextCheck;
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
