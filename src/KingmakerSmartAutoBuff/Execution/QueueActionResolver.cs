using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueActionResolver
    {
        internal static UnitEntityData ResolveCastTarget(BuffQueueAction action)
        {
            if (action == null)
            {
                return null;
            }

            return SpellCatalog.FindPartyUnit(action.CastTargetId, action.CastTargetName);
        }

        internal static List<UnitEntityData> ResolveRecipients(BuffQueueAction action)
        {
            List<UnitEntityData> result = new List<UnitEntityData>();
            if (action == null)
            {
                return result;
            }

            if (action.RecipientIds == null || action.RecipientIds.Count == 0)
            {
                UnitEntityData castTarget = ResolveCastTarget(action);
                if (castTarget != null)
                {
                    result.Add(castTarget);
                }

                return result;
            }

            for (int i = 0; i < action.RecipientIds.Count; i++)
            {
                string id = action.RecipientIds[i];
                string name = action.RecipientNames != null && i < action.RecipientNames.Count
                    ? action.RecipientNames[i]
                    : string.Empty;
                UnitEntityData unit = SpellCatalog.FindPartyUnit(id, name);
                if (unit != null && !result.Contains(unit))
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        internal static string FormatUnitList(List<UnitEntityData> units)
        {
            if (units == null || units.Count == 0)
            {
                return ModLocalization.T("Common.None");
            }

            List<string> names = new List<string>();
            foreach (UnitEntityData unit in units)
            {
                names.Add(SpellCatalog.SafeUnitName(unit));
            }

            return string.Join(", ", names.ToArray());
        }
    }
}
