using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class MovementCommandRunner
    {
        internal static bool TryMoveTo(
            UnitEntityData unit,
            Vector3 point,
            float approachRadius,
            out UnitMoveTo command,
            out string reason)
        {
            command = null;
            reason = string.Empty;
            if (unit == null)
            {
                reason = ModLocalization.T("Movement.UnitMissing");
                return false;
            }

            try
            {
                command = new UnitMoveTo(point, approachRadius);
                command.MovementDelay = 0f;
                command.ShowTargetMarker = false;
                command.CreatedByPlayer = true;
                unit.Commands.Run(command);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to start movement for " + SpellCatalog.SafeUnitName(unit) + ".", ex);
                reason = ModLocalization.T("Movement.CommandFailed");
                return false;
            }
        }
    }
}
