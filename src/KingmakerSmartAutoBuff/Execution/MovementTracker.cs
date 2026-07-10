using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;

namespace KingmakerSmartAutoBuff
{
    internal static class MovementTracker
    {
        internal static void Update(MovementTrack track, UnitEntityData center, float radiusMeters, float margin)
        {
            if (track == null || track.Status != MovementTrackStatus.Running)
            {
                return;
            }

            if (track.Unit == null)
            {
                track.Status = MovementTrackStatus.Failed;
                track.Reason = ModLocalization.T("Movement.UnitMissing");
                return;
            }

            if (UnitDistanceHelper.IsWithinRadius(center, track.Unit, radiusMeters, margin))
            {
                InterruptMove(track);
                track.Status = MovementTrackStatus.Arrived;
                return;
            }

            if (track.Command == null)
            {
                track.Status = MovementTrackStatus.Failed;
                track.Reason = ModLocalization.T("Movement.CommandFailed");
                return;
            }

            if (!track.Command.IsFinished)
            {
                return;
            }

            if (track.Command.Result == UnitCommand.ResultType.Success)
            {
                track.Status = UnitDistanceHelper.IsWithinRadius(center, track.Unit, radiusMeters, margin)
                    ? MovementTrackStatus.Arrived
                    : MovementTrackStatus.Failed;
                track.Reason = track.Status == MovementTrackStatus.Arrived
                    ? string.Empty
                    : ModLocalization.T("Movement.NotInRadius");
                return;
            }

            track.Status = track.Command.Result == UnitCommand.ResultType.Interrupt
                ? MovementTrackStatus.Interrupted
                : MovementTrackStatus.Failed;
            track.Reason = track.Command.Result.ToString();
        }

        internal static void MarkTimedOut(MovementTrack track)
        {
            if (track == null || track.Status != MovementTrackStatus.Running)
            {
                return;
            }

            InterruptMove(track);
            track.Status = MovementTrackStatus.TimedOut;
            track.Reason = ModLocalization.T("Movement.TimedOut");
        }

        private static void InterruptMove(MovementTrack track)
        {
            try
            {
                if (track.Command != null && !track.Command.IsFinished)
                {
                    track.Command.Interrupt(false);
                }
            }
            catch
            {
            }
        }
    }
}
