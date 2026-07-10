namespace KingmakerSmartAutoBuff
{
    internal sealed class ResolvedCastTask
    {
        internal ResolvedCastTask(BuffQueueAction action)
        {
            Action = action;
        }

        internal BuffQueueAction Action;
    }
}
