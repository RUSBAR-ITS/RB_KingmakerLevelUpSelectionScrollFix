using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class UnitMovementCapability
    {
        internal static bool CanGatherUnit(UnitEntityData unit, out string reason)
        {
            reason = string.Empty;
            if (unit == null)
            {
                reason = ModLocalization.T("Movement.UnitMissing");
                return false;
            }

            try
            {
                if (!unit.IsInGame || unit.Descriptor == null || unit.Descriptor.State == null)
                {
                    reason = ModLocalization.T("Movement.UnitUnavailable");
                    return false;
                }

                if (unit.Descriptor.State.IsDead || unit.Descriptor.State.IsUnconscious)
                {
                    reason = ModLocalization.T("Movement.UnitCannotMove");
                    return false;
                }

                if (!unit.Descriptor.State.CanMove || unit.AiMovementForbidden)
                {
                    reason = ModLocalization.T("Movement.UnitCannotMove");
                    return false;
                }

                if (unit.CurrentSpeedMps <= 0.01f)
                {
                    reason = ModLocalization.T("Movement.UnitCannotMove");
                    return false;
                }
            }
            catch
            {
                reason = ModLocalization.T("Movement.UnitUnavailable");
                return false;
            }

            return true;
        }
    }
}
