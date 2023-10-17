using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using NarutoMod;
using Thing_TakeDamage_Patch = NarutoMod.Thing_TakeDamage_Patch;
using TaranMagicFramework;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse.Noise;
using System.Reflection.Emit;

namespace WNDE.Dojutsu
{
    // Integrates dojutsu def's ability trees and abilities
    [HarmonyPatch(typeof(CompAbilities), nameof(CompAbilities.RecheckAbilities))]
    public static class WNDE_CompAbilities_RecheckAbilities_Patch
    {
        public static void Postfix(ref CompAbilities __instance)
        {
            foreach (WNDE_Hediff_Dojutsu dojutsu in __instance.Pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>())
            {
                if (dojutsu.DojutsuData.DojutsuDef.stageAbilityTrees != null || dojutsu.DojutsuData.DojutsuDef.stageAbilities != null)
                {
                    WNDE_Ability_Utilities.UnlockAbilities(__instance.Pawn, dojutsu.DojutsuData);
                }
            }
        }
    }

    // Disables the original patch for drawing dojutsu eyes which relied on the gene instead of the hediff
    [HarmonyPatch(typeof(DrawGeneEyes_Patch), nameof(DrawGeneEyes_Patch.Prefix))]
    public static class WNDE_PawnRenderer_DrawHeadHair_Patch
    {
        public static bool Prefix()
        {
            // Disabled
            return false;
        }
    }

    // Implements a similar method to the original patch but instead checks each eye for the hediff instead of the gene before applying graphics
    [HarmonyPatch]
    public static class WNDE_DrawGeneEyes_Patch
    {
        public static MethodBase TargetMethod()
        {
            Type[] nestedTypes = typeof(PawnRenderer).GetNestedTypes(AccessTools.all);
            for (int i = 0; i < nestedTypes.Length; i++)
            {
                foreach (MethodInfo methodInfo in nestedTypes[i].GetMethods(AccessTools.all))
                {
                    if (methodInfo.Name.Contains("DrawExtraEyeGraphic"))
                    {
                        return methodInfo;
                    }
                }
            }
            return null;
        }

        public static bool Prefix(List<GeneGraphicRecord> ___geneGraphics, Graphic graphic, float scale, float yOffset, ref bool drawLeft, ref bool drawRight)
        {
            foreach (GeneGraphicRecord gene in ___geneGraphics)
            {
                Pawn pawn = gene.sourceGene.pawn;
                IEnumerable<WNDE_Hediff_Dojutsu> dojutsuHediffs = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>();
                if (!dojutsuHediffs.Any())
                {
                    return true;
                }
                BodyPartRecord leftEye = pawn.def.race.body.AllParts.FirstOrDefault((BodyPartRecord p) => p.woundAnchorTag == "LeftEye");
                BodyPartRecord rightEye = pawn.def.race.body.AllParts.FirstOrDefault((BodyPartRecord p) => p.woundAnchorTag == "RightEye");
                foreach (WNDE_Hediff_Dojutsu dojutsu in dojutsuHediffs.Where(x => x.DojutsuData.DojutsuDef.DojutsuGeneDef == gene.sourceGene.def))
                {
                    if (dojutsu.DojutsuData.DojutsuDef.DrawnByDefault)
                    {
                        continue;
                    }
                    if (!dojutsu.DojutsuData.DojutsuActive)
                    {
                        if (dojutsu.Part == leftEye)
                        {
                            drawLeft = false;
                        }
                        if (dojutsu.Part == rightEye)
                        {
                            drawRight = false;
                        }
                    }
                }
            }

            return true;
        }
    }

    // Idk what this does but I updated it just to tie loose ends
    // Yields errors (probably an easy fix but I have no idea what this does so not a priority)
    //[HarmonyPatch(typeof(Thing_TakeDamage_Patch), nameof(Thing_TakeDamage_Patch.HasSharinganOrByakuganActive))]
    //public static class WNDE_Thing_TakeDamage_Patch
    //{
    //    public static bool Prefix(this Pawn pawn)
    //    {
    //        WNDE_Hediff_Dojutsu hediffSharingan = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
    //                                                Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan).First() as WNDE_Hediff_Dojutsu;
    //        if (hediffSharingan == null)
    //        {
    //            WNDE_Hediff_Dojutsu hediffByakugan = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
    //                                                    Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Byakugan).First() as WNDE_Hediff_Dojutsu;

    //            bool? flag = (hediffByakugan != null) ? new bool?(hediffByakugan.DojutsuData.DojutsuActive) : null;
    //            return flag != null && !flag.GetValueOrDefault();
    //        }
    //        return hediffSharingan.DojutsuData.DojutsuActive;
    //    }
    //}

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
}
