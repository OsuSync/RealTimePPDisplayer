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
    class StringFormatter:IEnumerable<string>
    {
        private static ThreadLocal<StringFormatter> s_pp_format_local = new ThreadLocal<StringFormatter>(() => new StringFormatter(Setting.PPFormat));
        private static ThreadLocal<StringFormatter> s_hit_count_format_local = new ThreadLocal<StringFormatter>(() => new StringFormatter(Setting.HitCountFormat));

        private string m_format;
        private StringBuilder m_builder=new StringBuilder(1024);

        public List<string> m_args = new List<string>(16);
        static Regex pattern = new Regex(@"\$\{(.+?)\}");

        protected StringFormatter(string format)
        {
            m_format = format;
            m_builder.Append(format);

            var result = pattern.Match(format);

            while(result.Success)
            {
                var key = result.Groups[1].Value.Trim();
                m_args.Add(key);
                result=result.NextMatch();
            }
        }

        public void Clear()
        {
            m_builder.Clear();
            m_builder.Append(m_format);
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

        public void Fill(string key,string val)
        {
            m_builder.Replace($"${{{key}}}", val);
        }

        public void Fill(string name, int n)
        {
            Fill(name, n.ToString());
        }

        public void Fill(string name, double n)
        {
            Fill(name, $"{n:F2}");
        }

        public IEnumerator<string> GetEnumerator()
        {
            foreach (var p in m_args)
                yield return p;
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
}
