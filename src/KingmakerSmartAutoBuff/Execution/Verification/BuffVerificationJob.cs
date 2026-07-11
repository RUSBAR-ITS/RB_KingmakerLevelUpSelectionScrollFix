using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal sealed class BuffVerificationJob
    {
        internal QueueActionReport ReportAction;
        internal SpellCatalogEntry Entry;
        internal AbilityBuffProfile Profile;
        internal List<UnitEntityData> Recipients = new List<UnitEntityData>();
        internal float ElapsedSeconds;
        internal float NextCheckSeconds;
    }
}
