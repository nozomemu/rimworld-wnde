using HarmonyLib;
using NarutoMod;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TaranMagicFramework;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static Verse.DamageWorker;
using Ability = TaranMagicFramework.Ability;

namespace WNDE.Dojutsu.Sharingan
{
    public class WNDE_Ability_BaseSharingan : Ability_Toggleable
    {
        public override float? ResourceRegenRate
        {
            get
            {
                float? num = base.ResourceRegenRate;
                if (!this.pawn.genes.HasEndogene(WN_DefOf.WN_Sharingan))
                {
                    num *= 2f;
                }
                return num;
            }
        }

        public override void Start(bool consumeEnergy = true)
        {
            base.Start(consumeEnergy);
            if (!this.pawn.genes.HasEndogene(WN_DefOf.WN_Sharingan))
            {
                Log.Error("Attempted to activate Sharingan without endogene");
                return;
            }
            IEnumerable<WNDE_Hediff_Dojutsu> sharinganHediffs = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                                    Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan);
            foreach (WNDE_Hediff_Dojutsu hediffSharingan in sharinganHediffs)
            {
                hediffSharingan.DojutsuData.DojutsuActive = true;
            }
        }

        public override void End()
        {
            base.End();
            if (!this.pawn.genes.HasEndogene(WN_DefOf.WN_Sharingan))
            {
                // Skip deactivation for non-Uchiha/endogene wielders
                return;
            }
            IEnumerable<WNDE_Hediff_Dojutsu> sharinganHediffs = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                                    Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan);
            foreach (WNDE_Hediff_Dojutsu hediffSharingan in sharinganHediffs)
            {
                hediffSharingan.DojutsuData.DojutsuActive = false;
            }
        }

        public override Func<string> CanBeActivatedValidator()
        {
            return delegate
            {
                if (!pawn.health.hediffSet.GetNotMissingParts().Any((BodyPartRecord x) => x.def == BodyPartDefOf.Eye))
                {
                    return "WN.SharinganEyeMustBeImplanted".Translate();
                }
                return (pawn.health.hediffSet.hediffs.Where(x => x.Part?.def == BodyPartDefOf.Eye).ToList().Count(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan) <= 0) ? ((string)"WN.SharinganEyeMustBeImplanted".Translate()) : "";
            };
        }
    }

    // Quick fix for Genjutsu: Sharingan to require the hediff active instead of the ability active
    public class WNDE_Ability_SharinganGenjutsu : Ability_SharinganGenjutsu
    {
        public override Func<string> CanBeActivatedValidator()
        {
            return delegate
            {
                if (!pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Sharingan))
                {
                    return "WN.SharinganEyeMustBeImplanted".Translate();
                }
                bool anyActiveSharingan = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                            Where((WNDE_Hediff_Dojutsu x) => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan && x.DojutsuData.DojutsuActive).Any();
                return (!anyActiveSharingan) ? ((string)"WNDE_SharinganMustBeActive".Translate()) : "";
            };
        }
    }

    // Adds most of the "awakening" and "upgrading" features of the Sharingan to Gene_Sharingan.TryLearnTomoe
    [HarmonyPatch(typeof(Gene_Sharingan), nameof(Gene_Sharingan.TryLearnTomoe))]
    public static class Gene_Sharingan_TryLearnTomoe_Patch
    {
        public static bool Prefix(ref Gene_Sharingan __instance)
        {
            // Only proceed if the gene is "innate" or an endogene, since xenogene dojutsu are supposed to represent transplants or non-Uchiha
            if (!__instance.pawn.genes.HasEndogene(WN_DefOf.WN_Sharingan))
            {
                return false;
            }

            AbilityClass abilityClass = __instance.pawn.GetComp<CompAbilities>().GetAbilityClass(WN_DefOf.WN_Clan);
            bool notAwakened = !__instance.pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Sharingan);
            if (notAwakened)
            {
                Awaken(__instance.pawn, abilityClass);
                return false;
            }

            // This indiscriminately checks and upgrades Sharingan regardless if the eyes belong to someone else
            // Since it is said only Uchiha blood has the unique chakra to awaken and (de)activate the Sharingan, we can assume this applies to upgrading
            // However, we do not know if the eyes have their own reserve of "Uchiha chakra" or if the Uchiha blood is what funnels chakra to the eyes during trauma, thus (cont.)
            // (cont.) it is unclear whether the eyes are capable of upgrading in a non-Uchiha since we don't have any cases of immature (non-3 tomoe) Sharingan upgrading
            // Hence we get the Kakashi MS dilemma of did he awaken his MS because he also saw Rin die or because it was linked to Obito, and if this applies to base Sharingan too
            // For the sake of simplicity and performance, we'll assume for now the upgrades occur from the current wielder instead of the origin
            // i.e. if you have the (endo)gene (MS may be handled differently) it upgrades and syncs all the Sharingan hediffs currently in the Pawn

            WNDE_Hediff_Dojutsu hediffHighestStage = __instance.pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                        Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan).OrderByDescending(x => x.CurStageIndex).First();
            int currentHighestStage = hediffHighestStage.CurStageIndex;
            if (currentHighestStage < 2)
            {
                abilityClass.LearnAbility(WNDE_DefOf.WNDE_Ability_BaseSharingan, false, currentHighestStage + 1);
                foreach (WNDE_Hediff_Dojutsu sharinganEye in __instance.pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                                Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan))
                {
                    sharinganEye.DojutsuData.DojutsuStage = currentHighestStage + 1;
                    sharinganEye.SetStage();
                }
                abilityClass.GetLearnedAbility(WNDE_DefOf.WNDE_Ability_BaseSharingan).Start(true);
                string tomoeCount = (hediffHighestStage.CurStage.label).Remove(1, hediffHighestStage.CurStage.label.Length - 1);
                Find.LetterStack.ReceiveLetter("WNDE_SharinganTomoeGained".Translate(),
                    "WNDE_SharinganTomoeGained_Desc".Translate(__instance.pawn.Named("PAWN"), tomoeCount.Named("TOMOE")),
                    LetterDefOf.PositiveEvent, __instance.pawn, null, null, null, null);
            }

            return false;
        }

        public static void Awaken(Pawn pawn, AbilityClass abilityClass)
        {
            foreach (BodyPartRecord eyePart in pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Eye))
            {
                bool notMissing = !pawn.health.hediffSet.PartIsMissing(eyePart);
                bool noAddedPart = !pawn.health.hediffSet.hediffs.OfType<Hediff_AddedPart>().Where(x => x.Part == eyePart).Any();
                bool noDojutsu = !pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().Where(x => x.Part == eyePart).Any();
                if (notMissing && noAddedPart && noDojutsu)
                {
                    WNDE_Hediff_Dojutsu sharinganEye = HediffMaker.MakeHediff(WNDE_DefOf.WNDE_Hediff_Sharingan, pawn, eyePart) as WNDE_Hediff_Dojutsu;
                    sharinganEye.DojutsuData.DojutsuDef = WNDE_DefOf.WNDE_Dojutsu_Sharingan;
                    pawn.health.AddHediff(sharinganEye, eyePart, (DamageInfo?)null, (DamageResult)null);
                }
            }
            bool recheckForSharingan = pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Sharingan);
            if (!recheckForSharingan)
            {
                return;
            }
            if (!abilityClass.Learned(WNDE_DefOf.WNDE_Ability_BaseSharingan))
            {
                abilityClass.LearnAbility(WNDE_DefOf.WNDE_Ability_BaseSharingan, false, 0);
            }
            if (pawn.health.hediffSet.GetFirstHediffOfDef(WNDE_DefOf.WNDE_Hediff_Sharingan) != null)
            {
                abilityClass.GetLearnedAbility(WNDE_DefOf.WNDE_Ability_BaseSharingan).Start(true);
                Find.LetterStack.ReceiveLetter("WNDE_SharinganAwakened".Translate(),
                    "WNDE_SharinganAwakened_Desc".Translate(pawn.Named("PAWN")),
                    LetterDefOf.PositiveEvent, pawn, null, null, null, null);
            }
        }
    }

    // Makes it so the Sharingan gene being active is reliant on an existing Sharingan hediff being active, as opposed to the ability being active
    [HarmonyPatch(typeof(Gene_Sharingan), nameof(Gene_Sharingan.Active), MethodType.Getter)]
    public static class Gene_Sharingan_Active_Patch
    {
        public static bool Prefix(ref Gene_Sharingan __instance, ref bool __result)
        {
            bool hasActiveSharingan = false;
            foreach (WNDE_Hediff_Dojutsu sharinganEye in __instance.pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                            Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Sharingan))
            {
                if (sharinganEye.DojutsuData.DojutsuActive)
                {
                    hasActiveSharingan = true;
                    break;
                }
            }
            __result = Active(__instance) && hasActiveSharingan;
            return false;
        }

        public static bool Active(Gene_Sharingan __instance)
        {
            if (__instance.Overridden)
            {
                return false;
            }
            Pawn pawn = __instance.pawn;
            return ((pawn != null) ? pawn.ageTracker : null) == null || (float)__instance.pawn.ageTracker.AgeBiologicalYears >= __instance.def.minAgeActive;
        }
    }

    // Replaces the WN_SharinganTomoe ability in Gene_Sharingan.Ability with the new ability made
    // I'm not sure where else this is called other than Gene_Sharingan.Active, but I still patched it to tie any potential loose ends
    [HarmonyPatch(typeof(Gene_Sharingan), nameof(Gene_Sharingan.Ability), MethodType.Getter)]
    public static class Gene_Sharingan_Ability_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldsfld && code[i].operand == AccessTools.Field(typeof(WN_DefOf), nameof(WN_DefOf.WN_SharinganTomoe)))
                {
                    code[i].operand = AccessTools.Field(typeof(WNDE_DefOf), nameof(WNDE_DefOf.WNDE_Ability_BaseSharingan));
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

    // Patches it so leveling up an ability class checks if any dojutsu should be upgraded, like the acquireRequirement in AbilityTierDefs, since we are now hediff-based
    // Ideally, either this feature should be built into the framework, or this patch should just call a "TryProgressStage" in the WNDE_Hediff_Dojutsu so it's cleaner and flexible
    // The latter could make it so you can specify requirements -- e.g. minimum age, minimum ability class levels -- in the DojutsuDef like how AbilityTierDefs does it
    // However, since this feature is only used by the Sharingan, I just patched everything here for now
    [HarmonyPatch(typeof(AbilityClass), "OnLevelGained")]
    public static class AbilityClass_OnLevelGained_Patch
    {
        private static void Postfix(ref AbilityClass __instance)
        {
            AbilityClass ninjutsuClass = __instance.pawn.GetComp<CompAbilities>().GetAbilityClass(WN_DefOf.WN_Ninjutsu);
            bool hasInnateSharingan = __instance.pawn.genes.HasEndogene(WN_DefOf.WN_Sharingan);

            if (hasInnateSharingan)
            {
                Gene_Sharingan geneSharingan = __instance.pawn.genes.GetGene(WN_DefOf.WN_Sharingan) as Gene_Sharingan;
                bool noSharingan = !__instance.pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Sharingan);

                if (noSharingan)
                {
                    if (5 <= ninjutsuClass.level && ninjutsuClass.level < 8)
                    {
                        geneSharingan.TryLearnTomoe();
                    }
                }
                else
                {
                    int currentHighestStage = (from x in __instance.pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>()
                                               where x.def == WNDE_DefOf.WNDE_Hediff_Sharingan
                                               select x).OrderByDescending(x => x.CurStageIndex).First().CurStageIndex;
                    if (8 <= ninjutsuClass.level && ninjutsuClass.level < 14 && currentHighestStage < 1)
                    {
                        geneSharingan.TryLearnTomoe();
                    }
                    if (14 <= ninjutsuClass.level && currentHighestStage < 2)
                    {
                        geneSharingan.TryLearnTomoe();
                    }
                }
            }
        }
    }
}
