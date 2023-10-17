using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaranMagicFramework;
using Verse;

namespace WNDE
{
    // GOD MODE GIZMOS FIX
    // Small personal fix: Replaces the DevMode condition in CompAbilities for debug gizmos to be GodMode instead, to be consistent with vanilla
    [HarmonyPatch]
    public static class WNDE_CompGetGizmosExtra_Patch
    {
        public static MethodBase TargetMethod()
        {
            Type nestedType = typeof(CompAbilities).GetNestedTypes(AccessTools.all).First((Type c) => c.Name.Contains("d__21"));
            MethodInfo methodInfo = AccessTools.Method(nestedType, "MoveNext");
            return methodInfo;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Call && code[i].operand == AccessTools.Method(typeof(Prefs), "get_DevMode"))
                {
                    code[i].opcode = OpCodes.Ldsfld;
                    code[i].operand = AccessTools.Field(typeof(DebugSettings), "godMode");
                    yield return code[i];
                }
                else
                {
                    yield return code[i];
                }
            }
            yield break;
        }
    }

    // GOD MODE UNLOCKS EVERY JUTSU FIX
    // Fixes the bug where enabling GodMode causes you to learn every unlocked/learnable jutsu every 2500 ticks
    [HarmonyPatch(typeof(AbilityClass), nameof(AbilityClass.CanUnlockNextTier))]
    public static class WNDE_AbilityClass_CanUnlockNextTier_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldsfld && code[i].operand == AccessTools.Field(typeof(DebugSettings), "godMode"))
                {
                    code[i].opcode = OpCodes.Nop;
                    code[i + 1].opcode = OpCodes.Nop;
                    code[i + 2].opcode = OpCodes.Nop;
                    code[i + 3].opcode = OpCodes.Nop;
                    yield return code[i];
                }
                else
                {
                    yield return code[i];
                }
            }
            yield break;
        }
    }
}
