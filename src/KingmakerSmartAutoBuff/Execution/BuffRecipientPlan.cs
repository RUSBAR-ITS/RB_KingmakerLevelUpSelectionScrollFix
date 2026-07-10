using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal sealed class BuffRecipientPlan
    {
        internal List<UnitEntityData> RecipientsNeedingBuff = new List<UnitEntityData>();
        internal List<UnitEntityData> RecipientsAlreadyBuffed = new List<UnitEntityData>();
        internal List<string> RecipientsUnavailable = new List<string>();
    }
}
