using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaranMagicFramework;
using UnityEngine;
using Verse;
using Ability = TaranMagicFramework.Ability;
using AbilityDef = TaranMagicFramework.AbilityDef;

namespace WNDE.Dojutsu
{
    public static class WNDE_Ability_Utilities
    {
        // This is supposed to check if the ability being removed is still given by the remaining eye and so it exempts it from being removed
        // Idk how to handle removing ability trees, since we have cases like Susanoo which requires the MS to unlock but can be used without the MS, and removing these trees (cont.)
        // (cont.) indiscriminately may lead to these edge cases being "unlearned" too (unless they have their own ability trees or code implemented to make them persist)
        // So for now I won't add code to remove ability trees (most clan/dojutsu-exclusive jutsu for now rely on the dojutsu being active, anyway)
        public static void RemoveAbilities(Pawn pawn, WNDE_DojutsuData dojutsuData)
        {
            CompAbilities comp = pawn.GetComp<CompAbilities>();
            List<AbilityDef> keptAbilities = new List<AbilityDef>();
            if (dojutsuData.DojutsuDef.stageAbilities == null)
            {
                return;
            }
            if (dojutsuData.DojutsuDef.Abilities(dojutsuData.DojutsuStage) == null)
            {
                return;
            }
            foreach (WNDE_Hediff_Dojutsu dojutsu in pawn.health.hediffSet.hediffs.OfType<WNDE_Hediff_Dojutsu>())
            {
                if (pawn.health.hediffSet.hediffs.Where(x => x.def == dojutsu.def).Count() != 0)
                {
                    foreach (AbilityDef ability in dojutsuData.DojutsuDef.Abilities(dojutsuData.DojutsuStage).Keys)
                    {
                        keptAbilities.Add(ability);
                    }
                }
            }
            foreach (KeyValuePair<AbilityDef, int> ability in dojutsuData.DojutsuDef.Abilities(dojutsuData.DojutsuStage))
            {
                if (keptAbilities.Contains(ability.Key))
                {
                    continue;
                }
                foreach (AbilityClass allUnlockedAbilityClass in comp.AllUnlockedAbilityClasses)
                {
                    foreach (AbilityTreeDef abilityTree in allUnlockedAbilityClass.def.abilityTrees)
                    {
                        if (abilityTree.AllAbilities.Contains(ability.Key))
                        {
                            Ability learnedAbility = allUnlockedAbilityClass.GetLearnedAbility(ability.Key);
                            if (learnedAbility != null)
                            {
                                allUnlockedAbilityClass.RemoveAbility(learnedAbility);
                            }
                        }
                    }
                }
            }
        }

        // Basically the UnlockAbilities method in the framework's AbilityExtension class but integrates the new dojutsu system
        public static void UnlockAbilities(Pawn pawn, WNDE_DojutsuData dojutsuData)
        {
            CompAbilities comp = pawn.GetComp<CompAbilities>();
            if (dojutsuData.DojutsuDef.stageAbilityTrees != null)
            {
                List<AbilityTreeDef> abilityTrees = dojutsuData.DojutsuDef.AbilityTrees(dojutsuData.DojutsuStage);
                foreach (AbilityClass allUnlockedAbilityClass in comp.AllUnlockedAbilityClasses)
                {
                    foreach (AbilityTreeDef abilityTree in abilityTrees)
                    {
                        if (allUnlockedAbilityClass.def.abilityTrees.Contains(abilityTree) && !allUnlockedAbilityClass.TreeUnlocked(abilityTree))
                        {
                            allUnlockedAbilityClass.UnlockTree(abilityTree);
                        }
                    }
                }
            }
            if (dojutsuData.DojutsuDef.stageAbilities != null)
            {
                Dictionary<AbilityDef, int> abilities = dojutsuData.DojutsuDef.Abilities(dojutsuData.DojutsuStage);
                foreach (KeyValuePair<AbilityDef, int> ability in abilities)
                {
                    TryLearnAbility(comp, ability.Key, ability.Value);
                }
            }
            comp.TryAutoGainAbilities();
        }

        private static void TryLearnAbility(CompAbilities comp, AbilityDef ability, int level)
        {
            foreach (AbilityClass allUnlockedAbilityClass in comp.AllUnlockedAbilityClasses)
            {
                foreach (AbilityTreeDef abilityTree in allUnlockedAbilityClass.def.abilityTrees)
                {
                    if (abilityTree.AllAbilities.Contains(ability))
                    {
                        allUnlockedAbilityClass.LearnAbility(ability, spendSkillPoints: false, level);
                        return;
                    }
                }
            }
        }
    }
}
