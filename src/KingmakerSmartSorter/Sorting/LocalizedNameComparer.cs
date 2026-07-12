using System;
using System.Globalization;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

namespace KingmakerSmartSorter
{
    internal static class LocalizedNameComparer
    {
        internal static int Compare(string left, string right)
        {
            string first = left ?? string.Empty;
            string second = right ?? string.Empty;

            try
            {
                CompareInfo compareInfo = ResolveCulture(LocalizationManager.CurrentLocale).CompareInfo;
                int localized = compareInfo.Compare(
                    first,
                    second,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
                if (localized != 0)
                {
                    return localized;
                }
            }
            catch (Exception)
            {
                int currentCulture = string.Compare(first, second, StringComparison.CurrentCultureIgnoreCase);
                if (currentCulture != 0)
                {
                    return currentCulture;
                }
            }

            return string.Compare(first, second, StringComparison.Ordinal);
        }

        private static CultureInfo ResolveCulture(Locale locale)
        {
            switch (locale)
            {
                case Locale.ruRU:
                    return CultureInfo.GetCultureInfo("ru-RU");
                case Locale.deDE:
                    return CultureInfo.GetCultureInfo("de-DE");
                case Locale.frFR:
                    return CultureInfo.GetCultureInfo("fr-FR");
                case Locale.itIT:
                    return CultureInfo.GetCultureInfo("it-IT");
                case Locale.esES:
                    return CultureInfo.GetCultureInfo("es-ES");
                case Locale.zhCN:
                    return CultureInfo.GetCultureInfo("zh-CN");
                case Locale.jaJP:
                    return CultureInfo.GetCultureInfo("ja-JP");
                case Locale.enGB:
                default:
                    return CultureInfo.GetCultureInfo("en-GB");
            }
        }
    }
}
