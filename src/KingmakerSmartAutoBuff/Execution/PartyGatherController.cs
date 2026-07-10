using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal sealed class PartyGatherController
    {
        private const float DefaultTimeoutSeconds = 12f;
        private const float RadiusMarginMeters = 0.75f;
        private const float ApproachRadiusMeters = 0.4f;

        private readonly UnitEntityData m_Caster;
        private readonly float m_RadiusMeters;
        private readonly List<MovementTrack> m_Tracks = new List<MovementTrack>();
        private float m_Elapsed;
        private bool m_IsFinished;

        internal PartyGatherController(UnitEntityData caster, List<UnitEntityData> recipients, float radiusMeters)
        {
            m_Caster = caster;
            m_RadiusMeters = Mathf.Max(0f, radiusMeters);
            Start(recipients ?? new List<UnitEntityData>());
        }

        internal bool IsFinished
        {
            get { return m_IsFinished; }
        }

        internal List<UnitEntityData> ArrivedRecipients
        {
            get
            {
                return m_Tracks
                    .Where(track => track.Status == MovementTrackStatus.Arrived)
                    .Select(track => track.Unit)
                    .Where(unit => unit != null)
                    .ToList();
            }
        }

        internal void Update(float deltaTime)
        {
            if (m_IsFinished)
            {
                return;
            }

            m_Elapsed += deltaTime;
            foreach (MovementTrack track in m_Tracks)
            {
                MovementTracker.Update(track, m_Caster, m_RadiusMeters, RadiusMarginMeters);
            }

            if (m_Elapsed >= DefaultTimeoutSeconds)
            {
                foreach (MovementTrack track in m_Tracks)
                {
                    MovementTracker.MarkTimedOut(track);
                }
            }

            m_IsFinished = m_Tracks.All(track => track.Status != MovementTrackStatus.Running);
        }

        internal string Summary()
        {
            int arrived = m_Tracks.Count(track => track.Status == MovementTrackStatus.Arrived);
            int skipped = m_Tracks.Count(track => track.Status == MovementTrackStatus.Skipped);
            int failed = m_Tracks.Count - arrived - skipped;
            return "arrived=" + arrived + ", skipped=" + skipped + ", failed=" + failed;
        }

        private void Start(List<UnitEntityData> recipients)
        {
            if (m_Caster == null || recipients.Count == 0 || m_RadiusMeters <= 0.01f)
            {
                m_IsFinished = true;
                return;
            }

            Dictionary<UnitEntityData, Vector3> points = GatherPointPlanner.BuildGatherPoints(m_Caster, recipients, m_RadiusMeters);
            foreach (UnitEntityData recipient in recipients)
            {
                MovementTrack track = new MovementTrack();
                track.Unit = recipient;
                m_Tracks.Add(track);

                if (recipient == null)
                {
                    track.Status = MovementTrackStatus.Skipped;
                    track.Reason = ModLocalization.T("Movement.UnitMissing");
                    continue;
                }

                if (recipient == m_Caster || UnitDistanceHelper.IsWithinRadius(m_Caster, recipient, m_RadiusMeters, RadiusMarginMeters))
                {
                    track.Status = MovementTrackStatus.Arrived;
                    continue;
                }

                string reason;
                if (!UnitMovementCapability.CanGatherUnit(recipient, out reason))
                {
                    track.Status = MovementTrackStatus.Skipped;
                    track.Reason = reason;
                    Logger.Info("Gather skipped. unit=" + SpellCatalog.SafeUnitName(recipient) + ", reason=" + reason + ".");
                    continue;
                }

                Vector3 point;
                if (!points.TryGetValue(recipient, out point))
                {
                    point = m_Caster.Position;
                }

                track.TargetPoint = point;
                Kingmaker.UnitLogic.Commands.UnitMoveTo command;
                if (!MovementCommandRunner.TryMoveTo(recipient, point, ApproachRadiusMeters, out command, out reason))
                {
                    track.Status = MovementTrackStatus.Failed;
                    track.Reason = reason;
                    continue;
                }

                track.Command = command;
                track.Status = MovementTrackStatus.Running;
                Logger.Info("Gather moving. unit=" + SpellCatalog.SafeUnitName(recipient) + ", caster=" + SpellCatalog.SafeUnitName(m_Caster) + ".");
            }

            m_IsFinished = m_Tracks.All(track => track.Status != MovementTrackStatus.Running);
        }
    }
}
