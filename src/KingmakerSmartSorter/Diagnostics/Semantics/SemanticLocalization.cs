using System;
using System.Text;

namespace KingmakerSmartSorter
{
    internal static class SemanticLocalization
    {
        internal static string Category(string category)
        {
            return TranslateOrFallback("Semantic.Category." + category, category);
        }

        internal static string Scope(string scope)
        {
            return TranslateOrFallback("Semantic.Scope." + scope, scope);
        }

        internal static string DefinitionMode(string mode)
        {
            return TranslateOrFallback("Semantic.DefinitionMode." + mode, mode);
        }

        internal static string NameSource(string source)
        {
            return TranslateOrFallback("Semantic.NameSource." + source, source);
        }

        internal static string Report(string key, string fallback)
        {
            return TranslateOrFallback("Semantic.Report." + key, fallback);
        }

        internal static string Label(string label)
        {
            return TranslateOrFallback("Semantic.Label." + label, label);
        }

        internal static string Component(string componentType)
        {
            string key = "Semantic.Component." + componentType;
            string translated;
            return TryTranslate(key, out translated)
                ? translated
                : HumanizeIdentifier(componentType);
        }

        internal static bool TryReference(string internalName, out string value)
        {
            return TryTranslate(
                "Semantic.Reference." + (internalName ?? string.Empty),
                out value);
        }

        internal static string EnumValue(string type, string raw, string existingDisplay)
        {
            string shortType = GetShortType(type);
            string key = "Semantic.Enum." + shortType + "." + raw;
            string translated;
            if (TryTranslate(key, out translated))
            {
                return translated;
            }

            if (TryTranslate("Semantic.EnumValue." + raw, out translated))
            {
                return translated;
            }

            if (!string.IsNullOrEmpty(raw) && raw.IndexOf(',') >= 0)
            {
                string[] members = raw.Split(',');
                string[] displays = new string[members.Length];
                bool translatedAny = false;
                for (int i = 0; i < members.Length; i++)
                {
                    string member = members[i].Trim();
                    if (TryTranslate(
                        "Semantic.Enum." + shortType + "." + member,
                        out translated))
                    {
                        displays[i] = translated;
                        translatedAny = true;
                    }
                    else if (TryTranslate(
                        "Semantic.EnumValue." + member,
                        out translated))
                    {
                        displays[i] = translated;
                        translatedAny = true;
                    }
                    else
                    {
                        displays[i] = member;
                    }
                }

                if (translatedAny)
                {
                    return string.Join(", ", displays);
                }
            }

            if (!string.IsNullOrEmpty(existingDisplay)
                && !string.Equals(existingDisplay, raw, StringComparison.Ordinal))
            {
                return existingDisplay;
            }

            return string.IsNullOrEmpty(existingDisplay) ? raw : existingDisplay;
        }

        internal static string Template(string key, string fallback)
        {
            return TranslateOrFallback("Semantic.Template." + key, fallback);
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            string translated = ModLocalization.T(key);
            return string.Equals(translated, key, StringComparison.Ordinal)
                ? fallback ?? string.Empty
                : translated;
        }

        private static bool TryTranslate(string key, out string value)
        {
            value = ModLocalization.T(key);
            return !string.Equals(value, key, StringComparison.Ordinal);
        }

        private static string GetShortType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return "Unknown";
            }

            int nested = type.LastIndexOf('+');
            int dotted = type.LastIndexOf('.');
            int separator = Math.Max(nested, dotted);
            return separator < 0 ? type : type.Substring(separator + 1);
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
                if (i > 0
                    && char.IsUpper(current)
                    && (char.IsLower(value[i - 1])
                        || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                {
                    result.Append(' ');
                }

                result.Append(current);
            }

            return result.ToString();
        }
    }
}
