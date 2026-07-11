using System.Collections.Generic;
using System.Linq;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueExecutionReport
    {
        internal string QueueName = string.Empty;
        internal QueueExecutionMode Mode = QueueExecutionMode.Full;
        internal int TotalTasks;
        internal bool IsCastingFinished;
        internal bool WasStopped;
        internal string StopReason = string.Empty;
        internal bool FinalSummaryPublished;
        internal List<QueueActionReport> Actions = new List<QueueActionReport>();

        internal int CastCount
        {
            get { return Actions.Count(action => action.ExecutionStatus == QueueActionExecutionStatus.CastSucceeded); }
        }

        internal int SkipCount
        {
            get { return Actions.Count(action => action.ExecutionStatus == QueueActionExecutionStatus.Skipped); }
        }

        internal int FailCount
        {
            get { return Actions.Count(action => action.ExecutionStatus == QueueActionExecutionStatus.Failed); }
        }

        internal int PendingVerificationCount
        {
            get { return Actions.Count(action => action.VerificationStatus == BuffVerificationStatus.Pending); }
        }

        internal int VerifiedCount
        {
            get { return Actions.Count(action => action.VerificationStatus == BuffVerificationStatus.Verified); }
        }

        internal int PartialCount
        {
            get { return Actions.Count(action => action.VerificationStatus == BuffVerificationStatus.Partial); }
        }

        internal int MissingCount
        {
            get { return Actions.Count(action => action.VerificationStatus == BuffVerificationStatus.Missing); }
        }

        internal int UnavailableCount
        {
            get { return Actions.Count(action => action.VerificationStatus == BuffVerificationStatus.Unavailable); }
        }

        internal int VerificationErrorCount
        {
            get { return Actions.Count(action => action.VerificationStatus == BuffVerificationStatus.Error); }
        }

        internal int VerificationIssueCount
        {
            get { return PartialCount + MissingCount + UnavailableCount + VerificationErrorCount; }
        }

        internal bool IsSettled
        {
            get { return IsCastingFinished && PendingVerificationCount == 0; }
        }
    }
}
