using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerSmartAutoBuff
{
    internal sealed class CasterOption
    {
        internal UnitEntityData Unit;
        internal string Id;
        internal string Name;
        internal int MaxSpellLevel;
    }

    internal sealed class SpellCatalogEntry
    {
        internal UnitEntityData Caster;
        internal Spellbook Spellbook;
        internal AbilityData Ability;
        internal string CasterId;
        internal string CasterName;
        internal string SpellbookId;
        internal string SpellbookName;
        internal string SpellBlueprintId;
        internal string SpellVariantId;
        internal string SpellName;
        internal string Description;
        internal int SpellLevel;
        internal string MetamagicText;
        internal List<string> MetamagicNames = new List<string>();
        internal string TargetSummary;
        internal TargetKind TargetKind;
        internal AbilityTargetProfile TargetProfile;
        internal AbilityBuffProfile BuffProfile;
        internal int AvailableCasts;
    }

    internal sealed class TargetOption
    {
        internal UnitEntityData Unit;
        internal string Id;
        internal string Name;
    }
}
