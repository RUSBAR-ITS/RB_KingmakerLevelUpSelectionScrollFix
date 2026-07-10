using System;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;

namespace KingmakerSmartAutoBuff
{
    internal static class CastCommandRunner
    {
        internal static bool TryRun(
            SpellCatalogEntry entry,
            TargetWrapper target,
            out UnitUseAbility command,
            out string reason)
        {
            command = null;
            reason = string.Empty;

            if (entry == null || entry.Caster == null || entry.Ability == null || target == null)
            {
                reason = ModLocalization.T("Execution.Skip.BadCommand");
                return false;
            }

            try
            {
                command = new UnitUseAbility(entry.Ability, target);
                entry.Caster.Commands.Run(command);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to start ability command.", ex);
                reason = ModLocalization.T("Execution.Skip.BadCommand");
                return false;
            }
        }
    }
}
