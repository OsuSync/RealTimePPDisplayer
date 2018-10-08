using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{
    public struct FormatArg
    {
        public string RawString { get; set; }
        public string ExprString { get; set; }
        public int Digits { get; set; }
    }

    public class StringFormatter:IEnumerable<FormatArg>
    {
        private static readonly ThreadLocal<StringFormatter> s_ppFormatLocal = new ThreadLocal<StringFormatter>(() => new PPStringFormatter());
        private static readonly ThreadLocal<StringFormatter> s_hitCountFormatLocal = new ThreadLocal<StringFormatter>(() => new HitCountStringFormatter());

        public string Format { get; private set; }
        private readonly StringBuilder _builder=new StringBuilder(1024);

        private readonly object _mtx = new object();
        private readonly List<FormatArg> _args = new List<FormatArg>(16);
        private static readonly Regex s_pattern = new Regex(@"\$\{(\w|\s|_|\.|,|\(|\)|\^|\+|\-|\*|\/|\%)*(@\d+)?\}");
        private static readonly Regex s_newLinePattern = new Regex(@"(?<=[^\\])\\n");

        protected StringFormatter(string format)
        {
            ReplaceFormat(format);
        }

        protected void ReplaceFormat(string format)
        {
            lock (_mtx)
            {
                _args.Clear();
                Format = s_newLinePattern.Replace(format, Environment.NewLine);

                var result = s_pattern.Matches(format);

                foreach (Match match in result)
                {
                    string rawExpr =  match.Value.TrimStart('$','{').TrimEnd('}');
                    FormatArg arg = new FormatArg
                    {
                        RawString = rawExpr,
                        ExprString = rawExpr,
                        Digits = Int32.MinValue
                    };

                    if (arg.RawString.Contains('@'))
                    {
                        var pair = arg.RawString.Split('@');
                        arg.ExprString = pair[0];
                        arg.Digits = int.Parse(pair[1]);
                    }

                    _args.Add(arg);
                }
            }
        }

        public void Clear()
        {
            _builder.Clear();
            _builder.Append(Format);
        }

        public int CopyTo(int srcIndex,char[] dst,int dstIndex)
        {
            _builder.CopyTo(srcIndex,dst,dstIndex,_builder.Length);
            return _builder.Length;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public void Fill(FormatArg arg,string val)
        {
            _builder.Replace($"${{{arg.RawString}}}", val);
        }

        public void Fill(FormatArg arg, int n)
        {
            Fill(arg, n.ToString());
        }

        public void Fill(FormatArg arg, double n)
        {
            int digits = arg.Digits == Int32.MinValue?Setting.RoundDigits:arg.Digits;

            Fill(arg, string.Format($"{{0:F{digits}}}",n));
        }

        public IEnumerator<FormatArg> GetEnumerator()
        {
            lock (_mtx)
            {
                foreach (var p in _args)
                    yield return p;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var p in _args)
                yield return p;
        }

        public static StringFormatter GetPPFormatter()
        {
            var t = s_ppFormatLocal.Value;
            t.Clear();
            return t;
        }

        public static StringFormatter GetHitCountFormatter()
        {
            var t = s_hitCountFormatLocal.Value;
            t.Clear();
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
