using System.Collections.Generic;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class ExecutionReportPanel
    {
        private static readonly Color WarningColor = new Color(1f, 0.75f, 0.3f);
        private static readonly Color ErrorColor = new Color(1f, 0.45f, 0.45f);

        internal static void Draw(SmartAutoBuffUi ui)
        {
            QueueExecutionReport report = Main.Executor != null ? Main.Executor.LatestReport : null;
            if (report == null)
            {
                return;
            }

            UiState state = ui.State;
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                ModLocalization.T("Report.Title")
                + ": "
                + report.QueueName
                + " ("
                + FormatMode(report.Mode)
                + ")");

            string detailsButton = state.ShowExecutionReportDetails
                ? ModLocalization.T("Report.HideDetails")
                : ModLocalization.T("Report.ShowDetails");
            if (GUILayout.Button(detailsButton, GUILayout.Width(UiLayout.MediumButtonWidth)))
            {
                state.ShowExecutionReportDetails = !state.ShowExecutionReportDetails;
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(FormatReportState(report));
            UiHelpers.ColoredWrappedLabel(
                FormatSummary(report),
                report.VerificationIssueCount > 0 || report.FailCount > 0 ? ErrorColor : GUI.color);

            if (state.ShowExecutionReportDetails)
            {
                state.ExecutionReportScroll = GUILayout.BeginScrollView(
                    state.ExecutionReportScroll,
                    GUILayout.Height(UiLayout.ExecutionReportHeight));
                foreach (QueueActionReport action in report.Actions)
                {
                    UiHelpers.ColoredWrappedLabel(FormatAction(action), ResolveActionColor(action));
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private static string FormatMode(QueueExecutionMode mode)
        {
            return mode == QueueExecutionMode.Smart
                ? ModLocalization.T("Report.Mode.Smart")
                : ModLocalization.T("Report.Mode.Full");
        }

        private static string FormatReportState(QueueExecutionReport report)
        {
            if (report.WasStopped)
            {
                return ModLocalization.T("Report.State.Stopped");
            }

            if (!report.IsCastingFinished)
            {
                return ModLocalization.T("Report.State.Running");
            }

            if (report.PendingVerificationCount > 0)
            {
                return ModLocalization.T("Report.State.Verifying");
            }

            return ModLocalization.T("Report.State.Completed");
        }

        private static string FormatSummary(QueueExecutionReport report)
        {
            return string.Format(
                ModLocalization.T("Report.Summary"),
                report.CastCount,
                report.SkipCount,
                report.FailCount,
                report.VerifiedCount,
                report.PartialCount,
                report.MissingCount,
                report.UnavailableCount,
                report.VerificationErrorCount,
                report.PendingVerificationCount);
        }

        private static string FormatAction(QueueActionReport action)
        {
            string recipients = UiHelpers.ListOrNone(action.RecipientNames);
            string line = string.Format(
                ModLocalization.T("Report.Action.Line"),
                action.Sequence,
                string.IsNullOrEmpty(action.CasterName) ? ModLocalization.T("Common.None") : action.CasterName,
                string.IsNullOrEmpty(action.SpellName) ? ModLocalization.T("Common.None") : action.SpellName,
                recipients,
                FormatExecutionStatus(action),
                FormatVerificationStatus(action));

            if (!string.IsNullOrEmpty(action.ExecutionMessage))
            {
                line += " " + string.Format(ModLocalization.T("Report.Reason"), action.ExecutionMessage);
            }

            if (action.MissingRecipientNames.Count > 0)
            {
                line += " " + string.Format(
                    ModLocalization.T("Report.MissingRecipients"),
                    UiHelpers.ListOrNone(action.MissingRecipientNames));
            }

            if (action.UnavailableRecipientNames.Count > 0)
            {
                line += " " + string.Format(
                    ModLocalization.T("Report.UnavailableRecipients"),
                    UiHelpers.ListOrNone(action.UnavailableRecipientNames));
            }

            return line;
        }

        private static string FormatExecutionStatus(QueueActionReport action)
        {
            switch (action.ExecutionStatus)
            {
                case QueueActionExecutionStatus.Casting:
                    return ModLocalization.T("Report.Execution.Casting");
                case QueueActionExecutionStatus.CastSucceeded:
                    return ModLocalization.T("Report.Execution.CastSucceeded");
                case QueueActionExecutionStatus.Skipped:
                    return ModLocalization.T("Report.Execution.Skipped");
                case QueueActionExecutionStatus.Failed:
                    return ModLocalization.T("Report.Execution.Failed");
                case QueueActionExecutionStatus.Stopped:
                    return ModLocalization.T("Report.Execution.Stopped");
                default:
                    return ModLocalization.T("Report.Execution.NotRun");
            }
        }

        private static string FormatVerificationStatus(QueueActionReport action)
        {
            switch (action.VerificationStatus)
            {
                case BuffVerificationStatus.Pending:
                    return ModLocalization.T("Report.Verification.Pending");
                case BuffVerificationStatus.Verified:
                    return FormatCoverage("Report.Verification.Verified", action);
                case BuffVerificationStatus.Partial:
                    return FormatCoverage("Report.Verification.Partial", action);
                case BuffVerificationStatus.Missing:
                    return FormatCoverage("Report.Verification.Missing", action);
                case BuffVerificationStatus.Unavailable:
                    return FormatCoverage("Report.Verification.Unavailable", action);
                case BuffVerificationStatus.Error:
                    return ModLocalization.T("Report.Verification.Error");
                default:
                    return ModLocalization.T("Report.Verification.NotRequired");
            }
        }

        private static string FormatCoverage(string key, QueueActionReport action)
        {
            return string.Format(
                ModLocalization.T(key),
                action.CoveredRecipientCount,
                action.ExpectedRecipientCount);
        }

        private static Color ResolveActionColor(QueueActionReport action)
        {
            if (action.ExecutionStatus == QueueActionExecutionStatus.Failed
                || action.VerificationStatus == BuffVerificationStatus.Missing
                || action.VerificationStatus == BuffVerificationStatus.Unavailable
                || action.VerificationStatus == BuffVerificationStatus.Error)
            {
                return ErrorColor;
            }

            if (action.ExecutionStatus == QueueActionExecutionStatus.Skipped
                || action.ExecutionStatus == QueueActionExecutionStatus.Stopped
                || action.VerificationStatus == BuffVerificationStatus.Partial
                || action.VerificationStatus == BuffVerificationStatus.Pending)
            {
                return WarningColor;
            }

            return GUI.color;
        }
    }
}
