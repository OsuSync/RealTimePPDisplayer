using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class TextDisplayer : IDisplayer
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

        public void Clear()
        {
            using (var fp = File.Open(m_filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
            }
        }

        public void OnUpdatePP(double cur_pp, double if_fc_pp, double max_pp)
        {
            StringFormatter.PPFormatter.Clear();
            foreach (var arg in StringFormatter.PPFormatter)
            {
                switch (arg)
                {
                    case "pp":
                        StringFormatter.PPFormatter.Fill(arg, $"{cur_pp:F2}"); break;
                    case "if_fc_pp":
                        StringFormatter.PPFormatter.Fill(arg, $"{if_fc_pp:F2}"); break;
                    case "max_pp":
                        StringFormatter.PPFormatter.Fill(arg, $"{max_pp:F2}"); break;
                }
            }

            m_pp_str_len = StringFormatter.HitCountFormat.CopyTo(0,m_pp_buffer,0);
        }

        public void OnUpdateHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo)
        {
            StringFormatter.HitCountFormat.Clear();
            foreach (var arg in StringFormatter.HitCountFormat)
            {
                switch (arg)
                {
                    case "n300":
                        StringFormatter.HitCountFormat.Fill(arg, n300.ToString()); break;
                    case "n100":
                        StringFormatter.HitCountFormat.Fill(arg, n100.ToString()); break;
                    case "n50":
                        StringFormatter.HitCountFormat.Fill(arg, n50.ToString()); break;
                    case "nmiss":
                        StringFormatter.HitCountFormat.Fill(arg, nmiss.ToString()); break;
                    case "combo":
                        StringFormatter.HitCountFormat.Fill(arg, combo.ToString()); break;
                    case "max_combo":
                        StringFormatter.HitCountFormat.Fill(arg, max_combo.ToString()); break;
                }
            }

            m_hit_str_len = StringFormatter.HitCountFormat.CopyTo(0, m_hit_buffer, 0);
        }

        private bool _init = false;

        public void Display()
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
                    sw.Write('\0');
                }
            }
        }

        public void FixedDisplay(double time)
        {
        }
    }
}
