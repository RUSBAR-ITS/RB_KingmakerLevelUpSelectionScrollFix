using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class GatherPointPlanner
    {
        internal static Dictionary<UnitEntityData, Vector3> BuildGatherPoints(
            UnitEntityData caster,
            List<UnitEntityData> recipients,
            float radiusMeters)
        {
            Dictionary<UnitEntityData, Vector3> result = new Dictionary<UnitEntityData, Vector3>();
            if (caster == null || recipients == null)
            {
                return result;
            }

            float ringRadius = Mathf.Clamp(radiusMeters * 0.5f, 1.5f, Mathf.Max(1.5f, radiusMeters - 1f));
            int count = Math.Max(1, recipients.Count);
            for (int i = 0; i < recipients.Count; i++)
            {
                UnitEntityData unit = recipients[i];
                if (unit == null || unit == caster)
                {
                    continue;
                }

                float angle = (Mathf.PI * 2f * i) / count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
                result[unit] = caster.Position + offset;
            }

            return result;
        }
    }
}
