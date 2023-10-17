using NarutoMod;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WNDE.Dojutsu
{
    [HotSwappable]
    public class WNDE_Thing_Dojutsu : ThingWithComps
    {
        private WNDE_DojutsuData dojutsuData;

        public WNDE_DojutsuData DojutsuData
        {
            get
            {
                bool flag = dojutsuData == null;
                if (flag)
                {
                    dojutsuData = new WNDE_DojutsuData();
                    dojutsuData.SaveOwnerData(null, WNDE_DefOf.WNDE_Dojutsu_Unknown, null, false);
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
                if (hasOriginalOwner)
                {
                    originalOwner = DojutsuData.OriginalOwner.Name.ToStringShort + "WNDE_Possessive".Translate();
                }

                return originalOwner + base.Label;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string dojutsuStatus = (DojutsuData.DojutsuActive) ? "WNDE_DojutsuStatusActive" : "WNDE_DojutsuStatusInactive";
            stringBuilder.AppendLine("WNDE_DojutsuStatus".Translate() + ": " + dojutsuStatus.Translate());

            bool hasStageName = DojutsuData.StageName != null;
            if (hasStageName)
            {
                stringBuilder.AppendLine("WNDE_DojutsuStage".Translate() + ": " + DojutsuData.StageName);
            }
            bool hasOriginalOwner = DojutsuData.HasOriginalOwner;
            if (hasOriginalOwner)
            {
                stringBuilder.AppendLine("WNDE_OriginalOwner".Translate() + ": " + DojutsuData.OriginalOwner.Name.ToStringFull);
            }
            stringBuilder.Append(base.GetInspectString());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<WNDE_DojutsuData>(ref dojutsuData, "WNDE_DojutsuData", Array.Empty<object>());
        }
    }
}
