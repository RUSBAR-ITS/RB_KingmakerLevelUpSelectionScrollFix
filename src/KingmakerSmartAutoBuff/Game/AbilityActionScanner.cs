using System;
using System.Collections;
using System.Reflection;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace KingmakerSmartAutoBuff
{
    internal static class AbilityActionScanner
    {
        private const int MaxDepth = 6;

        internal static AbilityActionScanResult Scan(ActionList actions)
        {
            AbilityActionScanResult result = new AbilityActionScanResult();
            ScanActionList(actions, result, 0);
            return result;
        }

        private static void ScanActionList(ActionList actions, AbilityActionScanResult result, int depth)
        {
            if (actions == null || depth > MaxDepth)
            {
                return;
            }

            GameAction[] items;
            try
            {
                items = actions.Actions;
            }
            catch
            {
                return;
            }

            if (items == null)
            {
                return;
            }

            foreach (GameAction action in items)
            {
                ScanAction(action, result, depth + 1);
            }
        }

        private static void ScanAction(GameAction action, AbilityActionScanResult result, int depth)
        {
            if (action == null || depth > MaxDepth)
            {
                return;
            }

            result.Diagnostics.Add(action.GetType().Name);

            ContextActionApplyBuff applyBuff = action as ContextActionApplyBuff;
            if (applyBuff != null)
            {
                AddAppliedBuff(result, applyBuff.Buff);
            }

            if (action is ContextActionPartyMembers)
            {
                result.HasPartyMembersAction = true;
            }

            ScanNestedActionLists(action, result, depth + 1);
        }

        private static void ScanNestedActionLists(object owner, AbilityActionScanResult result, int depth)
        {
            if (owner == null || depth > MaxDepth)
            {
                return;
            }

            Type type = owner.GetType();
            FieldInfo[] fields;
            try
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch
            {
                return;
            }

            foreach (FieldInfo field in fields)
            {
                object value;
                try
                {
                    value = field.GetValue(owner);
                }
                catch
                {
                    continue;
                }

                ActionList nested = value as ActionList;
                if (nested != null)
                {
                    ScanActionList(nested, result, depth + 1);
                    continue;
                }

                IEnumerable enumerable = value as IEnumerable;
                if (enumerable == null || value is string)
                {
                    continue;
                }

                foreach (object item in enumerable)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    ActionList itemActions = item as ActionList;
                    if (itemActions != null)
                    {
                        ScanActionList(itemActions, result, depth + 1);
                        continue;
                    }

                    if (item.GetType().Namespace != null && item.GetType().Namespace.StartsWith("Kingmaker", StringComparison.Ordinal))
                    {
                        ScanNestedActionLists(item, result, depth + 1);
                    }
                }
            }
        }

        private static void AddAppliedBuff(AbilityActionScanResult result, BlueprintBuff buff)
        {
            if (buff == null)
            {
                return;
            }

            string id = SafeBlueprintId(buff);
            if (!string.IsNullOrEmpty(id) && !result.AppliedBuffBlueprintIds.Contains(id))
            {
                result.AppliedBuffBlueprintIds.Add(id);
                result.AppliedBuffNames.Add(SafeBlueprintName(buff));
            }
        }

        private static string SafeBlueprintId(BlueprintBuff buff)
        {
            try
            {
                return buff.AssetGuid ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeBlueprintName(BlueprintBuff buff)
        {
            try
            {
                if (!string.IsNullOrEmpty(buff.Name))
                {
                    return buff.Name;
                }

                return buff.name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
