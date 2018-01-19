using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class TextDisplayer : DisplayerBase
    {
        private char[] m_pp_buffer = new char[1024];
        private char[] m_hit_buffer = new char[1024];
        private int m_pp_str_len = 0;
        private int m_hit_str_len = 0;
        private string m_filename;

        public TextDisplayer(string filename)
        {
            if (Path.IsPathRooted(filename))
                m_filename = filename;
            else
                m_filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            Clear();//Create File
        }

        public override void Clear()
        {
            using (var fp = File.Open(m_filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
            }
        }

        public override void OnUpdatePP(PPTuple tuple)
        {
            var formatter = GetFormattedPP(tuple);

            m_pp_str_len = formatter.CopyTo(0,m_pp_buffer,0);
        }

        public override void OnUpdateHitCount(HitCountTuple tuple)
        {
            var formatter = GetFormattedHitCount(tuple);

            m_hit_str_len = formatter.CopyTo(0, m_hit_buffer, 0);
        }

        private bool _init = false;

        public override void Display()
        {
            if (!_init)
            {
                Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.TEXT_MODE_OUTPUT_PATH_FORMAT, m_filename), ConsoleColor.DarkGreen);
                _init = true;
            }

            using (var fp = File.Open(m_filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var sw = new StreamWriter(fp))
                {
                    sw.Write(m_pp_buffer, 0, m_pp_str_len);
                    sw.Write('\n');
                    sw.Write(m_hit_buffer, 0, m_hit_str_len);
                }
            }
        }
    }
}
