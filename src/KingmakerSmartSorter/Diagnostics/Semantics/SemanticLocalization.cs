using System;

namespace KingmakerSmartSorter
{
    internal static class SemanticLocalization
    {
        internal static string Category(string category)
        {
            return TranslateOrFallback("Semantic.Category." + category, category);
        }

        internal static string EnumValue(string type, string raw, string existingDisplay)
        {
            if (!string.IsNullOrEmpty(existingDisplay)
                && !string.Equals(existingDisplay, raw, StringComparison.Ordinal))
            {
                return existingDisplay;
            }

            string shortType = GetShortType(type);
            return TranslateOrFallback(
                "Semantic.Enum." + shortType + "." + raw,
                string.IsNullOrEmpty(existingDisplay) ? raw : existingDisplay);
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
    }
}
