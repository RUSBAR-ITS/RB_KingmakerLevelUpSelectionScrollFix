using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;

namespace KingmakerSmartAutoBuff
{
    internal static class ActiveBuffHelper
    {
        internal static List<ActiveBuffInfo> GetActiveBuffs(UnitEntityData target)
        {
            List<ActiveBuffInfo> result = new List<ActiveBuffInfo>();
            if (target == null || target.Descriptor == null || target.Descriptor.Buffs == null)
            {
                return result;
            }

            try
            {
                foreach (Buff buff in target.Descriptor.Buffs.Enumerable)
                {
                    ActiveBuffInfo info = CreateInfo(buff);
                    if (info != null)
                    {
                        result.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to read active buffs for " + SpellCatalog.SafeUnitName(target) + ".", ex);
            }

            return result;
        }

        internal static bool HasBuffFromAbility(
            UnitEntityData target,
            SpellCatalogEntry entry,
            out ActiveBuffInfo matchedBuff)
        {
            matchedBuff = null;
            string abilityId = GetEntryAbilityId(entry);
            if (target == null || string.IsNullOrEmpty(abilityId))
            {
                return false;
            }

            foreach (ActiveBuffInfo buff in GetActiveBuffs(target))
            {
                if (string.Equals(buff.SourceAbilityId, abilityId, StringComparison.Ordinal))
                {
                    matchedBuff = buff;
                    return true;
                }
            }

            return false;
        }

        private static ActiveBuffInfo CreateInfo(Buff buff)
        {
            if (buff == null)
            {
                return null;
            }

            ActiveBuffInfo info = new ActiveBuffInfo();
            info.Buff = buff;
            info.BuffName = SafeBuffName(buff);
            info.BuffBlueprintId = SafeBlueprintId(buff.Blueprint);

            MechanicsContext context = SafeContext(buff);
            BlueprintAbility sourceAbility = context != null ? SafeSourceAbility(context) : null;
            if (sourceAbility != null)
            {
                info.SourceAbilityId = SafeBlueprintId(sourceAbility);
                info.SourceAbilityName = SafeBlueprintName(sourceAbility);
            }

            return info;
        }

        private static string GetEntryAbilityId(SpellCatalogEntry entry)
        {
            if (entry == null || entry.Ability == null || entry.Ability.Blueprint == null)
            {
                return string.Empty;
            }

            return SafeBlueprintId(entry.Ability.Blueprint);
        }

        private static MechanicsContext SafeContext(Buff buff)
        {
            try
            {
                return buff.Context ?? buff.MaybeContext;
            }
            catch
            {
                return null;
            }
        }

        private static BlueprintAbility SafeSourceAbility(MechanicsContext context)
        {
            try
            {
                return context.SourceAbility;
            }
            catch
            {
                return null;
            }
        }

        private static string SafeBuffName(Buff buff)
        {
            try
            {
                return buff.Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeBlueprintName(BlueprintScriptableObject blueprint)
        {
            try
            {
                return blueprint != null ? blueprint.name : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeBlueprintId(BlueprintScriptableObject blueprint)
        {
            try
            {
                return blueprint != null ? blueprint.AssetGuid : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
