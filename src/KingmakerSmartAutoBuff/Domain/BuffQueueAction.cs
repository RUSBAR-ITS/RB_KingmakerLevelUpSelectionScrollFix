using System.Collections.Generic;

namespace KingmakerSmartAutoBuff
{
    public sealed class BuffQueueAction
    {
        public string CasterId;
        public string CasterName;
        public string SpellbookId;
        public string SpellbookName;
        public string SpellBlueprintId;
        public int SpellLevel;
        public string SpellName;
        public List<string> Metamagic = new List<string>();
        public BuffDeliveryKind DeliveryKind;
        public string CastTargetId;
        public string CastTargetName;
        public List<string> RecipientIds = new List<string>();
        public List<string> RecipientNames = new List<string>();
    }
}
