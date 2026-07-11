namespace KingmakerSmartAutoBuff
{
    internal sealed class CasterCandidate
    {
        internal QueueCasterReference Reference;
        internal SpellCatalogEntry Entry;
        internal bool IsAvailable;
        internal string Reason = string.Empty;
        internal int CasterLevel;
        internal int CastingAttributeValue;
        internal string DisplayName
        {
            get
            {
                if (Entry != null)
                {
                    return Entry.CasterName;
                }

                return Reference != null ? Reference.CasterName : string.Empty;
            }
        }
    }
}
