using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueActionReport
    {
        internal int Sequence;
        internal string CasterName = string.Empty;
        internal string SpellName = string.Empty;
        internal List<string> RecipientNames = new List<string>();
        internal QueueActionExecutionStatus ExecutionStatus = QueueActionExecutionStatus.NotRun;
        internal string ExecutionMessage = string.Empty;
        internal BuffVerificationStatus VerificationStatus = BuffVerificationStatus.NotRequired;
        internal int ExpectedRecipientCount;
        internal int CoveredRecipientCount;
        internal List<string> MissingRecipientNames = new List<string>();
        internal List<string> UnavailableRecipientNames = new List<string>();
        internal float VerificationSeconds;
    }
}
