using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Expression;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace RealTimePPDisplayer.Displayer
{
    public struct BeatmapTuple
    {
        public int ObjectsCount;
        public double Duration;
    }

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
        public int CurrentMaxCombo;
    }

    public class DisplayerBase
    {
        static DisplayerBase()
        {
            Setting.OnSettingChanged += () =>
            {
                s_ppAstDict.Clear();
                s_hitCountAstDict.Clear();
            };
        }

        /// <summary>
        /// Clear Output
        /// </summary>
        public virtual void Clear()
        {
            lock (_mtx)
            {
                foreach (var ctx in s_exprCtx.Values)
                foreach (var k in ctx.Variables.Keys)
                    ctx.Variables[k] = 0;
            }

            HitCount = new HitCountTuple();
            Pp = new PPTuple();
        }

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

        private object _mtx = new object();
        public HitCountTuple HitCount { get; set; } = new HitCountTuple();
        public PPTuple Pp { get; set; } = new PPTuple();
        public BeatmapTuple BeatmapTuple { get; set; } = new BeatmapTuple();
        public double Playtime { get; set; }
        public OsuStatus Status { get; set; }
        public OsuPlayMode Mode { get; set; }
        public ModsInfo Mods { get; set; }
        

        private readonly ThreadLocal<ExpressionContext> s_exprCtx = new ThreadLocal<ExpressionContext>(()=>new ExpressionContext(),true);

        private static readonly ConcurrentDictionary<FormatArgs, IAstNode> s_ppAstDict = new ConcurrentDictionary<FormatArgs, IAstNode>();

        private void UpdateContextVariablesFromPpTuple(ExpressionContext ctx, PPTuple tuple)
        {
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
        }

        private void UpdateContextVariablesBeatmapTuple(ExpressionContext ctx, BeatmapTuple tuple)
        {
            ctx.Variables["duration"] = tuple.Duration;
            ctx.Variables["objects_count"] = tuple.ObjectsCount;
        }

        private void UpdateContextVariablesFromHitCountTuple(ExpressionContext ctx, HitCountTuple tuple)
        {
            ctx.Variables["n300g"] = tuple.CountGeki;
            ctx.Variables["n300"] = tuple.Count300;
            ctx.Variables["n200"] = tuple.CountKatu;
            ctx.Variables["n100"] = tuple.Count100;
            ctx.Variables["n150"] = tuple.Count100;
            ctx.Variables["n50"] = tuple.Count50;
            ctx.Variables["nmiss"] = tuple.CountMiss;
            ctx.Variables["ngeki"] = tuple.CountGeki;
            ctx.Variables["nkatu"] = tuple.CountKatu;

            ctx.Variables["current_maxcombo"] = tuple.CurrentMaxCombo;
            ctx.Variables["fullcombo"] = tuple.FullCombo;
            ctx.Variables["maxcombo"] = tuple.PlayerMaxCombo;
            ctx.Variables["player_maxcombo"] = tuple.PlayerMaxCombo;
            ctx.Variables["combo"] = tuple.Combo;

        }

        public StringFormatter Format(
            StringFormatter fmt,
            ConcurrentDictionary<FormatArgs, IAstNode> astDict)
        {
            return Format(fmt, astDict, Pp, HitCount,BeatmapTuple);
        }

        public StringFormatter Format(
            StringFormatter formatter,
            ConcurrentDictionary<FormatArgs, IAstNode> astDict,
            PPTuple pp,
            HitCountTuple hitcount,
            BeatmapTuple beatmap)
        {
            formatter.Clear();
            var ctx = s_exprCtx.Value;

            lock (_mtx)
            {
                UpdateContextVariablesFromPpTuple(ctx, pp);
                UpdateContextVariablesFromHitCountTuple(ctx, hitcount);
                UpdateContextVariablesBeatmapTuple(ctx, beatmap);
                ctx.Variables["playtime"] = Playtime;
            }

            foreach (var arg in formatter)
            {
                if (!astDict.TryGetValue(arg, out var root))
                {
                    var parser = new ExpressionParser();
                    try
                    {
                        root = parser.Parse(arg.ExprString);
                    }
                    catch (ExprssionTokenException e)
                    {
                        Sync.Tools.IO.CurrentIO.WriteColor($"[RTPP:Expression]{e.Message}", ConsoleColor.Yellow);
                    }

                    astDict[arg] = root;
                }

                try
                {
                    formatter.Fill(arg, ctx.ExecAst(root));
                }
                catch (ExpressionException e)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor($"[RTPP:Expression]{e.Message}", ConsoleColor.Yellow);
                }
            }

            return formatter;
        }

        public StringFormatter FormatPp(PPTuple? pp=null)
        {
            var formatter = StringFormatter.GetPPFormatter();
            Pp = pp??Pp;

            return Format(formatter,s_ppAstDict);
        }

        private static readonly ConcurrentDictionary<FormatArgs, IAstNode> s_hitCountAstDict = new ConcurrentDictionary<FormatArgs, IAstNode>();

        public StringFormatter FormatHitCount(HitCountTuple? hitCount=null)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            var hitCountAstDict = s_hitCountAstDict;
            if (!Setting.DisplayHitObject) return formatter;

            HitCount = hitCount??HitCount;

            return Format(formatter,hitCountAstDict);
        }
    }
}
