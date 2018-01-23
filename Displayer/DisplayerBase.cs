using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    public struct PPTuple
    {
        public static readonly PPTuple Empty;

        public double RealTimePP;
        public double RealTimeAimPP;
        public double RealTimeSpeedPP;
        public double RealTimeAccuracyPP;

        public double FullComboPP;
        public double FullComboAimPP;
        public double FullComboSpeedPP;
        public double FullComboAccuracyPP;

        public double MaxPP;
        public double MaxAimPP;
        public double MaxSpeedPP;
        public double MaxAccuracyPP;
    }

    public struct HitCountTuple
    {
        public int Count300;
        public int Count100;
        public int Count50;
        public int CountGeki;
        public int CountKatu;
        public int CountMiss;
        public int Combo;
        public int MaxCombo;
        public int FullCombo;
    }

    public abstract class DisplayerBase
    {
        /// <summary>
        /// Update PP
        /// </summary>
        /// <param name="cur_pp">real time PP</param>
        /// <param name="if_fc_pp">if FC pp</param>
        /// <param name="max_pp">beatmap max pp</param>
        public abstract void OnUpdatePP(PPTuple tuple);

        /// <summary>
        /// Update HitCount
        /// </summary>
        /// <param name="n300">300 count</param>
        /// <param name="n100">100 count</param>
        /// <param name="n50">50 count</param>
        /// <param name="nmiss">miss count</param>
        /// <param name="combo">current combo</param>
        /// <param name="max_combo">current max combo</param>
        public abstract void OnUpdateHitCount(HitCountTuple tuple);

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

        protected StringFormatter GetFormattedPP(PPTuple tuple)
        {
            var formatter = StringFormatter.GetPPFormatter();

            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "rtpp":
                        formatter.Fill(arg, tuple.RealTimePP); break;
                    case "rtpp_aim":
                        formatter.Fill(arg, tuple.RealTimeAimPP); break;
                    case "rtpp_speed":
                        formatter.Fill(arg, tuple.RealTimeSpeedPP); break;
                    case "rtpp_acc":
                        formatter.Fill(arg, tuple.RealTimeAccuracyPP); break;

                    case "fcpp":
                        formatter.Fill(arg, tuple.FullComboPP); break;
                    case "fcpp_aim":
                        formatter.Fill(arg, tuple.FullComboAimPP); break;
                    case "fcpp_speed":
                        formatter.Fill(arg, tuple.FullComboSpeedPP); break;
                    case "fcpp_acc":
                        formatter.Fill(arg, tuple.FullComboAccuracyPP); break;

                    case "maxpp":
                        formatter.Fill(arg, tuple.MaxPP); break;
                    case "maxpp_aim":
                        formatter.Fill(arg, tuple.MaxAimPP); break;
                    case "maxpp_speed":
                        formatter.Fill(arg, tuple.MaxSpeedPP); break;
                    case "maxpp_acc":
                        formatter.Fill(arg, tuple.MaxAccuracyPP); break;
                }
            }

            return formatter;
        }
        protected StringFormatter GetFormattedHitCount(HitCountTuple tuple)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "n300":
                        formatter.Fill(arg, tuple.Count300); break;
                    case "n100":
                        formatter.Fill(arg, tuple.Count100); break;
                    case "n50":
                        formatter.Fill(arg, tuple.Count50); break;
                    case "nmiss":
                        formatter.Fill(arg, tuple.CountMiss); break;
                    case "ngeki":
                        formatter.Fill(arg, tuple.CountGeki); break;
                    case "nkatu":
                        formatter.Fill(arg, tuple.CountKatu); break;
                    case "combo":
                        formatter.Fill(arg, tuple.Combo); break;
                    case "maxcombo":
                        formatter.Fill(arg, tuple.MaxCombo); break;
                    case "fullcombo":
                        formatter.Fill(arg, tuple.FullCombo); break;
                }
            }

            return formatter;
        }
    }
}
