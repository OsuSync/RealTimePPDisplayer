using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RealTimePPDisplayer.Expression;

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

        private static ExpressionContext _exprCtx =new ExpressionContext();
        private static ThreadLocal<Dictionary<FormatArg, IAstNode>> s_pp_ast_dict = new ThreadLocal<Dictionary<FormatArg, IAstNode>>(() => new Dictionary<FormatArg, IAstNode>());

        public static StringFormatter GetFormattedPP(PPTuple tuple)
        {
            var formatter = StringFormatter.GetPPFormatter();

            var ctx = _exprCtx;
            var pp_expression_dict = s_pp_ast_dict.Value;


            ctx.Variables["rtpp_speed"] = tuple.RealTimeSpeedPP;
            ctx.Variables["rtpp_aim"] = tuple.RealTimeAimPP;
            ctx.Variables["rtpp_acc"] = tuple.RealTimeAccuracyPP;
            ctx.Variables["rtpp"] = tuple.RealTimePP;

            ctx.Variables["fcpp_speed"] = tuple.FullComboSpeedPP;
            ctx.Variables["fcpp_aim"] = tuple.FullComboAimPP;
            ctx.Variables["fcpp_acc"] = tuple.FullComboAccuracyPP;
            ctx.Variables["fcpp"] = tuple.FullComboPP;

            ctx.Variables["maxpp_speed"] = tuple.MaxSpeedPP;
            ctx.Variables["maxpp_aim"] = tuple.MaxAimPP;
            ctx.Variables["maxpp_acc"] = tuple.MaxAccuracyPP;
            ctx.Variables["maxpp"] = tuple.MaxPP;


            foreach (var arg in formatter)
            {
                IAstNode root;
                if (!pp_expression_dict.TryGetValue(arg,out root))
                {
                    var parser = new ExpressionParser();
                    try
                    {
                        root = parser.Parse(arg.ExprString);
                    }
                    catch (ExprssionTokenException e)
                    {
                        Sync.Tools.IO.CurrentIO.WriteColor(e.Message,ConsoleColor.Yellow);
                    }

                    pp_expression_dict[arg]=root;
                }

                try
                {
                    formatter.Fill(arg, ctx.ExecAst(root));
                }
                catch (ExpressionException e)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor(e.Message, ConsoleColor.Yellow);
                }
            }

            return formatter;
        }

        private static ThreadLocal<Dictionary<FormatArg, IAstNode>> s_hit_count_expression_dict = new ThreadLocal<Dictionary<FormatArg, IAstNode>>(() => new Dictionary<FormatArg,IAstNode>());

        public static StringFormatter GetFormattedHitCount(HitCountTuple tuple)
        {
            var formatter = StringFormatter.GetHitCountFormatter();

            var ctx = _exprCtx;
            var hit_count_expression_dict = s_hit_count_expression_dict.Value;

            ctx.Variables["n300g"] = tuple.CountGeki;
            ctx.Variables["n300"] = tuple.Count300;
            ctx.Variables["n200"] = tuple.CountKatu;
            ctx.Variables["n100"] = tuple.Count100;
            ctx.Variables["n150"] = tuple.Count100;
            ctx.Variables["n50"] = tuple.Count50;
            ctx.Variables["nmiss"] = tuple.CountMiss;
            ctx.Variables["ngeki"] = tuple.CountGeki;
            ctx.Variables["nkatu"] = tuple.CountKatu;

            ctx.Variables["rtmaxcombo"] = tuple.RealTimeMaxCombo;
            ctx.Variables["fullcombo"] = tuple.FullCombo;
            ctx.Variables["maxcombo"] = tuple.PlayerMaxCombo;
            ctx.Variables["combo"] = tuple.Combo;


            foreach (var arg in formatter)
            {
                IAstNode root;
                if (!hit_count_expression_dict.TryGetValue(arg,out root))
                {
                    var parser = new ExpressionParser();
                    try
                    {
                        root = parser.Parse(arg.ExprString);
                    }
                    catch (ExprssionTokenException e)
                    {
                        Sync.Tools.IO.CurrentIO.WriteColor(e.Message,ConsoleColor.Yellow);
                    }

                    hit_count_expression_dict[arg]=root;
                }

                try
                {
                    formatter.Fill(arg, ctx.ExecAst(root));
                }
                catch (ExpressionException e)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor(e.Message,ConsoleColor.Yellow);
                }
            }

            return formatter;
        }
    }
}
