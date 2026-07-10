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
        public TargetKind TargetKind;
        public List<string> TargetIds = new List<string>();
        public List<string> TargetNames = new List<string>();
    }
}
