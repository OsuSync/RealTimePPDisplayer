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
            var formatter = StringFormatter.GetPPFormatter();

            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "pp":
                        formatter.Fill(arg, cur_pp); break;
                    case "if_fc_pp":
                        formatter.Fill(arg, if_fc_pp); break;
                    case "max_pp":
                        formatter.Fill(arg, max_pp); break;
                }
            }

            m_pp_str_len = formatter.CopyTo(0,m_pp_buffer,0);
        }

        public void OnUpdateHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "n300":
                        formatter.Fill(arg, n300); break;
                    case "n100":
                        formatter.Fill(arg, n100); break;
                    case "n50":
                        formatter.Fill(arg, n50); break;
                    case "nmiss":
                        formatter.Fill(arg, nmiss); break;
                    case "combo":
                        formatter.Fill(arg, combo); break;
                    case "max_combo":
                        formatter.Fill(arg, max_combo); break;
                }
            }

            m_hit_str_len = formatter.CopyTo(0, m_hit_buffer, 0);
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
