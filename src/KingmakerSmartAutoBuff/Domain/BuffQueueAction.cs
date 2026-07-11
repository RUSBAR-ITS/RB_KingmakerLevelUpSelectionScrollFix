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
        public string SpellVariantId;
        public int SpellLevel;
        public string SpellName;
        public List<string> Metamagic = new List<string>();
        public TargetKind TargetKind;
        public BuffDeliveryKind DeliveryKind;
        public List<QueueCasterReference> CandidateCasters = new List<QueueCasterReference>();
        public List<string> CastTargetIds = new List<string>();
        public List<string> CastTargetNames = new List<string>();
        public string CastTargetId;
        public string CastTargetName;
        public List<string> RecipientIds = new List<string>();
        public List<string> RecipientNames = new List<string>();
    }
}
