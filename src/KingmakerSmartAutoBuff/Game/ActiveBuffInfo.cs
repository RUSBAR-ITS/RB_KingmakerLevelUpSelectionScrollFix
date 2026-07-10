using Kingmaker.UnitLogic.Buffs;

namespace KingmakerSmartAutoBuff
{
    internal sealed class ActiveBuffInfo
    {
        internal Buff Buff;
        internal string BuffName = string.Empty;
        internal string BuffBlueprintId = string.Empty;
        internal string SourceAbilityId = string.Empty;
        internal string SourceAbilityName = string.Empty;

        internal string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(BuffName))
                {
                    return BuffName;
                }

                if (!string.IsNullOrEmpty(SourceAbilityName))
                {
                    return SourceAbilityName;
                }

                return ModLocalization.T("Common.None");
            }
        }
    }
}
