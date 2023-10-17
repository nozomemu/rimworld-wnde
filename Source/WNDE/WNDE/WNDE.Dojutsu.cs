using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using TaranMagicFramework;
using AbilityDef = TaranMagicFramework.AbilityDef;
using Verse;
using Verse.AI;

namespace WNDE.Dojutsu
{
    public class WNDE_DojutsuDef : Def
    {
        public HediffDef dojutsuHediffDef;
        public ThingDef dojutsuThingDef;
        public GeneDef dojutsuGeneDef;
        public AbilityDef dojutsuAbilityDef;

        public HediffDef activeHediffDef;
        public Dictionary<int, List<AbilityTreeDef>> stageAbilityTrees;
        public Dictionary<int, Dictionary<AbilityDef, int>> stageAbilities;

        public Dictionary<int, float> stageDrainRates;
        public Dictionary<int, List<XPGain>> stageXPGain;

        public GraphicData dojutsuGraphic;
        public GraphicData dojutsuGraphicEast;
        public bool drawnByDefault;

        public HediffDef DojutsuHediffDef
        {
            get
            {
                return dojutsuHediffDef ?? WNDE_DefOf.WNDE_Hediff_Dojutsu;
            }
        }

        public ThingDef DojutsuThingDef
        {
            get
            {
                return dojutsuThingDef ?? WNDE_DefOf.WNDE_Thing_DojutsuEye;
            }
        }

        public GeneDef DojutsuGeneDef
        {
            get
            {
                return dojutsuGeneDef ?? null;
            }
        }

        public AbilityDef DojutsuAbilityDef
        {
            get
            {
                return dojutsuAbilityDef ?? null;
            }
        }

        public HediffDef ActiveHediffDef
        {
            get
            {
                return activeHediffDef ?? null;
            }
        }

        public List<AbilityTreeDef> AbilityTrees(int stage)
        {
            return stageAbilityTrees.TryGetValue(stage);
        }

        public Dictionary<AbilityDef, int> Abilities(int stage)
        {
            return stageAbilities.TryGetValue(stage);
        }

        public float DrainRate(int stage)
        {
            return stageDrainRates.TryGetValue(stage, 0);
        }

        public List<XPGain> XPGains(int stage)
        {
            return stageXPGain.TryGetValue(stage, null);
        }

        // Hediffs have eyeGraphicSouth & eyeGraphicEast but I made it so you can specify graphics directly in the DojutsuDef
        // Ultimately just a matter of personal taste on my part, seems tidier if everything related to the dojutsu is in its own def
        public GraphicData GetDojutsuGraphic(bool isHorizontal)
        {
            if (isHorizontal && dojutsuGraphicEast != null)
            {
                return dojutsuGraphicEast;
            }
            return dojutsuGraphic;
        }

        // Render eye graphics by default even if inactive, e.g. Byakugan (not sure about this -- see Himawari's case -- but idk idc I just modeled it off the base mod)
        public bool DrawnByDefault
        {
            get
            {
                return drawnByDefault;
            }
        }
    }
}
