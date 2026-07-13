using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticValueDisplayBuilder
    {
        internal static string Build(string type, JObject fields)
        {
            if (fields == null)
            {
                return string.Empty;
            }

            if (type.EndsWith("ContextDiceValue", StringComparison.Ordinal))
            {
                return BuildDiceValue(fields);
            }

            if (type.EndsWith("ContextDurationValue", StringComparison.Ordinal))
            {
                return BuildDurationValue(fields);
            }

            if (type.EndsWith("ContextValue", StringComparison.Ordinal))
            {
                return BuildContextValue(fields);
            }

            if (type.EndsWith("DiceFormula", StringComparison.Ordinal))
            {
                return BuildDiceFormula(fields);
            }

            if (type.EndsWith(".Feet", StringComparison.Ordinal))
            {
                string value = Read(fields["m_Value"]);
                return string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value + " " + SemanticLocalization.Label("Feet");
            }

            return string.Empty;
        }

        private static string BuildContextValue(JObject fields)
        {
            string valueType = ReadRaw(fields["ValueType"]);
            string value = Read(fields["Value"]);
            if (valueType == "Simple" || string.IsNullOrEmpty(valueType))
            {
                return value;
            }

            if (valueType == "Rank")
            {
                string rank = Read(fields["ValueRank"]);
                if (ReadRaw(fields["ValueRank"]) == "Default")
                {
                    rank = string.Empty;
                }

                return AppendOffset(
                    SemanticLocalization.Label("Rank")
                        + (string.IsNullOrEmpty(rank) ? string.Empty : " " + rank),
                    value);
            }

            if (valueType == "Shared")
            {
                return SemanticLocalization.Label("SharedValue")
                    + " "
                    + Read(fields["ValueShared"]);
            }

            string property = Read(fields["Property"]);
            return AppendOffset(
                string.IsNullOrEmpty(property) ? valueType : property,
                value);
        }

        private static string BuildDiceValue(JObject fields)
        {
            string dice = Read(fields["DiceType"]);
            string count = Read(fields["DiceCountValue"]);
            string bonus = Read(fields["BonusValue"]);
            string result = IsZeroDice(dice) || IsZero(count)
                ? string.Empty
                : count + dice;
            return AppendBonus(result, bonus);
        }

        private static string BuildDurationValue(JObject fields)
        {
            string value = BuildDiceValue(fields);
            string rate = Read(fields["Rate"]);
            if (string.IsNullOrEmpty(value))
            {
                return rate;
            }

            return string.IsNullOrEmpty(rate) ? value : value + " " + rate;
        }

        private static string BuildDiceFormula(JObject fields)
        {
            string dice = Read(fields["m_Dice"]);
            string rolls = Read(fields["m_Rolls"]);
            return IsZeroDice(dice) || IsZero(rolls) ? string.Empty : rolls + dice;
        }

        private static string AppendBonus(string value, string bonus)
        {
            if (IsZero(bonus))
            {
                return value;
            }

            int numeric;
            bool isNumeric = int.TryParse(
                bonus,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out numeric);
            string formatted = isNumeric && numeric > 0
                ? "+" + bonus
                : bonus;
            if (string.IsNullOrEmpty(value))
            {
                return formatted;
            }

            return isNumeric ? value + formatted : value + " + " + formatted;
        }

        private static string AppendOffset(string value, string offset)
        {
            return IsZero(offset) ? value : value + " + " + offset;
        }

        private static string Read(JToken value)
        {
            return SemanticValueNormalizer.ReadDisplay(value);
        }

        private static string ReadRaw(JToken value)
        {
            JObject obj = value as JObject;
            return obj == null ? string.Empty : (string)obj["Raw"] ?? string.Empty;
        }

        private static bool IsZeroDice(string value)
        {
            return string.IsNullOrEmpty(value) || value == "Zero" || value == "0";
        }

        private static bool IsZero(string value)
        {
            return string.IsNullOrEmpty(value) || value == "0" || value == "+0";
        }
    }
}
