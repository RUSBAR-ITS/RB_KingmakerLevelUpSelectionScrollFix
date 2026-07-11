using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal sealed class BuffVerificationTracker
    {
        private const float InitialDelaySeconds = 0.35f;
        private const float RetryDelaySeconds = 0.25f;
        private const float TimeoutSeconds = 5f;

        private readonly List<BuffVerificationJob> m_Jobs = new List<BuffVerificationJob>();

        internal int PendingCount
        {
            get { return m_Jobs.Count; }
        }

        internal void Schedule(
            QueueActionReport reportAction,
            SpellCatalogEntry entry,
            List<UnitEntityData> expectedRecipients)
        {
            if (reportAction == null)
            {
                return;
            }

            if (entry == null || expectedRecipients == null || expectedRecipients.Count == 0)
            {
                reportAction.VerificationStatus = BuffVerificationStatus.NotRequired;
                return;
            }

            List<UnitEntityData> recipients = expectedRecipients.ToList();
            reportAction.VerificationStatus = BuffVerificationStatus.Pending;
            reportAction.ExpectedRecipientCount = recipients.Count;
            reportAction.CoveredRecipientCount = 0;
            reportAction.MissingRecipientNames.Clear();
            reportAction.UnavailableRecipientNames.Clear();
            reportAction.VerificationSeconds = 0f;

            m_Jobs.Add(new BuffVerificationJob
            {
                ReportAction = reportAction,
                Entry = entry,
                Profile = entry.BuffProfile,
                Recipients = recipients,
                NextCheckSeconds = InitialDelaySeconds
            });
        }

        internal void Update(float deltaTime)
        {
            float elapsed = Math.Max(0f, deltaTime);
            for (int i = m_Jobs.Count - 1; i >= 0; i--)
            {
                BuffVerificationJob job = m_Jobs[i];
                try
                {
                    if (UpdateJob(job, elapsed))
                    {
                        m_Jobs.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    MarkError(job);
                    m_Jobs.RemoveAt(i);
                    Logger.Exception("Background buff verification failed.", ex);
                }
            }
        }

        private static bool UpdateJob(BuffVerificationJob job, float deltaTime)
        {
            job.ElapsedSeconds += deltaTime;
            job.NextCheckSeconds -= deltaTime;
            if (job.NextCheckSeconds > 0f)
            {
                return false;
            }

            BuffApplicationResult result = BuffApplicationVerifier.VerifyRecipients(
                job.Entry,
                job.Profile,
                job.Recipients);

            int expected = job.Recipients.Count;
            bool verified = result.Covered.Count >= expected
                && result.Missing.Count == 0
                && result.Unavailable.Count == 0;
            if (verified)
            {
                CompleteJob(job, result, BuffVerificationStatus.Verified);
                Logger.Info(
                    "Buff verification ok. spell="
                    + job.Entry.SpellName
                    + ", covered="
                    + result.Covered.Count
                    + ".");
                return true;
            }

            if (job.ElapsedSeconds < TimeoutSeconds)
            {
                job.NextCheckSeconds = RetryDelaySeconds;
                return false;
            }

            BuffVerificationStatus status = ResolveFinalStatus(result);
            CompleteJob(job, result, status);
            Logger.Warning(
                "Buff verification completed with issues. spell="
                + (job.Entry != null ? job.Entry.SpellName : "<spell>")
                + ", status="
                + status
                + ", covered="
                + result.Covered.Count
                + "/"
                + expected
                + ", missing="
                + QueueActionResolver.FormatUnitList(result.Missing)
                + ", unavailable="
                + UiHelpers.ListOrNone(result.Unavailable)
                + ", waited="
                + job.ElapsedSeconds.ToString("0.##")
                + "s.");
            return true;
        }

        private static BuffVerificationStatus ResolveFinalStatus(BuffApplicationResult result)
        {
            if (result.Covered.Count > 0)
            {
                return BuffVerificationStatus.Partial;
            }

            if (result.Unavailable.Count > 0 && result.Missing.Count == 0)
            {
                return BuffVerificationStatus.Unavailable;
            }

            return BuffVerificationStatus.Missing;
        }

        private static void CompleteJob(
            BuffVerificationJob job,
            BuffApplicationResult result,
            BuffVerificationStatus status)
        {
            QueueActionReport report = job.ReportAction;
            report.VerificationStatus = status;
            report.ExpectedRecipientCount = job.Recipients.Count;
            report.CoveredRecipientCount = result.Covered.Count;
            report.MissingRecipientNames = result.Missing
                .Where(unit => unit != null)
                .Select(SpellCatalog.SafeUnitName)
                .ToList();
            report.UnavailableRecipientNames = new List<string>(result.Unavailable);
            report.VerificationSeconds = job.ElapsedSeconds;
        }

        private static void MarkError(BuffVerificationJob job)
        {
            if (job == null || job.ReportAction == null)
            {
                return;
            }

            job.ReportAction.VerificationStatus = BuffVerificationStatus.Error;
            job.ReportAction.VerificationSeconds = job.ElapsedSeconds;
        }
    }
}
