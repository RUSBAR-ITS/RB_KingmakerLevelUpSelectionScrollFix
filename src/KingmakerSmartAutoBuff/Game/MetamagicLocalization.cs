using System.Collections.Generic;
using System.Linq;

namespace KingmakerSmartAutoBuff
{
    internal static class MetamagicLocalization
    {
        internal static string ListOrNone(IList<string> metamagicNames)
        {
            if (metamagicNames == null || metamagicNames.Count == 0)
            {
                return ModLocalization.T("Common.None");
            }

            return string.Join(", ", metamagicNames.Select(Localize).ToArray());
        }

        private static string Localize(string metamagicName)
        {
            if (string.IsNullOrEmpty(metamagicName))
            {
                return string.Empty;
            }

            string key = "Metamagic." + metamagicName;
            string value = ModLocalization.T(key);
            return value == key ? metamagicName : value;
        }
    }
}
