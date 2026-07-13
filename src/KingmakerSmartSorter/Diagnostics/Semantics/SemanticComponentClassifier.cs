using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticComponentClassifier
    {
        private static readonly HashSet<string> s_AttributeStats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Strength",
                "Dexterity",
                "Constitution",
                "Intelligence",
                "Wisdom",
                "Charisma"
            };

        private static readonly HashSet<string> s_SkillStats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Athletics",
                "Mobility",
                "Trickery",
                "Stealth",
                "KnowledgeArcana",
                "KnowledgeWorld",
                "LoreNature",
                "LoreReligion",
                "Perception",
                "Persuasion",
                "UseMagicDevice"
            };

        internal static SemanticComponentClassification Classify(JObject value)
        {
            string shortType = (string)value["ShortType"] ?? string.Empty;
            string lower = shortType.ToLowerInvariant();

            if (lower.Contains("statbonus")
                || lower.Contains("contextstatbonus")
                || lower.Contains("increasestat"))
            {
                return Recognized(ClassifyStat(ReadEnumRaw(value, "Stat")));
            }

            if (lower.Contains("savingthrow") && lower.Contains("bonus"))
            {
                return Recognized("SavingThrow");
            }

            if (lower.Contains("allsavesbonus"))
            {
                return Recognized("SavingThrow");
            }

            if (lower.Contains("damageresistancephysical"))
            {
                return Recognized("DamageReduction");
            }

            if (lower.Contains("damageresistanceenergy")
                || lower.Contains("energyresistance"))
            {
                return Recognized("EnergyResistance");
            }

            if (lower.Contains("immunity"))
            {
                return Recognized("Immunity");
            }

            if (lower.Contains("armorclass")
                || lower.Contains("acbonus")
                || lower.Contains("naturalarmor"))
            {
                return Recognized("Defense");
            }

            if (lower.Contains("attackbonus")
                || lower.Contains("weaponattack")
                || lower.Contains("additionalattack")
                || lower.Contains("criticalconfirmation")
                || lower.Contains("criticaledge")
                || lower.Contains("maneuveronattack"))
            {
                return Recognized("Attack");
            }

            if (lower.Contains("dealdamage")
                || lower.Contains("damagebonus")
                || lower.Contains("additionaldamage")
                || lower.Contains("weapondamage")
                || lower.Contains("additionalbonusondamage")
                || lower.Contains("damagestatreplacement"))
            {
                return Recognized("Damage");
            }

            if (lower.Contains("heal"))
            {
                return Recognized("Healing");
            }

            if (lower.Contains("addunitfeature")
                || lower.Contains("addunitfact")
                || lower.StartsWith("addfeature", StringComparison.Ordinal)
                || lower.StartsWith("addfact", StringComparison.Ordinal)
                || lower.Contains("grantfeature"))
            {
                return Recognized("GrantedFeature");
            }

            if (lower.Contains("castspell"))
            {
                return Recognized("SpellCast");
            }

            if (lower.Contains("addability") || lower.Contains("grantability"))
            {
                return Recognized("GrantedAbility");
            }

            if (lower.Contains("applybuff"))
            {
                return Recognized("BuffApplication");
            }

            if (lower.Contains("casterlevel")
                || lower.Contains("spellpenetration")
                || lower.Contains("metamagic")
                || lower.Contains("spellresistance")
                || lower.Contains("spelldc")
                || lower.Contains("spellschooldc")
                || lower.Contains("spelldescriptordc")
                || lower.Contains("concentrationbonus")
                || lower.Contains("setabilityparams")
                || lower.Contains("draconicbloodlinearcana"))
            {
                return Recognized("Spellcasting");
            }

            if (lower.Contains("initiative"))
            {
                return Recognized("Initiative");
            }

            if (lower.Contains("speed") || lower.Contains("movement"))
            {
                return Recognized("Movement");
            }

            if (lower.Contains("resourceamount"))
            {
                return Recognized("Resource");
            }

            if (lower.Contains("weaponenhancement")
                || (lower.Contains("weapontype") && lower.Contains("enhancement")))
            {
                return Recognized("WeaponEnhancement");
            }

            if (lower.Contains("restriction")
                || lower.StartsWith("prerequisite", StringComparison.Ordinal)
                || lower.Contains("lockequipmentslot")
                || lower.Contains("isunitlevel"))
            {
                return Recognized("Restriction");
            }

            if (lower.Contains("allskillsbonus"))
            {
                return Recognized("Skill");
            }

            if (lower == "resistenergy")
            {
                return Recognized("EnergyResistance");
            }

            if (lower.Contains("enchantwornitem"))
            {
                return Recognized("Enchantment");
            }

            if (lower.Contains("trigger") || lower == "onspawnbuff")
            {
                return Recognized("TriggeredEffect");
            }

            if (lower.Contains("increasefeatrank"))
            {
                return Recognized("Feat");
            }

            if (lower.Contains("killtarget"))
            {
                return Recognized("Kill");
            }

            if (lower.Contains("spawnmonster")
                || (lower.Contains("spawn") && lower.Contains("monster")))
            {
                return Recognized("Summon");
            }

            if (lower.Contains("fortification")
                || lower.Contains("maneuverdefence")
                || lower == "evasion"
                || lower.Contains("mirrorimage")
                || lower.StartsWith("damagereductionagainst", StringComparison.Ordinal))
            {
                return Recognized("Defense");
            }

            if (lower.Contains("energyvulnerability"))
            {
                return Recognized("Vulnerability");
            }

            if (lower.Contains("additionallimb"))
            {
                return Recognized("GrantedFeature");
            }

            if (lower.Contains("partyencumbrance"))
            {
                return Recognized("Movement");
            }

            if (lower.Contains("aurafeature"))
            {
                return Recognized("Aura");
            }

            if (lower.Contains("areaeffect"))
            {
                return Recognized("AreaEffect");
            }

            if (lower.Contains("dispelmagic"))
            {
                return Recognized("Dispel");
            }

            if (lower.Contains("flagunlocked") || lower.Contains("unlockflag"))
            {
                return Recognized("WorldState");
            }

            if (lower.Contains("weariness")
                || lower.Contains("statuscondition")
                || lower == "addcondition"
                || lower.Contains("changeunitsize")
                || lower == "polymorph"
                || lower.Contains("changefaction")
                || lower.Contains("breakfree"))
            {
                return Recognized("Condition");
            }

            if (lower.Contains("buffsubstitution"))
            {
                return Recognized("BuffApplication");
            }

            if (lower.Contains("emptyhandweapon")
                || lower.Contains("secondaryattacks")
                || lower.Contains("criticalmultiplier")
                || lower.Contains("extraattack")
                || lower.Contains("ignoreconcealment"))
            {
                return Recognized("Attack");
            }

            if (lower.Contains("mechanicsfeature") || lower == "blindsense")
            {
                return Recognized("GrantedFeature");
            }

            if (lower.Contains("chirurgeonspell")
                || lower.Contains("replaceabilityparams"))
            {
                return Recognized("Spellcasting");
            }

            if (lower.Contains("forbidspellcasting") || lower == "uniquebuff")
            {
                return Recognized("Restriction");
            }

            if (lower.Contains("poisonstatdamage"))
            {
                return Recognized("Damage");
            }

            if (lower.Contains("temporaryhitpoints")
                || lower.Contains("invisibility")
                || lower.Contains("unwillingshield"))
            {
                return Recognized("Defense");
            }

            if (lower.Contains("breathoflife"))
            {
                return Recognized("Healing");
            }

            if (lower.Contains("modifyd20"))
            {
                return Recognized("RollModifier");
            }

            if (lower.Contains("removebuff") || lower.Contains("suppressbuff"))
            {
                return Recognized("Dispel");
            }

            if (lower.StartsWith("contextcondition", StringComparison.Ordinal)
                || lower == "conditional")
            {
                return Recognized("Condition");
            }

            if (IsStructural(lower))
            {
                return new SemanticComponentClassification
                {
                    Category = "Structural",
                    Structural = true
                };
            }

            return new SemanticComponentClassification
            {
                Category = "Other"
            };
        }

        private static string ClassifyStat(string stat)
        {
            if (s_AttributeStats.Contains(stat))
            {
                return "Attribute";
            }

            if (s_SkillStats.Contains(stat))
            {
                return "Skill";
            }

            if (string.Equals(stat, "AC", StringComparison.OrdinalIgnoreCase))
            {
                return "Defense";
            }

            if (string.Equals(stat, "Fortitude", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stat, "Reflex", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stat, "Will", StringComparison.OrdinalIgnoreCase))
            {
                return "SavingThrow";
            }

            if (stat.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Attack";
            }

            if (stat.IndexOf("Damage", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Damage";
            }

            return "StatBonus";
        }

        private static bool IsStructural(string lower)
        {
            return lower == "abilityeffectrunaction"
                || lower == "contextactionremovebuff"
                || lower == "contextactionremovebuffsinglestack"
                || lower == "contextactionsavingthrow"
                || lower == "contextactionconditionalsaved"
                || lower == "contextactionspawnfx"
                || lower == "contextcalculateSharedvalue".ToLowerInvariant()
                || lower.StartsWith("abilitydeliver", StringComparison.Ordinal)
                || lower.StartsWith("abilitytarget", StringComparison.Ordinal)
                || lower.StartsWith("abilityspawnfx", StringComparison.Ordinal)
                || lower.StartsWith("spellcomponent", StringComparison.Ordinal)
                || lower == "spelllistcomponent"
                || lower == "spelldescriptorcomponent"
                || lower == "contextrankconfig"
                || lower == "abilityresourcelogic"
                || lower == "contextactiononcontextcaster"
                || lower == "factowner"
                || lower.Contains("recommendation")
                || lower == "contextactionremoveself"
                || lower == "contextactionchangesharedvalue"
                || lower == "contextactionrandomize"
                || lower == "abilityuseonrest"
                || lower == "replaceaskslist"
                || lower == "abilityaoeradius"
                || lower == "abilityeffectstickytouch"
                || lower == "abilityexecuteactiononcast"
                || lower == "abilityvariants"
                || lower == "abilitycustomdimensiondoor"
                || lower == "activatableabilityresourcelogic"
                || lower == "buffparticleeffectplay"
                || lower == "replacesourcebone";
        }

        private static string ReadEnumRaw(JObject value, string fieldName)
        {
            JArray fields = value["Fields"] as JArray;
            if (fields == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                JObject field = fields[i] as JObject;
                if (field == null || (string)field["Name"] != fieldName)
                {
                    continue;
                }

                JObject enumValue = field["Value"] as JObject;
                return enumValue == null ? string.Empty : (string)enumValue["Raw"] ?? string.Empty;
            }

            return string.Empty;
        }

        private static SemanticComponentClassification Recognized(string category)
        {
            return new SemanticComponentClassification
            {
                Category = category,
                Recognized = true
            };
        }
    }
}
