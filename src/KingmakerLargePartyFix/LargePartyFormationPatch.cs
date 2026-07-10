using System;
using HarmonyLib;
using Kingmaker.Formations;
using Kingmaker.UI.Formation;
using UnityEngine;

namespace KingmakerLargePartyFix
{
    [HarmonyPatch(typeof(PartyFormationHelper), nameof(PartyFormationHelper.GetOffset))]
    internal static class PartyFormationHelperGetOffsetPatch
    {
        private static bool Prefix(Vector2[] positions, int index, ref Vector2 __result)
        {
            try
            {
                if (!LargePartyFormationLogic.NeedsGeneratedOffset(positions, index))
                {
                    return true;
                }

                __result = LargePartyFormationLogic.GenerateScaledOffset(positions, index);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to generate extra formation offset.", ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CustomPartyFormation), nameof(CustomPartyFormation.SetOffset))]
    internal static class CustomPartyFormationSetOffsetPatch
    {
        private static bool Prefix(CustomPartyFormation __instance, int index, Vector2 pos)
        {
            try
            {
                if (!LargePartyFormationLogic.ShouldExpandCustomFormations || __instance == null || index < 0)
                {
                    return true;
                }

                int currentLength = __instance.Positions != null ? __instance.Positions.Length : 0;
                if (index < currentLength)
                {
                    return true;
                }

                LargePartyFormationLogic.EnsureCapacity(__instance, index + 1, "CustomPartyFormation.SetOffset");
                __instance.Positions[index] = pos;

                return false;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to expand custom formation while saving an offset.", ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Kingmaker.Player), nameof(Kingmaker.Player.SureCustomFormation))]
    internal static class PlayerSureCustomFormationPatch
    {
        private static void Postfix(CustomPartyFormation __result)
        {
            try
            {
                if (!LargePartyFormationLogic.ShouldExpandCustomFormations || __result == null)
                {
                    return;
                }

                LargePartyFormationLogic.EnsureCapacity(
                    __result,
                    LargePartyFormationLogic.GetCurrentControllableCount(),
                    "Player.SureCustomFormation");
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to expand custom formation after creation.", ex);
            }
        }
    }

    [HarmonyPatch(typeof(FormationWindow), "SetFormationScreen")]
    internal static class FormationWindowSetFormationScreenPatch
    {
        private static void Prefix()
        {
            try
            {
                LargePartyFormationLogic.EnsureCurrentCustomFormationCapacity("FormationWindow.SetFormationScreen");
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to prepare formation screen for a large party.", ex);
            }
        }
    }
}
