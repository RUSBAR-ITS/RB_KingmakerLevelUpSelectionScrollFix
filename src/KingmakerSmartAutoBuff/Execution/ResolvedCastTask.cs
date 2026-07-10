namespace KingmakerSmartAutoBuff
{
    internal sealed class ResolvedCastTask
    {
        internal ResolvedCastTask(BuffQueueAction action, string targetId, string targetName)
        {
            Action = action;
            TargetId = targetId;
            TargetName = targetName;
        }

        internal BuffQueueAction Action;
        internal string TargetId;
        internal string TargetName;
    }
}
