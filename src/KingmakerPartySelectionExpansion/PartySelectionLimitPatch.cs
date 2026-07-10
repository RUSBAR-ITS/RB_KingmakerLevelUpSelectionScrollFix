using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.UI.Group;

namespace KingmakerPartySelectionExpansion
{
    internal static class PartySelectionLimitTranspiler
    {
        private static readonly MethodInfo GetMaxActivePartySizeMethod =
            AccessTools.Method(typeof(PartySelectionLimit), nameof(PartySelectionLimit.GetMaxActivePartySize));

        internal static IEnumerable<CodeInstruction> ReplaceVanillaPartyLimit(
            IEnumerable<CodeInstruction> instructions,
            MethodBase originalMethod)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int replacements = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldc_I4_6)
                {
                    continue;
                }

                codes[i] = new CodeInstruction(OpCodes.Call, GetMaxActivePartySizeMethod)
                {
                    labels = codes[i].labels,
                    blocks = codes[i].blocks
                };
                replacements++;
            }

            Logger.Info(
                "Patched party size limit constants in "
                + originalMethod.DeclaringType.FullName
                + "."
                + originalMethod.Name
                + ": replacements="
                + replacements
                + ".");

            return codes;
        }
    }

    [HarmonyPatch(typeof(GroupManager), nameof(GroupManager.SwitchCharacter))]
    internal static class GroupManagerSwitchCharacterLimitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            return PartySelectionLimitTranspiler.ReplaceVanillaPartyLimit(instructions, __originalMethod);
        }
    }

    [HarmonyPatch(typeof(GroupManager), nameof(GroupManager.AllToParty))]
    internal static class GroupManagerAllToPartyLimitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            return PartySelectionLimitTranspiler.ReplaceVanillaPartyLimit(instructions, __originalMethod);
        }
    }

    [HarmonyPatch(typeof(GroupManager), "MoveExtraCharactersToRemote")]
    internal static class GroupManagerMoveExtraCharactersToRemoteLimitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            return PartySelectionLimitTranspiler.ReplaceVanillaPartyLimit(instructions, __originalMethod);
        }
    }

    [HarmonyPatch(typeof(GroupManager), "SetFakeGroup")]
    internal static class GroupManagerSetFakeGroupLimitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            return PartySelectionLimitTranspiler.ReplaceVanillaPartyLimit(instructions, __originalMethod);
        }
    }

    [HarmonyPatch(typeof(GroupManager), "Fill")]
    internal static class GroupManagerFillLimitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            return PartySelectionLimitTranspiler.ReplaceVanillaPartyLimit(instructions, __originalMethod);
        }
    }

    [HarmonyPatch(typeof(MaxPartySize), nameof(MaxPartySize.GetValue))]
    internal static class MaxPartySizeGetValuePatch
    {
        private static bool Prefix(ref int __result)
        {
            __result = PartySelectionLimit.GetMaxActivePartySize();
            return false;
        }
    }
}
