using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Formatter;
using Sync;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using static OsuRTDataProvider.Mods.ModsInfo;

namespace RealTimePPDisplayer.Formatter
{
    class RtppFormatWithBp : RtppFormatter,IFormatterClearable
    {
        private static List<BeatPerformance> bps = null;
        private static int bps_locked = 0;
        public RtppFormatWithBp(string format) : base(format)
        {

        }

        private void InitPublicOsuBotTransferPlugin()
        {
        }

        private static int FindBpIndex(List<BeatPerformance> bps,double pp)
        {
            for (int i = bps.Count - 1; i >= 0; i--)
            {
                if (bps[i].PP > pp)
                {
                    return i+1;
                }
            }
            return 0;
        }

        private void GetBpFromOsu()
        {
            if (bps_locked == 0)
            {
                if (bps == null)
                {
                    var mode = Displayer.Mode;

                    Interlocked.Increment(ref bps_locked);
                    bps = OsuApi.GetBp(Displayer.Playername, mode);
                    Interlocked.Decrement(ref bps_locked);
                }
            }
        }

        public void UpdateBpList()
        {
            GetBpFromOsu();

            if (bps == null)
            {
                return;
            }

            int rtbp = FindBpIndex(bps, Displayer.Pp.RealTimePP);
            int fcbp = FindBpIndex(bps, Displayer.Pp.FullComboPP);
            double rtpp_weight = GetWeight(rtbp);
            double fcpp_weight = GetWeight(fcbp);

            if (rtbp != -1)
            {
                Context.Variables["rtbp"] = rtbp + 1;
                Context.Variables["fcbp"] = fcbp + 1;
                Context.Variables["rtpp_with_weight"] = Displayer.Pp.RealTimePP * rtpp_weight;
                Context.Variables["fcpp_with_weight"] = Displayer.Pp.FullComboPP * fcpp_weight;
            }
            else
            {
                Context.Variables["rtbp"] = 0;
                Context.Variables["fcbp"] = 0;
                Context.Variables["rtpp_with_weight"] = 0;
                Context.Variables["fcpp_with_weight"] = 0;
            }
            Context.Variables["rtpp_weight"] = rtpp_weight;
            Context.Variables["fcpp_weight"] = rtpp_weight;
        }

        public override string GetFormattedString()
        {
            if (Displayer.Mods.HasMod(Mods.Autoplay) || Displayer.Mods.HasMod(Mods.Cinema))
            {
                return base.GetFormattedString();
            }

            if (Constants.NO_FETCH_BP_USERNAMES.Any(u => u == Displayer.Playername))
            {
                return base.GetFormattedString();
            }

            UpdateBpList();

            return base.GetFormattedString();
        }

        public new void Clear()
        {
            bps = null;

            base.Clear();
        }

        private static double GetWeight(int index)
        {
            return Math.Pow(0.95, index);
        }
    }
}
