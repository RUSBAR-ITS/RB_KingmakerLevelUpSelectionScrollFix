using System;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.View;
using UnityEngine;

namespace KingmakerGlobalMapZoom
{
    internal static class GlobalMapZoomController
    {
        private const float MinimumAllowedFov = 1f;
        private const float MaximumAllowedFov = 170f;

        private static int s_diagnosticLogCount;

        internal static void ResetDiagnostics()
        {
            s_diagnosticLogCount = 0;
        }

        internal static void ApplyNow(string reason)
        {
            try
            {
                Game game = Game.Instance;
                if (game == null || game.UI == null)
                {
                    Log("Cannot apply global map zoom now: game UI is not available. reason=" + reason);
                    return;
                }

                CameraRig cameraRig = game.UI.GetCameraRig();
                if (cameraRig == null)
                {
                    Log("Cannot apply global map zoom now: CameraRig is not available. reason=" + reason);
                    return;
                }

                bool forceGlobalCameraFieldOfView = cameraRig.GetMapMode() && IsCurrentGameModeGlobalMap();
                bool clampLocalCameraFieldOfView = ShouldApplyLocalMapZoom(cameraRig);
                ApplyToRig(cameraRig, forceGlobalCameraFieldOfView, clampLocalCameraFieldOfView, reason);
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to apply camera zoom now.", ex);
            }
        }

        internal static void ApplyToRig(CameraRig cameraRig, bool forceCameraFieldOfView, string reason)
        {
            ApplyToRig(cameraRig, forceCameraFieldOfView, false, reason);
        }

        internal static void ApplyToRig(
            CameraRig cameraRig,
            bool forceGlobalCameraFieldOfView,
            bool clampLocalCameraFieldOfView,
            string reason)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch || cameraRig == null)
            {
                return;
            }

            try
            {
                CameraZoom cameraZoom = cameraRig.CameraZoom;
                if (cameraZoom != null)
                {
                    cameraZoom.FovGlobalMap = ClampGlobalFov(settings.GlobalMapFov);
                    ApplyLocalMapZoomToCameraZoom(cameraZoom, false, reason);
                }

                Camera camera = cameraRig.Camera;
                if (camera != null && forceGlobalCameraFieldOfView)
                {
                    camera.fieldOfView = ClampGlobalFov(settings.GlobalMapFov);
                }
                else if (camera != null && clampLocalCameraFieldOfView)
                {
                    ClampCurrentLocalCameraFieldOfView(camera, settings);
                }

                Log(
                    "Applied camera zoom settings. globalFov="
                    + ClampGlobalFov(settings.GlobalMapFov).ToString("0.###")
                    + ", localEnabled="
                    + settings.EnableLocalMapZoom
                    + ", localMin="
                    + ClampLocalFov(settings.LocalMapFovMin).ToString("0.###")
                    + ", localMax="
                    + ClampLocalFov(settings.LocalMapFovMax).ToString("0.###")
                    + ", setGlobalCameraFieldOfView="
                    + forceGlobalCameraFieldOfView
                    + ", clampLocalCameraFieldOfView="
                    + clampLocalCameraFieldOfView
                    + ", mapMode="
                    + SafeGetMapMode(cameraRig)
                    + ", currentMode="
                    + SafeGetCurrentMode()
                    + ", reason="
                    + reason
                    + ".");
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to apply camera zoom settings to CameraRig.", ex);
            }
        }

        internal static void ApplyLocalMapZoomToCameraZoom(CameraZoom cameraZoom, string reason)
        {
            ApplyLocalMapZoomToCameraZoom(cameraZoom, true, reason);
        }

        private static void ApplyLocalMapZoomToCameraZoom(CameraZoom cameraZoom, bool requireLocalGameMode, string reason)
        {
            Settings settings = Main.Settings;
            if (settings == null
                || !settings.EnablePatch
                || !settings.EnableLocalMapZoom
                || cameraZoom == null
                || (requireLocalGameMode && !IsCurrentGameModeDefault()))
            {
                return;
            }

            float min = ClampLocalFov(settings.LocalMapFovMin);
            float max = ClampLocalFov(settings.LocalMapFovMax);
            NormalizeLocalFovRange(ref min, ref max);

            cameraZoom.FovMin = min;
            cameraZoom.FovMax = max;
        }

        private static bool ShouldApplyLocalMapZoom(CameraRig cameraRig)
        {
            Settings settings = Main.Settings;
            return settings != null
                && settings.EnablePatch
                && settings.EnableLocalMapZoom
                && IsCurrentGameModeDefault()
                && !SafeGetMapMode(cameraRig);
        }

        private static void ClampCurrentLocalCameraFieldOfView(Camera camera, Settings settings)
        {
            float min = ClampLocalFov(settings.LocalMapFovMin);
            float max = ClampLocalFov(settings.LocalMapFovMax);
            NormalizeLocalFovRange(ref min, ref max);

            float clamped = Mathf.Clamp(camera.fieldOfView, min, max);
            if (!Mathf.Approximately(camera.fieldOfView, clamped))
            {
                camera.fieldOfView = clamped;
            }
        }

        private static float ClampGlobalFov(float value)
        {
            return Mathf.Clamp(value, 10f, MaximumAllowedFov);
        }

        private static float ClampLocalFov(float value)
        {
            return Mathf.Clamp(value, MinimumAllowedFov, MaximumAllowedFov);
        }

        private static void NormalizeLocalFovRange(ref float min, ref float max)
        {
            if (min <= max)
            {
                return;
            }

            float swap = min;
            min = max;
            max = swap;
        }

        private static bool SafeGetMapMode(CameraRig cameraRig)
        {
            try
            {
                return cameraRig != null && cameraRig.GetMapMode();
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCurrentGameModeDefault()
        {
            try
            {
                Game game = Game.Instance;
                return game != null && game.CurrentMode == GameModeType.Default;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCurrentGameModeGlobalMap()
        {
            try
            {
                Game game = Game.Instance;
                if (game == null)
                {
                    return false;
                }

                GameModeType mode = game.CurrentMode;
                return mode == GameModeType.GlobalMap || mode == GameModeType.CutsceneGlobalMap;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeGetCurrentMode()
        {
            try
            {
                Game game = Game.Instance;
                return game != null ? game.CurrentMode.ToString() : "<no game>";
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private static void Log(string message)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.LogDiagnostics || s_diagnosticLogCount >= settings.MaxDiagnosticLogs)
            {
                return;
            }

            s_diagnosticLogCount++;
            Logger.Info(message);
        }
    }
}
