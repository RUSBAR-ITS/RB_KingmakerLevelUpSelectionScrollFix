using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using UnityModManagerNet;

namespace KingmakerCategorizedFeatMultiplier
{
    internal static class Compatibility
    {
        private static bool s_BagOfTricksPrefixPatched;
        private static bool s_BagOfTricksPrefixUsed;

        internal static void PatchBagOfTricksMultiplier(Harmony harmony)
        {
            if (harmony == null || s_BagOfTricksPrefixPatched)
            {
                return;
            }

            try
            {
                Assembly bagAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.GetName().Name == "BagOfTricks");
                if (bagAssembly == null)
                {
                    return;
                }

                Type patchType = bagAssembly.GetType("BagOfTricks.HarmonyPatches+MultiplyFeatPoints_LevelUpHelper_AddFeatures_Patch");
                if (patchType == null)
                {
                    Logger.Warning("Bag of Tricks is loaded, but its feat multiplier patch type was not found.");
                    return;
                }

                MethodInfo target = patchType.GetMethod("Prefix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo prefix = typeof(Compatibility).GetMethod(
                    "BagOfTricksFeatMultiplierPrefix",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (target == null || prefix == null)
                {
                    Logger.Warning("Could not prepare Bag of Tricks feat multiplier compatibility patch.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                s_BagOfTricksPrefixPatched = true;
                Logger.Info("Patched Bag of Tricks feat multiplier prefix for compatibility.");
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to patch Bag of Tricks feat multiplier prefix.", ex);
            }
        }

        internal static void CheckBagOfTricks(string context)
        {
            try
            {
                UnityModManager.ModEntry bagOfTricks = UnityModManager.FindMod("BagOfTricks");
                if (bagOfTricks == null)
                {
                    return;
                }

                int? multiplier = TryReadBagOfTricksFeatMultiplier(bagOfTricks.Path);
                if (multiplier.HasValue && multiplier.Value > 1)
                {
                    Logger.Warning(
                        "Bag of Tricks is installed and its featMultiplier is "
                        + multiplier.Value
                        + ". Set Bag of Tricks Feat Selection Multiplier to 1 while using this mod, because both mods patch LevelUpHelper.AddFeatures. Context="
                        + context);
                    return;
                }

                if (multiplier.HasValue)
                {
                    Logger.Info("Bag of Tricks detected with featMultiplier=" + multiplier.Value + ". Context=" + context);
                }
                else
                {
                    Logger.Warning(
                        "Bag of Tricks is installed, but its Settings.xml featMultiplier could not be read. "
                        + "Keep Bag of Tricks Feat Selection Multiplier at 1 while using this mod. Context="
                        + context);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to check Bag of Tricks compatibility.", ex);
            }
        }

        private static bool BagOfTricksFeatMultiplierPrefix(
            LevelUpState state,
            UnitDescriptor unit,
            System.Collections.Generic.IList<BlueprintFeatureBase> features,
            BlueprintScriptableObject source,
            int level,
            ref bool __result)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch)
            {
                return true;
            }

            bool skipOriginalAddFeatures;
            LevelUpHelperAddFeaturesPatch.TryApplyOrFallback(
                state,
                unit,
                features,
                source,
                level,
                "Bag of Tricks compatibility prefix",
                out skipOriginalAddFeatures);

            if (!s_BagOfTricksPrefixUsed)
            {
                s_BagOfTricksPrefixUsed = true;
                Logger.Info(
                    "Bag of Tricks feat multiplier prefix was intercepted. "
                    + "The categorized multiplier is now handling this AddFeatures call path.");
            }

            __result = !skipOriginalAddFeatures;
            return false;
        }

        private static int? TryReadBagOfTricksFeatMultiplier(string bagOfTricksPath)
        {
            if (string.IsNullOrEmpty(bagOfTricksPath))
            {
                return null;
            }

            string settingsPath = Path.Combine(bagOfTricksPath, "Settings.xml");
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            XDocument document = XDocument.Load(settingsPath);
            XElement element = document.Descendants("featMultiplier").FirstOrDefault();
            if (element == null)
            {
                return null;
            }

            int multiplier;
            return int.TryParse(element.Value, out multiplier) ? (int?)multiplier : null;
        }
    }
}
