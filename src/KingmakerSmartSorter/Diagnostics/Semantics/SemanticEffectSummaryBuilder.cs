using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticEffectSummaryBuilder
    {
        internal static string Build(
            string componentType,
            string category,
            JObject parameters)
        {
            string type = componentType ?? string.Empty;
            if (SemanticComponentSemantics.IsInactive(type, parameters))
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    SemanticLocalization.Template(
                        "InactiveComponent",
                        "Inactive component: {0}"),
                    SemanticLocalization.Component(type));
            }

            if (SemanticComponentSemantics.IsPresenceBased(type))
            {
                return SemanticLocalization.Component(type);
            }

            if (type == "AllSavesBonusEquipment" || type == "BuffAllSavesBonus")
            {
                return FormatBonus(
                    SemanticLocalization.Label("AllSavingThrows"),
                    ReadValue(parameters["Value"]),
                    ReadDisplay(parameters["Descriptor"]));
            }

            if (type == "BuffAllSkillsBonus")
            {
                return FormatBonus(
                    SemanticLocalization.Label("AllSkills"),
                    ReadValue(parameters["Value"]),
                    ReadDisplay(parameters["Descriptor"]));
            }

            if (type.Contains("DamageResistanceEnergy") || type == "ResistEnergy")
            {
                return FormatValueTarget(
                    SemanticLocalization.Category("EnergyResistance"),
                    ReadValue(parameters["Value"]),
                    FirstDisplay(parameters, "Type", "EnergyType"));
            }

            if (type.Contains("DamageResistancePhysical"))
            {
                return FormatValueTarget(
                    SemanticLocalization.Category("DamageReduction"),
                    ReadValue(parameters["Value"]),
                    BuildDamageReductionBypass(parameters));
            }

            if (type.Contains("Immunity"))
            {
                string target = FirstDisplay(
                    parameters,
                    "Condition",
                    "Descriptor",
                    "SpellDescriptor",
                    "EnergyType",
                    "Type");
                return FormatTarget(
                    SemanticLocalization.Category("Immunity"),
                    string.IsNullOrEmpty(target)
                        ? SemanticLocalization.Component(type)
                        : target);
            }

            if (type.Contains("SpellSchoolDC")
                || type.Contains("SpellDescriptorDC")
                || type == "IncreaseSpellDC")
            {
                return FormatValueTarget(
                    SemanticLocalization.Label("SpellDifficultyClass"),
                    FirstValue(parameters, "BonusDC", "Value"),
                    FirstDisplay(parameters, "School", "Descriptor", "SpellDescriptor"));
            }

            if (type == "IncreaseResourceAmount")
            {
                return FormatValueTarget(
                    SemanticLocalization.Category("Resource"),
                    ReadValue(parameters["Value"]),
                    ReadDisplay(parameters["Resource"]));
            }

            if (type == "EquipmentRestrictionAlignment")
            {
                string alignment = ReadDisplay(parameters["Alignment"]);
                if ((bool?)parameters["Not"] == true)
                {
                    alignment = SemanticLocalization.Label("Not") + " " + alignment;
                }

                return FormatTarget(
                    SemanticLocalization.Category("Restriction"),
                    alignment);
            }

            if (type == "WeaponCategoryAttackBonus"
                || type == "WeaponGroupAttackBonus"
                || type == "AttackTypeAttackBonus"
                || type == "WeaponRangeTypeAttackBonusEquipment")
            {
                return FormatValueTarget(
                    SemanticLocalization.Category("Attack"),
                    FirstValue(parameters, "AttackBonus", "Bonus", "Value"),
                    FirstDisplay(
                        parameters,
                        "Category",
                        "WeaponGroup",
                        "Type",
                        "RangeType"));
            }

            if (type == "WeaponGroupDamageBonus"
                || type == "WeaponTypeDamageBonus"
                || type == "DamageBonusAgainstFactOwnerEquipment")
            {
                return FormatValueTarget(
                    SemanticLocalization.Category("Damage"),
                    FirstValue(parameters, "DamageBonus", "Bonus", "Value"),
                    FirstDisplay(
                        parameters,
                        "WeaponGroup",
                        "WeaponType",
                        "CheckedFact"));
            }

            if (type == "ContextActionDealDamage")
            {
                string damageType = FirstDisplay(parameters, "DamageType", "m_Type");
                return FormatValueTarget(
                    SemanticLocalization.Category("Damage"),
                    ReadValue(parameters["Value"]),
                    string.IsNullOrEmpty(damageType)
                        ? ReadDamageType(parameters["DamageType"])
                        : damageType);
            }

            if (type == "AddFacts")
            {
                return FormatTarget(
                    SemanticLocalization.Category("GrantedFeature"),
                    string.IsNullOrEmpty(ReadCollectionNames(parameters["Facts"]))
                        ? SemanticLocalization.Component(type)
                        : ReadCollectionNames(parameters["Facts"]));
            }

            return BuildGeneric(type, category, parameters);
        }

        private static string BuildGeneric(
            string componentType,
            string category,
            JObject parameters)
        {
            string label = SemanticLocalization.Category(category);
            string stat = FirstDisplay(parameters, "Stat");
            string descriptor = FirstDisplay(
                parameters,
                "Descriptor",
                "ModifierDescriptor");
            string value = FirstValue(
                parameters,
                "Value",
                "Bonus",
                "BonusDC",
                "AttackBonus",
                "DamageBonus",
                "Multiplier");

            if (!string.IsNullOrEmpty(stat) && !string.IsNullOrEmpty(value))
            {
                return FormatBonus(
                    stat,
                    value,
                    string.IsNullOrEmpty(descriptor) ? label : descriptor);
            }

            string target = FirstDisplay(
                parameters,
                "Feature",
                "Fact",
                "Ability",
                "Spell",
                "Buff",
                "Condition",
                "SpellDescriptor",
                "Descriptor",
                "School",
                "EnergyType",
                "DamageType",
                "Resource",
                "CheckedFact");
            if (string.IsNullOrEmpty(target))
            {
                target = FirstCollectionNames(parameters, "Facts", "Features", "Abilities");
            }

            if (!string.IsNullOrEmpty(value))
            {
                return FormatValueTarget(label, value, target);
            }

            return FormatTarget(
                label,
                string.IsNullOrEmpty(target)
                    ? SemanticLocalization.Component(componentType)
                    : target);
        }

        private static string BuildDamageReductionBypass(JObject parameters)
        {
            List<string> values = new List<string>();
            AddBypass(parameters, "BypassedByAlignment", "Alignment", values);
            AddBypass(parameters, "BypassedByForm", "Form", values);
            AddBypass(parameters, "BypassedByMaterial", "Material", values);
            AddBypass(parameters, "BypassedByReality", "Reality", values);
            AddBypass(parameters, "BypassedByWeaponType", "WeaponType", values);
            if ((bool?)parameters["BypassedByMagic"] == true)
            {
                values.Add(SemanticLocalization.Label("MagicWeapon"));
            }

            return string.Join(", ", values.ToArray());
        }

        private static void AddBypass(
            JObject parameters,
            string flagName,
            string valueName,
            List<string> result)
        {
            if ((bool?)parameters[flagName] != true)
            {
                return;
            }

            string value = ReadDisplay(parameters[valueName]);
            if (!string.IsNullOrEmpty(value) && value != "0")
            {
                result.Add(value);
            }
        }

        private static string ReadDamageType(JToken value)
        {
            JObject obj = value as JObject;
            JObject fields = obj == null ? null : obj["Fields"] as JObject;
            if (fields == null)
            {
                return ReadDisplay(value);
            }

            string kind = ReadDisplay(fields["Type"]);
            string detail = FirstDisplay(fields, "Energy", "Physical", "Common");
            return string.IsNullOrEmpty(detail)
                ? kind
                : string.IsNullOrEmpty(kind) ? detail : kind + ": " + detail;
        }

        private static string FirstValue(JObject parameters, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string value = ReadValue(parameters[names[i]]);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ReadValue(JToken value)
        {
            string display = ReadDisplay(value);
            if (!string.IsNullOrEmpty(display))
            {
                return display;
            }

            JObject obj = value as JObject;
            JObject fields = obj == null ? null : obj["Fields"] as JObject;
            return fields == null ? string.Empty : ReadDisplay(fields["Value"]);
        }

        private static string FirstDisplay(JObject parameters, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string value = ReadDisplay(parameters[names[i]]);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ReadDisplay(JToken value)
        {
            JObject obj = value as JObject;
            if (obj != null && obj["Fields"] is JObject)
            {
                JObject fields = (JObject)obj["Fields"];
                string nested = FirstDisplay(
                    fields,
                    "Display",
                    "Name",
                    "Energy",
                    "Type",
                    "Form");
                if (!string.IsNullOrEmpty(nested))
                {
                    return nested;
                }
            }

            return SemanticValueNormalizer.ReadDisplay(value);
        }

        private static string FirstCollectionNames(
            JObject parameters,
            params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string value = ReadCollectionNames(parameters[names[i]]);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ReadCollectionNames(JToken value)
        {
            JObject collection = value as JObject;
            JArray items = collection == null ? null : collection["Items"] as JArray;
            if (items == null)
            {
                return string.Empty;
            }

            List<string> values = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                string name = ReadDisplay(items[i]);
                if (!string.IsNullOrEmpty(name) && !values.Contains(name))
                {
                    values.Add(name);
                }
            }

            return string.Join(", ", values.ToArray());
        }

        private static string FormatBonus(
            string target,
            string value,
            string descriptor)
        {
            if (string.IsNullOrEmpty(value))
            {
                return FormatTarget(target, descriptor);
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                SemanticLocalization.Template("Bonus", "{2}: {1} ({0})"),
                descriptor,
                FormatSigned(value),
                target);
        }

        private static string FormatValueTarget(
            string label,
            string value,
            string target)
        {
            if (string.IsNullOrEmpty(value))
            {
                return FormatTarget(label, target);
            }

            if (string.IsNullOrEmpty(target))
            {
                return label + ": " + FormatSigned(value);
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                SemanticLocalization.Template("TargetValue", "{0}: {2} ({1})"),
                label,
                target,
                FormatSigned(value));
        }

        private static string FormatTarget(string label, string target)
        {
            return string.IsNullOrEmpty(target)
                ? label
                : string.Format(
                    CultureInfo.CurrentCulture,
                    SemanticLocalization.Template("Target", "{0}: {1}"),
                    label,
                    target);
        }

        private static string FormatSigned(string value)
        {
            if (string.IsNullOrEmpty(value)
                || value[0] == '+'
                || value[0] == '-')
            {
                return value;
            }

            int numeric;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numeric)
                && numeric > 0
                ? "+" + value
                : value;
        }
    }
}
