using System;
using HarmonyLib;
using Kingmaker.View;

namespace KingmakerGlobalMapZoom
{
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.SetMapMode))]
    internal static class CameraRigSetMapModePatch
    {
        private static void Postfix(CameraRig __instance, bool isGlobalMap)
        {
            Settings settings = Main.Settings;
            if (settings == null
                || !settings.EnablePatch
                || (isGlobalMap && !settings.ApplyWhenEnteringGlobalMap))
            {
                return;
            }

            try
            {
                GlobalMapZoomController.ApplyToRig(
                    __instance,
                    isGlobalMap,
                    false,
                    isGlobalMap ? "CameraRig.SetMapMode(global)" : "CameraRig.SetMapMode(local)");
            }
            catch (Exception ex)
            {
                Logger.Exception("CameraRig.SetMapMode postfix failed.", ex);
            }
        }
    }

    [HarmonyPatch(typeof(CameraZoom), "TickZoom")]
    internal static class CameraZoomTickZoomPatch
    {
        private static void Prefix(CameraZoom __instance)
        {
            try
            {
                GlobalMapZoomController.ApplyLocalMapZoomToCameraZoom(__instance, "CameraZoom.TickZoom");
            }
            catch (Exception ex)
            {
                Logger.Exception("CameraZoom.TickZoom prefix failed.", ex);
            }
        }
    }

    [HarmonyPatch(typeof(CameraZoom), "TickSmoothZoomToTargetValue")]
    internal static class CameraZoomTickSmoothZoomToTargetValuePatch
    {
        private static void Prefix(CameraZoom __instance)
        {
            try
            {
                GlobalMapZoomController.ApplyLocalMapZoomToCameraZoom(
                    __instance,
                    "CameraZoom.TickSmoothZoomToTargetValue");
            }
            catch (Exception ex)
            {
                Logger.Exception("CameraZoom.TickSmoothZoomToTargetValue prefix failed.", ex);
            }
        }
    }
}
