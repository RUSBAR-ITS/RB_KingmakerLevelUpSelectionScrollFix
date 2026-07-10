using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace KingmakerGlobalMapZoom
{
    internal static class ModLocalization
    {
        private const string FallbackLocale = "enGB";

        private static readonly Dictionary<string, string> s_Empty = new Dictionary<string, string>();

        private static Dictionary<string, string> s_CurrentStrings = s_Empty;
        private static Dictionary<string, string> s_FallbackStrings = s_Empty;
        private static string s_ModPath;
        private static string s_CurrentLocaleCode = "ruRU";

        internal static void Initialize(string modPath)
        {
            s_ModPath = modPath;
            s_FallbackStrings = LoadLocale(FallbackLocale);
            Reload();
        }

        internal static void Reload()
        {
            s_CurrentLocaleCode = ResolveLocaleCode();
            s_CurrentStrings = s_CurrentLocaleCode == FallbackLocale
                ? s_FallbackStrings
                : LoadLocale(s_CurrentLocaleCode);

            Logger.Info("Localization loaded. locale=" + s_CurrentLocaleCode);
        }

        internal static string T(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            string value;
            if (s_CurrentStrings != null && s_CurrentStrings.TryGetValue(key, out value))
            {
                return value;
            }

            if (s_FallbackStrings != null && s_FallbackStrings.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        private static string ResolveLocaleCode()
        {
            Settings settings = Main.Settings;
            return settings != null && settings.Language == ModLanguage.English ? "enGB" : "ruRU";
        }

        private static Dictionary<string, string> LoadLocale(string localeCode)
        {
            try
            {
                string path = Path.Combine(Path.Combine(s_ModPath ?? string.Empty, "Localization"), localeCode + ".json");
                if (!File.Exists(path))
                {
                    Logger.Warning("Localization file not found: " + path);
                    return s_Empty;
                }

                return FlatJsonParser.Parse(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to load localization file for locale " + localeCode + ".", ex);
                return s_Empty;
            }
        }

        private static class FlatJsonParser
        {
            internal static Dictionary<string, string> Parse(string json)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                if (string.IsNullOrEmpty(json))
                {
                    return result;
                }

                int index = 0;
                SkipWhitespace(json, ref index);
                Expect(json, ref index, '{');

                while (true)
                {
                    SkipWhitespace(json, ref index);
                    if (index < json.Length && json[index] == '}')
                    {
                        index++;
                        break;
                    }

                    string key = ReadString(json, ref index);
                    SkipWhitespace(json, ref index);
                    Expect(json, ref index, ':');
                    SkipWhitespace(json, ref index);
                    string value = ReadString(json, ref index);
                    result[key] = value;

                    SkipWhitespace(json, ref index);
                    if (index < json.Length && json[index] == ',')
                    {
                        index++;
                        continue;
                    }

                    if (index < json.Length && json[index] == '}')
                    {
                        index++;
                        break;
                    }

                    throw new FormatException("Expected ',' or '}' at position " + index + ".");
                }

                return result;
            }

            private static void SkipWhitespace(string json, ref int index)
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }

            private static void Expect(string json, ref int index, char expected)
            {
                if (index >= json.Length || json[index] != expected)
                {
                    throw new FormatException("Expected '" + expected + "' at position " + index + ".");
                }

                index++;
            }

            private static string ReadString(string json, ref int index)
            {
                Expect(json, ref index, '"');

                StringBuilder builder = new StringBuilder();
                while (index < json.Length)
                {
                    char c = json[index++];
                    if (c == '"')
                    {
                        return builder.ToString();
                    }

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (index >= json.Length)
                    {
                        throw new FormatException("Unterminated escape sequence.");
                    }

                    char escaped = json[index++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            builder.Append(ReadUnicodeEscape(json, ref index));
                            break;
                        default:
                            throw new FormatException("Unsupported escape sequence '\\" + escaped + "'.");
                    }
                }

                throw new FormatException("Unterminated string.");
            }

            private static char ReadUnicodeEscape(string json, ref int index)
            {
                if (index + 4 > json.Length)
                {
                    throw new FormatException("Incomplete unicode escape.");
                }

                string hex = json.Substring(index, 4);
                index += 4;
                return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
        }
    }
}
