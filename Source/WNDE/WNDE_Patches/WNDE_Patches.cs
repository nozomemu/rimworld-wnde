using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using FacialAnimation;
using UnityEngine;
using WNDE.Dojutsu;
using System.Reflection.Emit;

namespace WNDE_Patches
{
    [StaticConstructorOnStartup]
    public class WNDE_Patches : Mod
    {
        public WNDE_Patches(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("NozoMeMu.WorldOfNaruto.DojutsuExpanded.Patches");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // Integrates checks to determine if eye is drawn by default, e.g. Byakugan, (and if not, e.g. Sharingan, checks if it is active) before assigning color to the eye
    // "Deactivated" eyes are uncolored (black) by default, but since I've also separated dojutsu genes from eye color genes, they default to any eye color gene present
    // Note: White eyes may not necessarily be Byakugan possessers' "deactivated" color (see Himawari), but I'll leave it drawn by default since that's also how the base mod does it
    [HarmonyPatch(typeof(EyeballControllerComp), "LoadTextures")]
    public static class WNDE_FacialAnimations_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            Label firstJump = il.DefineLabel();
            Label replacedJump = il.DefineLabel();
            LocalBuilder newHediff = il.DeclareLocal(typeof(Hediff));
            LocalBuilder newDojutsu = il.DeclareLocal(typeof(WNDE_Hediff_Dojutsu));

            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<CodeInstruction> patchedCode = new List<CodeInstruction>();
            List<CodeInstruction> skippedCode = new List<CodeInstruction>();
            bool patchedMain = false;

            for (int i = 0; i < code.Count; i++)
            {
                if (patchedMain && skippedCode.Contains(code[i]))
                {
                    continue;
                }
                else if (patchedMain && code[i].opcode == OpCodes.Callvirt &&
                    code[i].operand.ToString().Contains("get_Part") &&
                    code[i + 1].opcode == OpCodes.Ldloc_2 &&
                    code[i + 2].opcode == OpCodes.Bne_Un_S &&
                    code[i + 3].opcode == OpCodes.Ldloc_S &&
                    code[i + 4].opcode == OpCodes.Ldfld)
                {
                    code[i - 5].operand = replacedJump;

                    CodeInstruction codeAfterJump = new CodeInstruction(OpCodes.Ldloc_S, newHediff) { labels = { replacedJump } };
                    patchedCode.Add(codeAfterJump);
                    yield return codeAfterJump;
                    patchedCode.Add(code[i]);
                    yield return code[i];
                }
                else if (!patchedMain && code[i].opcode == OpCodes.Ldloc_S &&
                    code[i + 1].opcode == OpCodes.Callvirt &&
                    code[i + 1].operand.ToString().Contains("get_Current") &&
                    code[i + 3].opcode == OpCodes.Callvirt &&
                    code[i + 3].operand.ToString().Contains("get_Part") &&
                    code[i + 4].opcode == OpCodes.Ldloc_1)
                {
                    skippedCode.Add(code[i + 1]);
                    skippedCode.Add(code[i + 2]);

                    List<CodeInstruction> newCodes = new List<CodeInstruction>
                    {
                        code[i],
                        code[i + 1],
                        new CodeInstruction(OpCodes.Stloc_S, newHediff),
                        new CodeInstruction(OpCodes.Ldloc_S, newHediff),
                        new CodeInstruction(OpCodes.Isinst, typeof(WNDE_Hediff_Dojutsu)),
                        new CodeInstruction(OpCodes.Brfalse_S, firstJump),

                        new CodeInstruction(OpCodes.Ldloc_S, newHediff),
                        new CodeInstruction(OpCodes.Isinst, typeof(WNDE_Hediff_Dojutsu)),
                        new CodeInstruction(OpCodes.Stloc_S, newDojutsu),
                        new CodeInstruction(OpCodes.Ldloc_S, newDojutsu),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(WNDE_Hediff_Dojutsu), "get_DojutsuData")),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(WNDE_DojutsuData), "get_DojutsuDef")),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(WNDE_DojutsuDef), "get_DrawnByDefault")),
                        new CodeInstruction(OpCodes.Brtrue_S, firstJump),

                        new CodeInstruction(OpCodes.Ldloc_S, newDojutsu),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(WNDE_Hediff_Dojutsu), "get_DojutsuData")),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(WNDE_DojutsuData), "get_DojutsuActive")),
                        new CodeInstruction(OpCodes.Brfalse_S, code[i + 12].operand),

                        new CodeInstruction(OpCodes.Ldloc_S, newHediff) { labels = { firstJump } }
                    };

                    foreach (CodeInstruction instruction in newCodes)
                    {
                        patchedCode.Add(instruction);
                        yield return instruction;
                    }

                    patchedMain = true;
                    i = 0;
                    continue;
                }
                else if (!patchedCode.Contains(code[i]))
                {
                    patchedCode.Add(code[i]);
                    yield return code[i];
                }

            }
            yield break;
        }
    }
}
