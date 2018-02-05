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

        private Dictionary<string, double> m_pp_expr_data = new Dictionary<string, double>();
        private Dictionary<string, Expression<double>> m_pp_expression_dict=new Dictionary<string, Expression<double>>();

        protected StringFormatter GetFormattedPP(PPTuple tuple)
        {
            var formatter = StringFormatter.GetPPFormatter();

            m_pp_expr_data["rtpp_speed"] = tuple.RealTimeSpeedPP;
            m_pp_expr_data["rtpp_aim"] = tuple.RealTimeAimPP;
            m_pp_expr_data["rtpp_acc"] = tuple.RealTimeAccuracyPP;
            m_pp_expr_data["rtpp"] = tuple.RealTimePP;

            m_pp_expr_data["fcpp_speed"] = tuple.FullComboSpeedPP;
            m_pp_expr_data["fcpp_aim"] = tuple.FullComboAimPP;
            m_pp_expr_data["fcpp_acc"] = tuple.FullComboAccuracyPP;
            m_pp_expr_data["fcpp"] = tuple.FullComboPP;

            m_pp_expr_data["maxpp_speed"] = tuple.MaxSpeedPP;
            m_pp_expr_data["maxpp_aim"] = tuple.MaxAimPP;
            m_pp_expr_data["maxpp_acc"] = tuple.MaxAccuracyPP;
            m_pp_expr_data["maxpp"] = tuple.MaxPP;


            foreach (var arg in formatter)
            {
                Expression<double> expr;
                if (!m_pp_expression_dict.ContainsKey(arg))
                {
                    expr = new Expression<double>(arg);
                    expr.Data = m_pp_expr_data;
                    m_pp_expression_dict[arg]=expr;
                }
                else
                {
                    expr = m_pp_expression_dict[arg];
                }

                formatter.Fill(arg, expr.EvalDouble());
            }

            return formatter;
        }


        private Dictionary<string, int> m_hit_count_expr_data = new Dictionary<string, int>();
        private Dictionary<string, Expression<int>> m_hit_count_expression_dict = new Dictionary<string, Expression<int>>();

        protected StringFormatter GetFormattedHitCount(HitCountTuple tuple)
        {
            var formatter = StringFormatter.GetHitCountFormatter();

            m_hit_count_expr_data["n300g"] = tuple.CountGeki;
            m_hit_count_expr_data["n300"] = tuple.Count300;
            m_hit_count_expr_data["n200"] = tuple.CountKatu;
            m_hit_count_expr_data["n100"] = tuple.Count100;
            m_hit_count_expr_data["n150"] = tuple.Count100;
            m_hit_count_expr_data["n50"] = tuple.Count50;
            m_hit_count_expr_data["nmiss"] = tuple.CountMiss;
            m_hit_count_expr_data["ngeki"] = tuple.CountGeki;
            m_hit_count_expr_data["nkatu"] = tuple.CountKatu;

            m_hit_count_expr_data["rtmaxcombo"] = tuple.RealTimeMaxCombo;
            m_hit_count_expr_data["fullcombo"] = tuple.FullCombo;
            m_hit_count_expr_data["maxcombo"] = tuple.PlayerMaxCombo;
            m_hit_count_expr_data["combo"] = tuple.Combo;


            foreach (var arg in formatter)
            {
                Expression<int> expr;
                if (!m_hit_count_expression_dict.ContainsKey(arg))
                {
                    expr = new Expression<int>(arg);
                    expr.Data = m_hit_count_expr_data;
                    m_hit_count_expression_dict[arg]=expr;
                }
                else
                {
                    expr = m_hit_count_expression_dict[arg];
                }

                formatter.Fill(arg, expr.EvalInt());
            }

            return formatter;
        }
    }
}
