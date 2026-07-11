using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueActionAvailability
    {
        internal bool IsExecutable;
        internal bool IsSpellAvailable;
        internal SpellCatalogEntry BestEntry;
        internal string BestCasterName = string.Empty;
        internal List<CasterCandidate> AvailableCasters = new List<CasterCandidate>();
        internal List<CasterCandidate> UnavailableCasters = new List<CasterCandidate>();
        internal List<string> MissingTargets = new List<string>();
        internal List<string> MissingRecipients = new List<string>();
        internal string Reason = string.Empty;
    }
}
