using HarmonyLib;
using NarutoMod;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static System.Collections.Specialized.BitVector32;

namespace WNDE.Dojutsu
{
    [HotSwappable]
    public class WNDE_DojutsuData : IExposable
    {
        private WNDE_DojutsuDef dojutsuDef;

        private Pawn originalOwner;
        private int dojutsuStage;
        private string stageName;
        private bool dojutsuActive;

        // TO-DO: Special abilities variable for dynamic dojutsu, to separate from fixed jutsu specified in the dojutsu def
        // Maybe specify a variable list or dictionary of "special jutsu" by the eye so special abilities will be more dynamic since collections allow us to add and remove items
        // This can prove useful for stuff like EMS, where instead of relying on genes or making separate hediffs for every possible combination, we can make it so (cont.)
        // (cont.) taking Person A's MS with its own list/dictionary/collection of jutsu merges/adds its items with Person B's, resulting in an EMS DojutsuData with both's (cont.)
        // abilities + any other special EMS-"exclusive" jutsu
        // Something like: private List<Dictionary<AbilityDef, int>> specialAbilities;

        // The DojutsuDef is the base or "heart" of the hediff, as this is where default values of the dojutsu are specified (see WNDE_DojutsuDef class)
        public WNDE_DojutsuDef DojutsuDef
        {
            get
            {
                return dojutsuDef ?? WNDE_DefOf.WNDE_Dojutsu_Unknown;
            }
            set
            {
                dojutsuDef = value;
            }
        }

        // Original owner of the eye
        // Might be useful for setting up drama ("rightful owner" situations), relationship penalties, incidents, etc.
        public Pawn OriginalOwner
        {
            get
            {
                return originalOwner;
            }
        }

        public bool HasOriginalOwner
        {
            get
            {
                return originalOwner != null;
            }
        }

        // An integer corresponding to the index of the "hediff stage" of the dojutsu
        // The hediff's stage should not change unless "upgrading" since it is not a disease/"cured" by anything, but I still specified this variable for syncing purposes (cont.)
        // (cont.) just to be safe
        public int DojutsuStage
        {
            get
            {
                return dojutsuStage;
            }
            set
            {
                dojutsuStage = value;
            }
        }

        public string StageName
        {
            get
            {
                return stageName;
            }
            set
            {
                stageName = value;
            }
        }

        public bool DojutsuActive
        {
            get
            {
                return dojutsuActive;
            }
            set
            {
                dojutsuActive = value;
            }
        }

        public void SaveOwnerData(Pawn pawn, WNDE_DojutsuDef dojutsuDef, string stageName, bool dojutsuActive, int dojutsuStage = 0)
        {
            this.dojutsuDef = dojutsuDef;
            this.originalOwner = pawn;
            this.dojutsuStage = dojutsuStage;
            this.stageName = stageName;
            this.dojutsuActive= dojutsuActive;
        }

        public void CopyDojutsuData(WNDE_DojutsuData other)
        {
            this.dojutsuDef = other.dojutsuDef;
            this.originalOwner = other.originalOwner;
            this.dojutsuStage = other.DojutsuStage;
            this.stageName = other.stageName;
            this.dojutsuActive = other.dojutsuActive;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<WNDE_DojutsuDef>(ref dojutsuDef, "WNDE_DojutsuData_DojutsuDef");
            Scribe_References.Look<Pawn>(ref originalOwner, "WNDE_DojutsuData_OriginalOwnerPawn", true);
            Scribe_Values.Look<int>(ref dojutsuStage, "WNDE_DojutsuData_DojutsuStage", 0);
            Scribe_Values.Look<string>(ref stageName, "WNDE_DojutsuData_StageName");
            Scribe_Values.Look<bool>(ref dojutsuActive, "WNDE_DojutsuData_DojutsuActive", false);
        }
    }
}
