using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal sealed class AbilityBuffProfile
    {
        internal BuffDeliveryKind DeliveryKind = BuffDeliveryKind.Unknown;
        internal bool IsFriendlyBuff;
        internal bool IsAreaBuff;
        internal bool CanUseCasterAsAnchor;
        internal float RadiusMeters;
        internal List<string> AppliedBuffBlueprintIds = new List<string>();
        internal List<string> AppliedBuffNames = new List<string>();
        internal List<string> Diagnostics = new List<string>();

        internal bool HasAppliedBuffs
        {
            get { return AppliedBuffBlueprintIds != null && AppliedBuffBlueprintIds.Count > 0; }
        }
    }
}
