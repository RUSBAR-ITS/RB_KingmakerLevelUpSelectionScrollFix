using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class DiagnosticFileWriter
    {
        internal static long WriteVerified(string outputPath, JObject report)
        {
            string directory = Path.GetDirectoryName(outputPath);
            Directory.CreateDirectory(directory);

            string temporaryPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                string json = report.ToString(Formatting.Indented);
                File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

                using (StreamReader stream = new StreamReader(
                    temporaryPath,
                    Encoding.UTF8,
                    true))
                using (JsonTextReader reader = new JsonTextReader(stream))
                {
                    JToken parsed = JToken.ReadFrom(reader);
                    if (parsed.Type != JTokenType.Object)
                    {
                        throw new InvalidDataException("Diagnostic root is not a JSON object.");
                    }
                }

                if (File.Exists(outputPath))
                {
                    File.Replace(temporaryPath, outputPath, null);
                }
                else
                {
                    File.Move(temporaryPath, outputPath);
                }

                return new FileInfo(outputPath).Length;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
