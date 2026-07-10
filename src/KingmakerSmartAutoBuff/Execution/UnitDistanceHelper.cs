using Kingmaker.EntitySystem.Entities;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class UnitDistanceHelper
    {
        internal static float DistanceMeters(UnitEntityData a, UnitEntityData b)
        {
            if (a == null || b == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(a.Position, b.Position);
        }

        internal static float EffectiveDistanceMeters(UnitEntityData a, UnitEntityData b)
        {
            if (a == null || b == null)
            {
                return float.MaxValue;
            }

            return Mathf.Max(0f, DistanceMeters(a, b) - SafeCorpulence(a) - SafeCorpulence(b));
        }

        internal static bool IsWithinRadius(UnitEntityData center, UnitEntityData target, float radiusMeters, float margin)
        {
            return EffectiveDistanceMeters(center, target) <= Mathf.Max(0f, radiusMeters - margin);
        }

        private static float SafeCorpulence(UnitEntityData unit)
        {
            try
            {
                return unit != null ? unit.Corpulence : 0f;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
