using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueExecutionReportController
    {
        private const int RetainedReportCount = 5;

        private readonly List<QueueExecutionReport> m_Reports = new List<QueueExecutionReport>();

        internal QueueExecutionReport LatestReport
        {
            get { return m_Reports.Count > 0 ? m_Reports[0] : null; }
        }

        internal QueueExecutionReport Create(
            string queueName,
            QueueExecutionMode mode,
            List<ResolvedCastTask> tasks)
        {
            QueueExecutionReport report = new QueueExecutionReport
            {
                QueueName = queueName ?? string.Empty,
                Mode = mode,
                TotalTasks = tasks != null ? tasks.Count : 0
            };

            if (tasks != null)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    ResolvedCastTask task = tasks[i];
                    BuffQueueAction action = task != null ? task.Action : null;
                    QueueActionReport actionReport = new QueueActionReport
                    {
                        Sequence = i + 1,
                        CasterName = action != null ? action.CasterName ?? string.Empty : string.Empty,
                        SpellName = action != null ? action.SpellName ?? string.Empty : string.Empty,
                        RecipientNames = action != null && action.RecipientNames != null
                            ? new List<string>(action.RecipientNames)
                            : new List<string>()
                    };

                    report.Actions.Add(actionReport);
                    if (task != null)
                    {
                        task.ReportAction = actionReport;
                    }
                }
            }

            m_Reports.Insert(0, report);
            TrimSettledReports();
            return report;
        }

        internal void Update(bool executorRunning)
        {
            foreach (QueueExecutionReport report in m_Reports)
            {
                if (report == null || !report.IsSettled || report.FinalSummaryPublished)
                {
                    continue;
                }

                report.FinalSummaryPublished = true;
                LogFinalSummary(report);

                if (ReferenceEquals(report, LatestReport)
                    && !report.WasStopped
                    && !executorRunning
                    && Main.Ui != null)
                {
                    Main.Ui.State.ExecutionStatus = string.Format(
                        ModLocalization.T("Execution.Status.ReportCompleted"),
                        report.QueueName,
                        report.CastCount,
                        report.SkipCount,
                        report.FailCount,
                        report.VerifiedCount,
                        report.VerificationIssueCount);
                }
            }

            TrimSettledReports();
        }

        private static void LogFinalSummary(QueueExecutionReport report)
        {
            Logger.Info(
                "Execution report finalized. queue="
                + report.QueueName
                + ", cast="
                + report.CastCount
                + ", skipped="
                + report.SkipCount
                + ", failed="
                + report.FailCount
                + ", verified="
                + report.VerifiedCount
                + ", partial="
                + report.PartialCount
                + ", missing="
                + report.MissingCount
                + ", unavailable="
                + report.UnavailableCount
                + ", verificationErrors="
                + report.VerificationErrorCount
                + ".");
        }

        private void TrimSettledReports()
        {
            for (int i = m_Reports.Count - 1; i >= RetainedReportCount; i--)
            {
                QueueExecutionReport report = m_Reports[i];
                if (report != null && report.IsSettled)
                {
                    m_Reports.RemoveAt(i);
                }
            }
        }
    }
}
