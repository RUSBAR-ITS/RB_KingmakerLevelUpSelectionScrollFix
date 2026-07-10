using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellEntryFactory
    {
        internal static SpellCatalogEntry CreateEntry(
            UnitEntityData caster,
            Spellbook spellbook,
            AbilityData ability,
            int level,
            int availableCasts)
        {
            SpellCatalogEntry entry = new SpellCatalogEntry();
            entry.Caster = caster;
            entry.Spellbook = spellbook;
            entry.Ability = ability;
            entry.CasterId = PartyProvider.GetUnitId(caster);
            entry.CasterName = PartyProvider.SafeUnitName(caster);
            entry.SpellbookId = SafeSpellbookId(spellbook);
            entry.SpellbookName = SafeSpellbookName(spellbook);
            entry.SpellBlueprintId = SafeBlueprintId(ability.Blueprint);
            entry.SpellName = SafeAbilityName(ability);
            entry.Description = SafeAbilityDescription(ability);
            entry.SpellLevel = level;
            entry.MetamagicNames = GetMetamagicNames(ability);
            entry.MetamagicText = MetamagicLocalization.ListOrNone(entry.MetamagicNames);
            entry.TargetKind = TargetResolver.DetermineTargetKind(ability);
            entry.BuffProfile = AbilityBuffProfileReader.Read(ability);
            entry.TargetSummary = AbilityBuffProfileReader.LocalizeDeliveryKind(entry.BuffProfile.DeliveryKind);
            entry.AvailableCasts = availableCasts;
            return entry;
        }

        internal static string BuildEntryKey(SpellCatalogEntry entry)
        {
            return entry.SpellbookId
                + "|"
                + entry.SpellBlueprintId
                + "|"
                + entry.SpellLevel
                + "|"
                + string.Join(",", entry.MetamagicNames.ToArray());
        }

        internal static int SafeAvailableCastCount(Spellbook spellbook, AbilityData ability)
        {
            try
            {
                return spellbook.GetAvailableForCastSpellCount(ability);
            }
            catch
            {
                try
                {
                    return ability != null && ability.IsAvailableForCast ? 1 : 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        private static string SafeSpellbookId(Spellbook spellbook)
        {
            try
            {
                return spellbook != null && spellbook.Blueprint != null ? spellbook.Blueprint.AssetGuid : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeSpellbookName(Spellbook spellbook)
        {
            try
            {
                if (spellbook == null || spellbook.Blueprint == null)
                {
                    return "<spellbook>";
                }

                if (!string.IsNullOrEmpty(spellbook.Blueprint.DisplayName))
                {
                    return spellbook.Blueprint.DisplayName;
                }

                return !string.IsNullOrEmpty(spellbook.Blueprint.name) ? spellbook.Blueprint.name : "<spellbook>";
            }
            catch
            {
                return "<spellbook>";
            }
        }

        private static string SafeBlueprintId(BlueprintAbility blueprint)
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

        private static string SafeAbilityName(AbilityData ability)
        {
            try
            {
                if (ability != null && !string.IsNullOrEmpty(ability.Name))
                {
                    return ability.Name;
                }

                return ability != null && ability.Blueprint != null ? ability.Blueprint.Name : "<spell>";
            }
            catch
            {
                return "<spell>";
            }
        }

        private static string SafeAbilityDescription(AbilityData ability)
        {
            try
            {
                if (ability != null && !string.IsNullOrEmpty(ability.Description))
                {
                    return ability.Description;
                }

                return ability != null && ability.Blueprint != null ? ability.Blueprint.Description : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<string> GetMetamagicNames(AbilityData ability)
        {
            List<string> result = new List<string>();
            try
            {
                if (ability == null || ability.MetamagicData == null || !ability.MetamagicData.NotEmpty)
                {
                    return result;
                }

                Metamagic mask = ability.MetamagicData.MetamagicMask;
                foreach (Metamagic value in Enum.GetValues(typeof(Metamagic)))
                {
                    if (Convert.ToInt32(value) == 0)
                    {
                        continue;
                    }

                    if ((mask & value) == value)
                    {
                        result.Add(value.ToString());
                    }
                }
            }
            catch
            {
                return result;
            }

            return result;
        }
    }
}
