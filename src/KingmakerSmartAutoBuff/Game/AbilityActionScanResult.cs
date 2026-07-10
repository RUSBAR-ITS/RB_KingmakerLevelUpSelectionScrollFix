using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    internal sealed class AbilityActionScanResult
    {
        internal bool HasPartyMembersAction;
        internal List<string> AppliedBuffBlueprintIds = new List<string>();
        internal List<string> AppliedBuffNames = new List<string>();
        internal List<string> Diagnostics = new List<string>();
    }
}
