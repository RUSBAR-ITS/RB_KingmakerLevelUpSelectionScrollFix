using System;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Formations;
using UnityEngine;

namespace KingmakerLargePartyFix
{
    internal static class LargePartyFormationLogic
    {
        private static int s_virtualOffsetLogs;
        private static int s_expandLogs;
        private static int s_contextLogs;

        internal static void ResetDiagnostics()
        {
            s_virtualOffsetLogs = 0;
            s_expandLogs = 0;
            s_contextLogs = 0;
        }

        internal static bool ShouldGenerateExtraOffsets
        {
            get
            {
                return Main.Settings != null
                    && Main.Settings.EnablePatch
                    && Main.Settings.GenerateExtraFormationOffsets;
            }
        }

        internal static bool ShouldExpandCustomFormations
        {
            get
            {
                return Main.Settings != null
                    && Main.Settings.EnablePatch
                    && Main.Settings.ExpandCustomFormations;
            }
        }

        internal static bool NeedsGeneratedOffset(Vector2[] positions, int index)
        {
            return ShouldGenerateExtraOffsets
                && index >= 0
                && (positions == null || index >= positions.Length);
        }

        internal static Vector2 GenerateScaledOffset(Vector2[] positions, int index)
        {
            Vector2 unscaled = GenerateUnscaledOffset(positions, index, positions != null ? positions.Length : 0);
            Vector2 scaled = unscaled * GetFormationScale();

            LogVirtualOffset(positions, index, unscaled, scaled);

            return scaled;
        }

        internal static bool EnsureCurrentCustomFormationCapacity(string reason)
        {
            if (!ShouldExpandCustomFormations)
            {
                return false;
            }

            try
            {
                Game game = Game.Instance;
                if (game == null || game.Player == null)
                {
                    return false;
                }

                CustomPartyFormation formation = game.Player.Formation as CustomPartyFormation;
                if (formation == null)
                {
                    LogFormationContext(reason + ": current formation is not custom.");
                    return false;
                }

                return EnsureCapacity(formation, GetCurrentControllableCount(), reason);
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to ensure current custom formation capacity.", ex);
                return false;
            }
        }

        internal static bool EnsureCapacity(CustomPartyFormation formation, int requiredLength, string reason)
        {
            if (!ShouldExpandCustomFormations || formation == null || requiredLength <= 0)
            {
                return false;
            }

            Vector2[] positions = formation.Positions;
            int oldLength = positions != null ? positions.Length : 0;
            if (oldLength >= requiredLength)
            {
                return false;
            }

            Vector2[] expanded = new Vector2[requiredLength];
            if (positions != null && positions.Length > 0)
            {
                Array.Copy(positions, expanded, positions.Length);
            }

            for (int i = oldLength; i < expanded.Length; i++)
            {
                expanded[i] = GenerateUnscaledOffset(expanded, i, oldLength);
            }

            formation.Positions = expanded;

            LogExpansion(reason, oldLength, requiredLength);

            return true;
        }

        internal static int GetCurrentControllableCount()
        {
            try
            {
                Game game = Game.Instance;
                if (game == null || game.Player == null || game.Player.ControllableCharacters == null)
                {
                    return 0;
                }

                return game.Player.ControllableCharacters.Count;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to read current controllable character count.", ex);
                return 0;
            }
        }

        private static Vector2 GenerateUnscaledOffset(Vector2[] positions, int index, int baseCount)
        {
            Settings settings = Main.Settings ?? new Settings();
            int columns = Mathf.Clamp(settings.ExtraColumns, 1, 12);
            float spacingX = Mathf.Clamp(settings.ExtraHorizontalSpacing, 0.5f, 6f);
            float spacingY = Mathf.Clamp(settings.ExtraVerticalSpacing, 0.5f, 6f);

            Bounds2D bounds;
            bool hasBounds = TryCalculateBounds(positions, baseCount, out bounds);

            int effectiveBaseCount = Mathf.Max(0, baseCount);
            int extraIndex = Mathf.Max(0, index - effectiveBaseCount);
            int row = extraIndex / columns;
            int column = extraIndex % columns;

            float centerX = hasBounds ? (bounds.MinX + bounds.MaxX) * 0.5f : 0f;
            float x = centerX + (column - (columns - 1) * 0.5f) * spacingX;
            float y = hasBounds ? bounds.MinY - (row + 1) * spacingY : -row * spacingY;

            return new Vector2(x, y);
        }

        private static bool TryCalculateBounds(Vector2[] positions, int sourceLength, out Bounds2D bounds)
        {
            bounds = default(Bounds2D);

            if (positions == null || positions.Length == 0 || sourceLength <= 0)
            {
                return false;
            }

            int length = Mathf.Min(positions.Length, sourceLength);
            bool initialized = false;

            for (int i = 0; i < length; i++)
            {
                Vector2 point = positions[i];
                if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y))
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds.MinX = point.x;
                    bounds.MaxX = point.x;
                    bounds.MinY = point.y;
                    bounds.MaxY = point.y;
                    initialized = true;
                    continue;
                }

                bounds.MinX = Mathf.Min(bounds.MinX, point.x);
                bounds.MaxX = Mathf.Max(bounds.MaxX, point.x);
                bounds.MinY = Mathf.Min(bounds.MinY, point.y);
                bounds.MaxY = Mathf.Max(bounds.MaxY, point.y);
            }

            return initialized;
        }

        private static float GetFormationScale()
        {
            try
            {
                BlueprintRoot root = BlueprintRoot.Instance;
                if (root != null && root.Formations != null && root.Formations.FormationsScale > 0f)
                {
                    return root.Formations.FormationsScale;
                }
            }
            catch
            {
                // BlueprintRoot may not be available very early during loading. Formation scale 1 is a safe fallback.
            }

            return 1f;
        }

        private static void LogVirtualOffset(Vector2[] positions, int index, Vector2 unscaled, Vector2 scaled)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.LogDiagnostics || s_virtualOffsetLogs >= settings.MaxDiagnosticLogs)
            {
                return;
            }

            s_virtualOffsetLogs++;
            int length = positions != null ? positions.Length : 0;
            Logger.Info(
                "Generated extra formation offset: index="
                + index
                + ", vanillaLength="
                + length
                + ", unscaled="
                + FormatVector(unscaled)
                + ", scaled="
                + FormatVector(scaled)
                + ".");
        }

        private static void LogExpansion(string reason, int oldLength, int newLength)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.LogDiagnostics || s_expandLogs >= settings.MaxDiagnosticLogs)
            {
                return;
            }

            s_expandLogs++;
            Logger.Info(
                "Expanded custom formation positions: "
                + oldLength
                + " -> "
                + newLength
                + " ("
                + reason
                + ").");
        }

        private static void LogFormationContext(string message)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.LogDiagnostics || s_contextLogs >= settings.MaxDiagnosticLogs)
            {
                return;
            }

            s_contextLogs++;
            Logger.Info(message);
        }

        private static string FormatVector(Vector2 vector)
        {
            return "(" + vector.x.ToString("0.###") + ", " + vector.y.ToString("0.###") + ")";
        }

        private struct Bounds2D
        {
            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;
        }
    }
}
