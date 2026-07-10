using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using UnityEngine;

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

        internal static bool TryRunAtPoint(
            SpellCatalogEntry entry,
            Vector3 point,
            out UnitUseAbility command,
            out string reason)
        {
            command = null;
            reason = string.Empty;

            if (entry == null || entry.Caster == null || entry.Ability == null)
            {
                reason = ModLocalization.T("Execution.Skip.BadCommand");
                return false;
            }

            try
            {
                TargetWrapper target = new TargetWrapper(point, SafeOrientation(entry.Caster));
                command = new UnitUseAbility(entry.Ability, target);
                entry.Caster.Commands.Run(command);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to start point ability command.", ex);
                reason = ModLocalization.T("Execution.Skip.BadCommand");
                return false;
            }
        }

        private static float SafeOrientation(UnitEntityData unit)
        {
            try
            {
                return unit != null ? unit.Orientation : 0f;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
