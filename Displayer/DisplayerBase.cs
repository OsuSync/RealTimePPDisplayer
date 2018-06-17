using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public int PlayerMaxCombo;
        public int FullCombo;
        public int RealTimeMaxCombo;
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

        private static ThreadLocal<Dictionary<string, double>> s_pp_expr_data = new ThreadLocal<Dictionary<string, double>>(()=> new Dictionary<string, double>());
        private static ThreadLocal<Dictionary<string, Expression<double>>> s_pp_expression_dict = new ThreadLocal<Dictionary<string, Expression<double>>>(() => new Dictionary<string, Expression<double>>());

        public static StringFormatter GetFormattedPP(PPTuple tuple)
        {
            var formatter = StringFormatter.GetPPFormatter();

            var pp_expr_data = s_pp_expr_data.Value;
            var pp_expression_dict = s_pp_expression_dict.Value;

            pp_expr_data["rtpp_speed"] = tuple.RealTimeSpeedPP;
            pp_expr_data["rtpp_aim"] = tuple.RealTimeAimPP;
            pp_expr_data["rtpp_acc"] = tuple.RealTimeAccuracyPP;
            pp_expr_data["rtpp"] = tuple.RealTimePP;

            pp_expr_data["fcpp_speed"] = tuple.FullComboSpeedPP;
            pp_expr_data["fcpp_aim"] = tuple.FullComboAimPP;
            pp_expr_data["fcpp_acc"] = tuple.FullComboAccuracyPP;
            pp_expr_data["fcpp"] = tuple.FullComboPP;

            pp_expr_data["maxpp_speed"] = tuple.MaxSpeedPP;
            pp_expr_data["maxpp_aim"] = tuple.MaxAimPP;
            pp_expr_data["maxpp_acc"] = tuple.MaxAccuracyPP;
            pp_expr_data["maxpp"] = tuple.MaxPP;


            foreach (var arg in formatter)
            {
                Expression<double> expr;
                if (!pp_expression_dict.ContainsKey(arg))
                {
                    expr = new Expression<double>(arg);
                    expr.Data = pp_expr_data;
                    pp_expression_dict[arg]=expr;
                }
                else
                {
                    expr = pp_expression_dict[arg];
                }

                formatter.Fill(arg, expr.EvalDouble());
            }

            return formatter;
        }


        private static ThreadLocal<Dictionary<string, int>> s_hit_count_expr_data = new ThreadLocal<Dictionary<string, int>>(()=>new Dictionary<string, int>());
        private static ThreadLocal<Dictionary<string, Expression<int>>> s_hit_count_expression_dict = new ThreadLocal<Dictionary<string, Expression<int>>>(() => new Dictionary<string, Expression<int>>());

        public static StringFormatter GetFormattedHitCount(HitCountTuple tuple)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            var hit_count_expr_data = s_hit_count_expr_data.Value;
            var hit_count_expression_dict = s_hit_count_expression_dict.Value;

            hit_count_expr_data["n300g"] = tuple.CountGeki;
            hit_count_expr_data["n300"] = tuple.Count300;
            hit_count_expr_data["n200"] = tuple.CountKatu;
            hit_count_expr_data["n100"] = tuple.Count100;
            hit_count_expr_data["n150"] = tuple.Count100;
            hit_count_expr_data["n50"] = tuple.Count50;
            hit_count_expr_data["nmiss"] = tuple.CountMiss;
            hit_count_expr_data["ngeki"] = tuple.CountGeki;
            hit_count_expr_data["nkatu"] = tuple.CountKatu;

            hit_count_expr_data["rtmaxcombo"] = tuple.RealTimeMaxCombo;
            hit_count_expr_data["fullcombo"] = tuple.FullCombo;
            hit_count_expr_data["maxcombo"] = tuple.PlayerMaxCombo;
            hit_count_expr_data["combo"] = tuple.Combo;


            foreach (var arg in formatter)
            {
                Expression<int> expr;
                if (!hit_count_expression_dict.ContainsKey(arg))
                {
                    expr = new Expression<int>(arg);
                    expr.Data = hit_count_expr_data;
                    hit_count_expression_dict[arg]=expr;
                }
                else
                {
                    expr = hit_count_expression_dict[arg];
                }

                formatter.Fill(arg, expr.EvalInt());
            }

            return formatter;
        }
    }
}
