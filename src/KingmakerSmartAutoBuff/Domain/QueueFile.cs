namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueFile
    {
        internal QueueFile(string path, BuffQueueDefinition queue)
        {
            Path = path;
            Queue = queue;
        }

        internal string Path;
        internal BuffQueueDefinition Queue;
    }
}
