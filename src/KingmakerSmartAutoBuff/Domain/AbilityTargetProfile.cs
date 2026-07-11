using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace KingmakerSmartAutoBuff
{
    internal sealed class AbilityTargetProfile
    {
        internal TargetKind TargetKind = TargetKind.Unknown;
        internal bool CanTargetSelf;
        internal bool CanTargetFriends;
        internal bool CanTargetEnemies;
        internal bool CanTargetPoint;
        internal bool IsPointTarget;
        internal bool IsAreaTarget;
        internal bool IsHostile;
        internal bool IsFriendly;
        internal AbilityRange Range;
        internal float RadiusMeters;
    }
}
