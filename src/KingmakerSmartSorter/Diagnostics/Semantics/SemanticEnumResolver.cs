using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticEnumResolver
    {
        private readonly Dictionary<string, string> m_Values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        internal SemanticEnumResolver(JArray enumIndex)
        {
            if (enumIndex == null)
            {
                return;
            }

            for (int i = 0; i < enumIndex.Count; i++)
            {
                JObject entry = enumIndex[i] as JObject;
                if (entry == null || entry["Numeric"] == null)
                {
                    continue;
                }

                string type = (string)entry["Type"] ?? string.Empty;
                long numeric = (long?)entry["Numeric"] ?? 0L;
                string raw = (string)entry["Raw"] ?? string.Empty;
                string display = (string)entry["Localized"];
                if (string.IsNullOrEmpty(display))
                {
                    display = (string)entry["Display"] ?? raw;
                }

                display = SemanticLocalization.EnumValue(type, raw, display);
                string key = MakeKey(type, numeric);
                if (!m_Values.ContainsKey(key))
                {
                    m_Values.Add(key, display);
                }
            }
        }

        internal string Resolve(string type, long numeric)
        {
            string result;
            if (m_Values.TryGetValue(MakeKey(type, numeric), out result))
            {
                return result;
            }

            try
            {
                Type enumType = FindLoadedType(type);
                if (enumType != null && enumType.IsEnum)
                {
                    object enumValue = Enum.ToObject(enumType, numeric);
                    string raw = enumValue.ToString();
                    if (!string.IsNullOrEmpty(raw)
                        && !string.Equals(
                            raw,
                            numeric.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            StringComparison.Ordinal))
                    {
                        return SemanticLocalization.EnumValue(type, raw, raw);
                    }
                }
            }
            catch
            {
                // The raw numeric value remains available when a game enum cannot be loaded.
            }

            return numeric.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Type FindLoadedType(string fullName)
        {
            Type result = Type.GetType(fullName + ", Assembly-CSharp", false);
            if (result != null)
            {
                return result;
            }

            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                result = assemblies[i].GetType(fullName, false);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static string MakeKey(string type, long numeric)
        {
            return (type ?? string.Empty)
                + "|"
                + numeric.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
