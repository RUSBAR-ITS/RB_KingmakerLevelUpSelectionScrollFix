using System;
using System.Collections.Generic;
using System.Text;

namespace KingmakerSmartSorter
{
    internal static class SemanticReferenceNameBuilder
    {
        private static readonly string[] s_TechnicalSuffixes =
        {
            "ActivatableAbility",
            "Enchantment",
            "AreaEffect",
            "Blueprint",
            "Feature",
            "Ability",
            "Resource",
            "Subtype",
            "Caster",
            "Effect",
            "Area",
            "Buff",
            "Item",
            "Type",
            "New"
        };

        internal static List<string> BuildAliases(string internalName)
        {
            List<string> result = new List<string>();
            AddAlias(result, NormalizeAlias(internalName));

            string simplified = RemoveTechnicalSuffixes(internalName);
            AddAlias(result, NormalizeAlias(simplified));
            AddAlias(result, NormalizeAlias(RemoveContextSuffix(internalName, "RageBuff")));
            AddAlias(result, NormalizeAlias(CollapseRepeatedTerminalWord(simplified)));
            return result;
        }

        internal static string BuildFallback(
            string internalName,
            string blueprintType,
            out string nameSource)
        {
            string translated;
            if (SemanticLocalization.TryReference(internalName, out translated))
            {
                nameSource = "ModLocalization";
                return translated;
            }

            nameSource = "HumanizedInternalName";
            string simplified = RemoveTechnicalSuffixes(internalName);
            string display = HumanizeIdentifier(simplified);
            if (!string.IsNullOrEmpty(display))
            {
                return display;
            }

            display = HumanizeIdentifier(internalName);
            return string.IsNullOrEmpty(display)
                ? SemanticLocalization.Component(blueprintType)
                : display;
        }

        private static string RemoveTechnicalSuffixes(string value)
        {
            string result = value == null ? string.Empty : value.Trim();
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < s_TechnicalSuffixes.Length; i++)
                {
                    string suffix = s_TechnicalSuffixes[i];
                    if (result.Length <= suffix.Length
                        || !result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    result = result.Substring(0, result.Length - suffix.Length).TrimEnd();
                    changed = true;
                    break;
                }
            }
            while (changed);

            return result;
        }

        private static string RemoveContextSuffix(string value, string suffix)
        {
            if (string.IsNullOrEmpty(value)
                || string.IsNullOrEmpty(suffix)
                || value.Length <= suffix.Length
                || !value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return RemoveTechnicalSuffixes(
                value.Substring(0, value.Length - suffix.Length));
        }

        private static string CollapseRepeatedTerminalWord(string value)
        {
            string humanized = HumanizeIdentifier(value);
            string[] words = humanized.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2
                || !string.Equals(
                    words[words.Length - 1],
                    words[words.Length - 2],
                    StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return string.Join(" ", words, 0, words.Length - 1);
        }

        private static string NormalizeAlias(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]))
                {
                    result.Append(char.ToLowerInvariant(value[i]));
                }
            }

            return result.ToString();
        }

        private static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '_' || current == '-' || current == '$')
                {
                    if (result.Length > 0 && result[result.Length - 1] != ' ')
                    {
                        result.Append(' ');
                    }

                    continue;
                }

                bool startsWord = i > 0
                    && ((char.IsUpper(current) && char.IsLower(value[i - 1]))
                        || (char.IsDigit(current) && !char.IsDigit(value[i - 1]))
                        || (!char.IsDigit(current) && char.IsDigit(value[i - 1]))
                        || (char.IsUpper(current)
                            && i + 1 < value.Length
                            && char.IsLower(value[i + 1])
                            && char.IsUpper(value[i - 1])));
                if (startsWord && result.Length > 0 && result[result.Length - 1] != ' ')
                {
                    result.Append(' ');
                }

                result.Append(current);
            }

            return result.ToString().Trim();
        }

        private static void AddAlias(List<string> aliases, string value)
        {
            if (!string.IsNullOrEmpty(value) && !aliases.Contains(value))
            {
                aliases.Add(value);
            }
        }
    }
}
