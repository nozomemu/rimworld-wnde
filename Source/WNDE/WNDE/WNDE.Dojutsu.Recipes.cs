using NarutoMod;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using WNDE.Dojutsu;

namespace WNDE.Dojutsu
{
    public class WNDE_Recipe_RemoveDojutsu : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
            int num;
            for (int i = 0; i < allHediffs.Count; i = num + 1)
            {
                if (allHediffs[i].Part != null && allHediffs[i] is WNDE_Hediff_Dojutsu)
                {
                    yield return allHediffs[i].Part;
                }
                num = i;
            }
            yield break;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            MedicalRecipesUtility.IsClean(pawn, part);
            bool flag = this.IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[]
                {
                    billDoer,
                    pawn
                });
                if (!pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null).Contains(part))
                {
                    return;
                }
                WNDE_Hediff_Dojutsu hediffDojutsu = (from x in pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>()
                 where x.Part == part
                 select x).FirstOrDefault();
                if (hediffDojutsu != null)
                {
                    ThingDef dojutsuThing;
                    if (hediffDojutsu.def.spawnThingOnRemoved != null)
                    {
                        dojutsuThing = hediffDojutsu.def.spawnThingOnRemoved;
                    }
                    else
                    {
                        dojutsuThing = hediffDojutsu.DojutsuData.DojutsuDef.DojutsuThingDef;
                    }
                    WNDE_Thing_Dojutsu dojutsuEye = GenSpawn.Spawn(dojutsuThing, billDoer.Position, billDoer.Map, WipeMode.Vanish) as WNDE_Thing_Dojutsu;
                    bool hasOriginalOwner = hediffDojutsu.DojutsuData.HasOriginalOwner;
                    Pawn originalOwner = hasOriginalOwner ? hediffDojutsu.DojutsuData.OriginalOwner : hediffDojutsu.pawn;
                    bool hasStages = hediffDojutsu.CurStage != null;
                    string stageName = hasStages ? hediffDojutsu.CurStage.label : null;
                    int dojutsuStage = hasStages ? hediffDojutsu.CurStageIndex : 0;
                    dojutsuEye.DojutsuData.SaveOwnerData(originalOwner, hediffDojutsu.DojutsuData.DojutsuDef, stageName, hediffDojutsu.DojutsuData.DojutsuActive, dojutsuStage);

                    pawn.health.RemoveHediff(hediffDojutsu);
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, 99999f, 999f, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
                }
            }
            if (flag)
            {
                base.ReportViolation(pawn, billDoer, pawn.HomeFaction, -70, null);
            }
        }
    }

    public class WNDE_Recipe_ImplantDojutsu : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                // TO-DO/SUGGESTION: Any part with Hashirama cells ala Danzo's arm
                // For now, just target the eyes
                bool isEye = part.def == BodyPartDefOf.Eye;
                bool noAddedPart = !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part);
                bool noDojutsu = !(from x in pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>()
                                    where x.Part == part
                                    select x).Any();
                if (isEye && noAddedPart && noDojutsu)
                {
                    yield return part;
                }
            }
            yield break;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            bool flag = MedicalRecipesUtility.IsClean(pawn, part);
            bool flag2 = !PawnGenerator.IsBeingGenerated(pawn) && this.IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[]
                {
                    billDoer,
                    pawn
                });
                MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, billDoer.Position, billDoer.Map);
                if (flag && flag2 && part.def.spawnThingOnRemoved != null)
                {
                    ThoughtUtility.GiveThoughtsForPawnOrganHarvested(pawn, billDoer);
                }
                if (flag2)
                {
                    base.ReportViolation(pawn, billDoer, pawn.HomeFaction, -70, null);
                }
                if (ModsConfig.IdeologyActive)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InstalledProsthetic, billDoer.Named(HistoryEventArgsNames.Doer)), true);
                }
            }
            else if (pawn.Map != null)
            {
                MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, pawn.Position, pawn.Map);
            }
            else
            {
                pawn.health.RestorePart(part, null, true);
            }
            ImplantDojutsu(this.recipe, pawn, part, ingredients.OfType<WNDE_Thing_Dojutsu>().FirstOrDefault<WNDE_Thing_Dojutsu>());
        }

        public static void ImplantDojutsu(RecipeDef recipe, Pawn pawn, BodyPartRecord part, WNDE_Thing_Dojutsu dojutsuEye)
        {
            WNDE_Hediff_Dojutsu hediffDojutsu = HediffMaker.MakeHediff(recipe.addsHediff, pawn, null) as WNDE_Hediff_Dojutsu;
            bool hasOriginalOwner = dojutsuEye.DojutsuData.HasOriginalOwner;
            if (hasOriginalOwner)
            {
                hediffDojutsu.DojutsuData.CopyDojutsuData(dojutsuEye.DojutsuData);

                pawn.health.AddHediff(hediffDojutsu, part, null, null);
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
            }
            else
            {
                pawn.health.AddHediff(hediffDojutsu, part, null, null);
            }
        }

        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            return ((pawn.Faction != billDoerFaction && pawn.Faction != null) || pawn.IsQuestLodger()) && (this.recipe.addsHediff.addedPartProps == null || !this.recipe.addsHediff.addedPartProps.betterThanNatural) && HealthUtility.PartRemovalIntent(pawn, part) == BodyPartRemovalIntent.Harvest;
        }
    }
}
