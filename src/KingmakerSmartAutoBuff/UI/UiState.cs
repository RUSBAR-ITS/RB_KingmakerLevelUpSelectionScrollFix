using System.Collections.Generic;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal sealed class UiState
    {
        internal readonly HashSet<string> SelectedTargetIds = new HashSet<string>();

        internal List<CasterOption> Casters = new List<CasterOption>();
        internal List<SpellCatalogEntry> SpellEntries = new List<SpellCatalogEntry>();
        internal List<TargetOption> TargetOptions = new List<TargetOption>();

        internal Vector2 QueueListScroll;
        internal Vector2 SpellScroll;
        internal Vector2 QueueScroll;
        internal Vector2 DescriptionScroll;
        internal Vector2 EditScroll;
        internal Vector2 EditTargetsScroll;
        internal Vector2 ExecutionReportScroll;

        internal int SelectedQueueIndex;
        internal int SelectedCasterIndex;
        internal int SelectedSpellIndex = -1;
        internal int SelectedActionIndex = -1;
        internal int EditingActionIndex = -1;
        internal int LevelFilter = -1;
        internal BuffQueueAction EditingActionDraft;
        internal bool ShowExecutionReportDetails;

        internal string NewQueueName = "Daily buffs";
        internal string RenameQueueText = string.Empty;
        internal string Status = string.Empty;
        internal string ExecutionStatus = string.Empty;
    }
}
