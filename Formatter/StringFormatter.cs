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
    public struct FormatArgs
    {
        public string RawString { get; set; }
        public string ExprString { get; set; }
        public int Digits { get; set; }
    }

    public class StringFormatter:IEnumerable<FormatArgs>
    {
        private static readonly ThreadLocal<StringFormatter> s_ppFormatLocal = new ThreadLocal<StringFormatter>(() => new PPStringFormatter());
        private static readonly ThreadLocal<StringFormatter> s_hitCountFormatLocal = new ThreadLocal<StringFormatter>(() => new HitCountStringFormatter());

        public string Format { get; private set; }
        private readonly StringBuilder _builder=new StringBuilder(1024);

        private readonly object _mtx = new object();
        private readonly List<FormatArgs> _args = new List<FormatArgs>(16);
        private static readonly Regex s_pattern = new Regex(@"\$\{(\w|\s|_|\.|,|\(|\)|\^|\+|\-|\*|\/|\%|\<|\>|\=|\!|\||\&)*(@\d+)?\}");
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
                    FormatArgs args = new FormatArgs
                    {
                        RawString = rawExpr,
                        ExprString = rawExpr,
                        Digits = Int32.MinValue
                    };

                    if (args.RawString.Contains('@'))
                    {
                        var pair = args.RawString.Split('@');
                        args.ExprString = pair[0];
                        args.Digits = int.Parse(pair[1]);
                    }

                    _args.Add(args);
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

        public void Fill(FormatArgs args,string val)
        {
            _builder.Replace($"${{{args.RawString}}}", val);
        }

        public void Fill(FormatArgs args, int n)
        {
            Fill(args, n.ToString());
        }

        public void Fill(FormatArgs args, double n)
        {
            int digits = args.Digits == Int32.MinValue?Setting.RoundDigits:args.Digits;

            Fill(args, string.Format($"{{0:F{digits}}}",n));
        }

        public IEnumerator<FormatArgs> GetEnumerator()
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
