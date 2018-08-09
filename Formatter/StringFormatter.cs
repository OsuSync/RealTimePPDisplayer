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
        private static ThreadLocal<StringFormatter> s_pp_format_local = new ThreadLocal<StringFormatter>(() => new PPStringFormatter());
        private static ThreadLocal<StringFormatter> s_hit_count_format_local = new ThreadLocal<StringFormatter>(() => new HitCountStringFormatter());

        public string Format { get; private set; }
        private StringBuilder m_builder=new StringBuilder(1024);

        private object _mtx = new object();
        public List<FormatArg> m_args = new List<FormatArg>(16);
        static Regex pattern = new Regex(@"\$\{(([A-Z]|[a-z]|[0-9]|_|\.|,|\(|\)|\^|\+|\-|\*|\/)+?)?(@\d+)?\}");
        static Regex new_line_pattern = new Regex(@"(?<=[^\\])\\n");

        protected StringFormatter(string format)
        {
            ReplaceFormat(format);
        }

        protected void ReplaceFormat(string format)
        {
            lock (_mtx)
            {
                m_args.Clear();
                Format = new_line_pattern.Replace(format, Environment.NewLine);

                var result = pattern.Matches(format);

                foreach (Match match in result)
                {
                    FormatArg arg = new FormatArg();
                    arg.RawString = match.Value.TrimStart('$','{').TrimEnd('}');
                    arg.ExprString = arg.RawString;
                    arg.Digits = Int32.MinValue;

                    if (arg.RawString.Contains('@'))
                    {
                        var pair = arg.RawString.Split('@');
                        arg.ExprString = pair[0];
                        arg.Digits = int.Parse(pair[1]);
                    }

                    m_args.Add(arg);
                }
            }
        }

        public void Clear()
        {
            m_builder.Clear();
            m_builder.Append(Format);
        }

        public int CopyTo(int src_index,char[] dst,int dst_index)
        {
            m_builder.CopyTo(src_index,dst,dst_index,m_builder.Length);
            return m_builder.Length;
        }

        public override string ToString()
        {
            return m_builder.ToString();
        }

        public void Fill(FormatArg arg,string val)
        {
            m_builder.Replace($"${{{arg.RawString}}}", val);
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
                foreach (var p in m_args)
                    yield return p;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var p in m_args)
                yield return p;
        }

        public static StringFormatter GetPPFormatter()
        {
            var t = s_pp_format_local.Value;
            t.Clear();
            return t;
        }

        public static StringFormatter GetHitCountFormatter()
        {
            var t = s_hit_count_format_local.Value;
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
