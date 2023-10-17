using NarutoMod;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaranMagicFramework;
using Verse;
using static Verse.DamageWorker;

namespace WNDE.Dojutsu.Byakugan
{
    // Adds a custom gene class for Byakugan like the Sharingan so we can handle handing out hediffs and abilities as appropriate
    public class WNDE_Gene_Byakugan : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            // Only turn available non-dojutsu eyes into Byakugan if the gene is "innate" or an endogene, since xenogene dojutsu are supposed to represent transplants or non-Hyuuga
            if (pawn.genes.HasEndogene(WN_DefOf.WN_ByakuganGene))
            {
                Awaken();
            }
        }

        public void Awaken()
        {
            AbilityClass abilityClass = pawn.GetComp<CompAbilities>().GetAbilityClass(WN_DefOf.WN_Clan);
            bool notAwakened = !pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Byakugan);
            if (notAwakened)
            {
                foreach (BodyPartRecord eyePart in pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Eye))
                {
                    bool notMissing = !pawn.health.hediffSet.PartIsMissing(eyePart);
                    bool noAddedPart = !pawn.health.hediffSet.hediffs.OfType<Hediff_AddedPart>().Where(x => x.Part == eyePart).Any();
                    bool noDojutsu = !pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().Where(x => x.Part == eyePart).Any();
                    if (notMissing && noAddedPart && noDojutsu)
                    {
                        WNDE_Hediff_Dojutsu byakuganEye = HediffMaker.MakeHediff(WNDE_DefOf.WNDE_Hediff_Byakugan, pawn, eyePart) as WNDE_Hediff_Dojutsu;
                        byakuganEye.DojutsuData.DojutsuDef = WNDE_DefOf.WNDE_Dojutsu_Byakugan;
                        pawn.health.AddHediff(byakuganEye, eyePart, (DamageInfo?)null, (DamageResult)null);
                    }
                }
                bool recheckForByakugan = pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Byakugan);
                if (!recheckForByakugan)
                {
                    return;
                }
                if (!abilityClass.Learned(WNDE_DefOf.WNDE_Ability_Byakugan))
                {
                    abilityClass.LearnAbility(WNDE_DefOf.WNDE_Ability_Byakugan, false, 0);
                }
            }
        }
    }

    public class WNDE_Ability_Byakugan : Ability_Toggleable
    {
        public TattooDef prevFaceTattoo;

        public override float? ResourceRegenRate
        {
            get
            {
                float? num = base.ResourceRegenRate;
                if (!this.pawn.genes.HasEndogene(WN_DefOf.WN_ByakuganGene))
                {
                    num *= (float)2;
                }
                return num;
            }
        }

        public override void Start(bool consumeEnergy = true)
        {
            base.Start(consumeEnergy);
            this.prevFaceTattoo = this.pawn.style.FaceTattoo;
            this.pawn.style.FaceTattoo = WN_DefOf.WN_ByakuganTattoo;
            this.pawn.style.Notify_StyleItemChanged();
            if (!this.pawn.genes.HasEndogene(WN_DefOf.WN_ByakuganGene))
            {
                // We know Byakugan can be activated even by non-Hyūga, e.g. Ao, so there shouldn't be any restrictions on using/during use
                // Maybe other theoretical factors like non-Hyūga using more chakra can be put here, otherwise this is blank
            }
            IEnumerable<WNDE_Hediff_Dojutsu> byakuganHediffs = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                                    Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Byakugan);
            foreach (WNDE_Hediff_Dojutsu hediffByakugan in byakuganHediffs)
            {
                hediffByakugan.DojutsuData.DojutsuActive = true;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 60 == 0 && this.Active && this.pawn.Spawned)
            {
                foreach (IntVec3 c in (from x in this.pawn.Map.AllCells
                                       where x.DistanceTo(this.pawn.Position) <= 30f && x.Fogged(this.pawn.Map)
                                       select x).ToList<IntVec3>())
                {
                    this.pawn.Map.fogGrid.Unfog(c);
                }
            }
        }

        public override void End()
        {
            base.End();
            IEnumerable<WNDE_Hediff_Dojutsu> byakuganHediffs = this.pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                                                    Where(x => x.def == WNDE_DefOf.WNDE_Hediff_Byakugan);
            foreach (WNDE_Hediff_Dojutsu hediffByakugan in byakuganHediffs)
            {
                hediffByakugan.DojutsuData.DojutsuActive = false;
            }
            if (this.prevFaceTattoo != null)
            {
                this.pawn.style.FaceTattoo = this.prevFaceTattoo;
            }
            else
            {
                this.pawn.style.FaceTattoo = null;
            }
            this.pawn.style.Notify_StyleItemChanged();
            TaranMagicFramework.Ability ability = this.pawn.GetAbility(WN_DefOf.WN_GentleFist);
            if (ability != null && ability.Active)
            {
                ability.End();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<TattooDef>(ref this.prevFaceTattoo, "prevFaceTattoo");
        }
    }

    // Quick fix for Gentle Fist to require the hediff active instead of the ability active
    public class WNDE_Ability_GentleFist : Ability_GentleFist
    {
        public override Func<string> CanBeActivatedValidator()
        {
            return delegate
            {
                if (!pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Byakugan))
                {
                    return "WNDE_ByakuganMustBeImplanted".Translate();
                }
                bool anyActiveByakugan = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                            Where((WNDE_Hediff_Dojutsu x) => x.def == WNDE_DefOf.WNDE_Hediff_Byakugan && x.DojutsuData.DojutsuActive).Any();
                return (!anyActiveByakugan) ? ((string)"WNDE_ByakuganMustBeActive".Translate()) : "";
            };
        }
    }

    // Quick fix for Sixty-Four Palms to require the hediff active instead of the ability active
    public class WNDE_Ability_SixtyFourPalms : Ability_SixtyFourPalms
    {
        public override Func<string> CanBeActivatedValidator()
        {
            return delegate
            {
                if (!pawn.health.hediffSet.HasHediff(WNDE_DefOf.WNDE_Hediff_Byakugan))
                {
                    return "WNDE_ByakuganMustBeImplanted".Translate();
                }
                bool anyActiveByakugan = pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>().
                                            Where((WNDE_Hediff_Dojutsu x) => x.def == WNDE_DefOf.WNDE_Hediff_Byakugan && x.DojutsuData.DojutsuActive).Any();
                return (!anyActiveByakugan) ? ((string)"WNDE_ByakuganMustBeActive".Translate()) : "";
            };
        }
    }
}
