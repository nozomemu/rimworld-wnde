using HarmonyLib;
using TaranMagicFramework;
using NarutoMod;
using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using WNDE.Dojutsu;
using AbilityDef = TaranMagicFramework.AbilityDef;
using UnityEngine;
using static Verse.DamageWorker;

namespace WNDE.Dojutsu
{
    public class WNDE_Hediff_Dojutsu : Hediff_Implant
    {
        private WNDE_DojutsuData dojutsuData;

        public override bool ShouldRemove
        {
            get
            {
                return false;
            }
        }

        public WNDE_DojutsuData DojutsuData
        {
            get
            {
                bool flag = dojutsuData == null;
                if (flag)
                {
                    dojutsuData = new WNDE_DojutsuData();
                    dojutsuData.SaveOwnerData(pawn, WNDE_DefOf.WNDE_Dojutsu_Unknown, null, false);
                }
                return dojutsuData;
            }
            set
            {
                dojutsuData = value;
            }
        }

        public override string Label
        {
            get
            {
                string originalOwner = null;

                bool hasOriginalOwner = DojutsuData.HasOriginalOwner;
                if (hasOriginalOwner && DojutsuData.OriginalOwner != pawn)
                {
                    // Flavor text for if the wielder is not the original owner
                    originalOwner = DojutsuData.OriginalOwner.Name.ToStringShort + "WNDE_Possessive".Translate();
                }
                return originalOwner + base.Label;
            }
        }

        public override Color LabelColor
        {
            get
            {
                if (!DojutsuData.DojutsuActive)
                {
                    return Color.grey;
                }
                return base.LabelColor;
            }
        }

        public override string TipStringExtra
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                string dojutsuStatus = (DojutsuData.DojutsuActive) ? "WNDE_DojutsuStatusActive" : "WNDE_DojutsuStatusInactive";
                stringBuilder.AppendLine("WNDE_DojutsuStatus".Translate() + ": " + dojutsuStatus.Translate());

                if (DojutsuData != null)
                {
                    stringBuilder.AppendLine("WNDE_OriginalOwner".Translate() + ": " + DojutsuData.OriginalOwner.Name.ToStringFull);
                }
                stringBuilder.Append("\n");
                if (DojutsuData.DojutsuActive)
                {
                    stringBuilder.AppendLine("WNDE_ActiveEffects".Translate());
                    stringBuilder.AppendLine("  - " + "WNDE_DrainRate".Translate() + ": " + DojutsuData.DojutsuDef.DrainRate(DojutsuData.DojutsuStage));

                    if (pawn.health.hediffSet.hediffs.Where(x => x.def == DojutsuData.DojutsuDef.ActiveHediffDef && x.Part == Part).Any())
                    {
                        Hediff activeHediff = pawn.health.hediffSet.hediffs.Where(x => x.def == DojutsuData.DojutsuDef.ActiveHediffDef && x.Part == Part).First();
                        foreach (StatDrawEntry item in HediffStatsUtility.SpecialDisplayStats(activeHediff.CurStage, activeHediff))
                        {
                            if (item.ShouldDisplay)
                            {
                                stringBuilder.AppendLine("  - " + item.LabelCap + ": " + item.ValueString);
                            }
                        }
                    }
                }

                stringBuilder.Append(base.TipStringExtra);
                return stringBuilder.ToString();
            }
        }


        // If the hediff has stages, synchronizes the current hediff stage with the saved "DojutsuStage" if they desync for some reason
        public void SetStage()
        {
            if (def.stages != null && def.stages.Count > 1)
            {
                int dojutsuStage = DojutsuData.DojutsuStage;
                Severity = def.stages[dojutsuStage].minSeverity;
                DojutsuData.StageName = CurStage.label;
                return;
            }
            DojutsuData.DojutsuStage = 0;
            return;
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            pawn.health.RestorePart(Part, this, false);
            for (int i = 0; i < Part.parts.Count; i++)
            {
                Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, null);
                hediff_MissingPart.IsFresh = true;
                hediff_MissingPart.lastInjury = HediffDefOf.SurgicalCut;
                hediff_MissingPart.Part = Part.parts[i];
                pawn.health.hediffSet.AddDirect(hediff_MissingPart, null, null);
            }

            // If transplanted to someone without the associated gene, adds a xenogene to represent the transplant
            if (DojutsuData.DojutsuDef.DojutsuGeneDef != null)
            {
                bool implantedDojutsu = !pawn.genes.HasEndogene(DojutsuData.DojutsuDef.DojutsuGeneDef);
                if (implantedDojutsu)
                {
                    pawn.genes.AddGene(DojutsuData.DojutsuDef.DojutsuGeneDef, true);
                }
            }
            // Synchronizes stage and abilities
            SetStage();
            WNDE_Ability_Utilities.UnlockAbilities(pawn, dojutsuData);
        }

        public override void Notify_PawnDied()
        {
            bool flag = !DojutsuData.HasOriginalOwner;
            if (flag)
            {
                DojutsuData.SaveOwnerData(pawn, DojutsuData.DojutsuDef, CurStage.label, DojutsuData.DojutsuActive, CurStageIndex);
            }
            base.Notify_PawnDied();
        }

        public override void Notify_PawnKilled()
        {
            bool flag = !DojutsuData.HasOriginalOwner;
            if (flag)
            {
                DojutsuData.SaveOwnerData(pawn, DojutsuData.DojutsuDef, CurStage.label, DojutsuData.DojutsuActive, CurStageIndex);
            }
            base.Notify_PawnKilled();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            // Removes any active hediffs
            HandleActiveHediffs(false);

            // Removes the associated xenogene if transplanted and no remaining dojutsu of that type
            // Ends any activator abilities if applicable
            bool hasRemainingDojutsu = pawn.health.hediffSet.HasHediff(this.def);
            if (!hasRemainingDojutsu)
            {
                bool implantedDojutsu = !pawn.genes.HasEndogene(DojutsuData.DojutsuDef.DojutsuGeneDef);
                if (implantedDojutsu)
                {
                    foreach (Gene gene in pawn.genes.GenesListForReading.Where(x => x.def == DojutsuData.DojutsuDef.DojutsuGeneDef))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
                if (DojutsuData.DojutsuDef.DojutsuAbilityDef != null)
                {
                    AbilityClass abilityClass = pawn.GetComp<CompAbilities>().GetAbilityClass(WN_DefOf.WN_Clan);
                    abilityClass.GetLearnedAbility(DojutsuData.DojutsuDef.DojutsuAbilityDef).End();
                }
            }

            // Removes any abilities exclusively provided by the dojutsu
            WNDE_Ability_Utilities.RemoveAbilities(pawn, dojutsuData);
        }

        public override void Tick()
        {
            base.Tick();
            bool isActive = DojutsuData.DojutsuActive;
            HandleActiveHediffs(isActive);
            HandleChakraDrain(isActive);
            HandleXPGain(isActive);
            HandleDojutsuGraphics();
        }

        public void HandleDojutsuGraphics()
        {
            if (DojutsuData.DojutsuDef.dojutsuGraphic != null)
            {
                def.eyeGraphicSouth = DojutsuData.DojutsuDef.GetDojutsuGraphic(false);
                def.eyeGraphicEast = DojutsuData.DojutsuDef.GetDojutsuGraphic(true);
            }
        }

        // Adds, updates, or removes the associated active hediff if dojutsu is (in)active
        public void HandleActiveHediffs(bool isActive)
        {
            if (isActive)
            {
                bool hasActiveHediffDef = DojutsuData.DojutsuDef.ActiveHediffDef != null;
                if (hasActiveHediffDef)
                {
                    HediffDef activeHediffDef = DojutsuData.DojutsuDef.ActiveHediffDef;
                    if (!pawn.health.hediffSet.hediffs.Where(x => x.def == activeHediffDef && x.Part == Part).Any())
                    {
                        pawn.health.AddHediff(HediffMaker.MakeHediff(activeHediffDef, pawn, Part), Part, (DamageInfo?)null, (DamageResult)null);
                    }
                    else
                    {
                        Hediff activeHediff = pawn.health.hediffSet.hediffs.Where(x => x.def == activeHediffDef && x.Part == Part).First();
                        if (activeHediffDef.stages != null && activeHediffDef.stages.Count > 1)
                        {
                            activeHediff.Severity = activeHediffDef.stages[DojutsuData.DojutsuStage].minSeverity;
                        }
                    }
                }
            }
            else
            {
                bool hasActiveHediffDef = DojutsuData.DojutsuDef.ActiveHediffDef != null;
                if (hasActiveHediffDef)
                {
                    HediffDef activeHediffDef = DojutsuData.DojutsuDef.ActiveHediffDef;
                    if (pawn.health.hediffSet.hediffs.Where(x => x.def == activeHediffDef && x.Part == Part).Any())
                    {
                        foreach (Hediff activeHediff in pawn.health.hediffSet.hediffs.Where(x => x.def == activeHediffDef && x.Part == Part))
                        {
                            pawn.health.RemoveHediff(activeHediff);
                            break;
                        }
                    }
                }
            }
        }

        // Handles chakra drain (if draining) when active
        public void HandleChakraDrain(bool isActive)
        {
            if (!isActive)
            {
                return;
            }
            bool drainsChakra = DojutsuData.DojutsuDef.DrainRate(this.CurStageIndex) != 0f;
            if (drainsChakra)
            {
                CompAbilities comp = pawn.GetComp<CompAbilities>();
                AbilityResource chakra = comp.GetAbilityResource(WN_DefOf.WN_ChakraEnergy);
                chakra.energy -= DojutsuData.DojutsuDef.DrainRate(this.CurStageIndex);
            }
        }

        // Handles passive XP gain when active
        public void HandleXPGain(bool isActive)
        {
            if (!isActive)
            {
                return;
            }
            bool hasXPGain = DojutsuData.DojutsuDef.stageXPGain != null;
            if (hasXPGain)
            {
                foreach (XPGain xpGain in DojutsuData.DojutsuDef.XPGains(this.CurStageIndex))
                {
                    AbilityClass abilityClass = pawn.GetComp<CompAbilities>().GetAbilityClass(xpGain.abilityClass);
                    if (Find.TickManager.TicksGame % xpGain.ticksInterval == 0)
                    {
                        abilityClass.GainXP(xpGain.xpGain);
                    }
                }
            }
        }

        // Not implemented, see comments on AbilityClass_OnLevelGained_Patch in WNDE.Dojutsu.Sharingan
        //public void TryProgressStage()
        //{
        //    bool hasStages = def.stages != null;
        //    if (hasStages)
        //    {
        //        if (def.stages.Count > 1 && (CurStageIndex < def.stages.Count))
        //        {
        //            Log.Error(Label + " tried progressing stage");
        //            return;
        //        }
        //    }
        //    Log.Error(Label + " lmoa loser");
        //}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<WNDE_DojutsuData>(ref dojutsuData, "WNDE_DojutsuData", Array.Empty<object>());
        }
    }
}
