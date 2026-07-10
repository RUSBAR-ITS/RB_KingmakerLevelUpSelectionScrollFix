using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal sealed class MovementTrack
    {
        internal UnitEntityData Unit;
        internal Vector3 TargetPoint;
        internal UnitMoveTo Command;
        internal MovementTrackStatus Status;
        internal string Reason = string.Empty;
    }
}
