using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using TaranMagicFramework;
using WNDE.Dojutsu;
using AbilityDef = TaranMagicFramework.AbilityDef;

namespace WNDE
{
    [StaticConstructorOnStartup]
    public class WNDE : Mod
    {
        public WNDE(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("NozoMeMu.WorldOfNaruto.DojutsuExpanded");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [DefOf]
    public static class WNDE_DefOf
    {
        public static HediffDef WNDE_Hediff_Dojutsu;
        public static HediffDef WNDE_Hediff_Byakugan;
        public static HediffDef WNDE_Hediff_Sharingan;

        public static ThingDef WNDE_Thing_DojutsuEye;
        public static ThingDef WNDE_Thing_DojutsuByakugan;
        public static ThingDef WNDE_Thing_DojutsuSharingan;

        public static GeneDef WNDE_Gene_Dojutsu;

        public static WNDE_DojutsuDef WNDE_Dojutsu_Unknown;
        public static WNDE_DojutsuDef WNDE_Dojutsu_Byakugan;
        public static WNDE_DojutsuDef WNDE_Dojutsu_Sharingan;

        public static AbilityDef WNDE_Ability_BaseSharingan;
        public static AbilityDef WNDE_Ability_Byakugan;
    }
}
