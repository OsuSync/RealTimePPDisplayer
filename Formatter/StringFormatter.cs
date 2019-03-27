using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Expression;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace RealTimePPDisplayer
{
    public class FormatArgs
    {
        public string RawString { get; set; }
        public int Digits { get; set; }
        public IAstNode AstRoot { get; set; }
    }

    public abstract class StringFormatterBase
    {
        public HitCountTuple HitCount { get; set; } = new HitCountTuple();
        public PPTuple Pp { get; set; } = new PPTuple();
        public BeatmapTuple BeatmapTuple { get; set; } = new BeatmapTuple();
        public double Playtime { get; set; }
        public OsuStatus Status { get; set; }
        public OsuPlayMode Mode { get; set; }
        public ModsInfo Mods { get; set; }

        public abstract string Format { get; set; }
        public abstract string GetFormattedString();
    }

    public class StringFormatter: StringFormatterBase
    {
        private string _format;
        public override string Format {
            get =>_format;
            set
            {
                _format = value;
                ReplaceFormat(_format);
            }
        }

        private readonly StringBuilder _builder=new StringBuilder(1024);

        private readonly object _mtx = new object();
        private readonly List<FormatArgs> _args = new List<FormatArgs>(32);
        private static readonly Regex s_pattern = new Regex(@"\$\{(((?:\w|\s|_|\.|,|\(|\)|\^|\+|\-|\*|\/|\%|\<|\>|\=|\!|\||\&)*)(?:@(\d+))?)\}");
        private static readonly Regex s_newLinePattern = new Regex(@"(?<=[^\\])&#10;");
        private static readonly ThreadLocal<ExpressionContext> s_exprCtx = new ThreadLocal<ExpressionContext>(() => new ExpressionContext(), true);

        public StringFormatter(string format)
        {
            ReplaceFormat(format);
        }

        protected void ReplaceFormat(string format)
        {
            lock (_mtx)
            {
                _args.Clear();
                _format = s_newLinePattern.Replace(format, Environment.NewLine);
                var result = s_pattern.Matches(format);

                var exprParser = new ExpressionParser();

                foreach (Match match in result)
                {
                    IAstNode astRoot = null;

                    try
                    {
                        astRoot = exprParser.Parse(match.Groups[2].Value.Trim());//Exprssion String
                    }
                    catch(Exception e)
                    {
                        Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{e.Message}", ConsoleColor.Yellow);
                    }

                    FormatArgs args = new FormatArgs
                    {
                        RawString = match.Groups[1].Value.Trim(),
                        AstRoot = astRoot,
                        Digits = int.MinValue
                    };

                    if(int.TryParse(match.Groups[3].Value.Trim(),out int digits))
                    {
                        args.Digits = digits;
                    }

                    _args.Add(args);
                }
            }
        }

        private void ResetAllVariables()
        {
            var ctx = s_exprCtx.Value;
            foreach (var kv in ctx.Variables)
            {
                ctx.Variables[kv.Key] = 0.0;
            }
        }

        private void ProcessFormat()
        {
            ResetAllVariables();
            var ctx = s_exprCtx.Value;

            UpdateContextVariablesFromPpTuple(ctx, Pp);
            UpdateContextVariablesFromHitCountTuple(ctx, HitCount);
            UpdateContextVariablesBeatmapTuple(ctx, BeatmapTuple);
            ctx.Variables["playtime"] = Playtime;

            _builder.Clear();
            _builder.Append(_format);
            try
            {
                foreach (var arg in _args)
                {
                    int digits = arg.Digits == int.MinValue ? Setting.RoundDigits : arg.Digits;

                    string s = string.Format($"{{0:F{digits}}}", ctx.ExecAst(arg.AstRoot));
                    _builder.Replace($"${{{arg.RawString}}}", s);
                }
            }catch(Exception e)
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{e.Message}",ConsoleColor.Yellow);
            }
        }

        public override string GetFormattedString()
        {
            ProcessFormat();
            return _builder.ToString();
        }

        public override string ToString()
        {
            return GetFormattedString();
        }

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

        private static readonly ThreadLocal<StringFormatter> s_hitCountFormatLocal = new ThreadLocal<StringFormatter>(() => new HitCountStringFormatter());

        public static StringFormatter GetPPFormatter()
        {
            var t = s_ppFormatLocal.Value;
            return t;
        }

        private static readonly ThreadLocal<StringFormatter> s_ppFormatLocal = new ThreadLocal<StringFormatter>(() => new PPStringFormatter());

        public static StringFormatter GetHitCountFormatter()
        {
            var t = s_hitCountFormatLocal.Value;
            return t;
        }
    }

    internal class PPStringFormatter : StringFormatter
    {
        public PPStringFormatter():base(Setting.PPFormat)
        {
            Setting.OnSettingChanged += () =>
              {
                  ReplaceFormat(Setting.PPFormat);
              };
        }
    }


    internal class HitCountStringFormatter : StringFormatter
    {
        public HitCountStringFormatter() : base(Setting.HitCountFormat)
        {
            Setting.OnSettingChanged += () =>
            {
                ReplaceFormat(Setting.HitCountFormat);
            };
        }
    }
}
