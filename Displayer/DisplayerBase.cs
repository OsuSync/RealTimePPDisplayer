using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    public abstract class DisplayerBase
    {
        /// <summary>
        /// Update PP
        /// </summary>
        /// <param name="cur_pp">real time PP</param>
        /// <param name="if_fc_pp">if FC pp</param>
        /// <param name="max_pp">beatmap max pp</param>
        public abstract void OnUpdatePP(double cur_pp, double if_fc_pp, double max_pp);

        /// <summary>
        /// Update HitCount
        /// </summary>
        /// <param name="n300">300 count</param>
        /// <param name="n100">100 count</param>
        /// <param name="n50">50 count</param>
        /// <param name="nmiss">miss count</param>
        /// <param name="combo">current combo</param>
        /// <param name="max_combo">current max combo</param>
        public abstract void OnUpdateHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo);

        /// <summary>
        /// Clear Output
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Displayer(ORTDP Thread[call interval=ORTDP.IntervalTime])
        /// </summary>
        public virtual void Display() { }

        /// <summary>
        /// Displayer(call interval = 1000/Setting.FPS)
        /// </summary>
        /// <param name="time">1000/Setting.FPS</param>
        public virtual void FixedDisplay(double time) { }

        public virtual void OnDestroy() { }

        protected StringFormatter GetFormattedPP(double cur_pp, double if_fc_pp, double max_pp)
        {
            var formatter = StringFormatter.GetPPFormatter();

            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "rtpp":
                        formatter.Fill(arg, cur_pp); break;
                    case "if_fc_pp":
                        formatter.Fill(arg, if_fc_pp); break;
                    case "max_pp":
                        formatter.Fill(arg, max_pp); break;
                }
            }

            return formatter;
        }
        protected StringFormatter GetFormattedHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "n300":
                        formatter.Fill(arg, n300); break;
                    case "n100":
                        formatter.Fill(arg, n100); break;
                    case "n50":
                        formatter.Fill(arg, n50); break;
                    case "nmiss":
                        formatter.Fill(arg, nmiss); break;
                    case "combo":
                        formatter.Fill(arg, combo); break;
                    case "max_combo":
                        formatter.Fill(arg, max_combo); break;
                }
            }

            return formatter;
        }
    }
}
