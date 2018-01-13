using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class MmfDisplayer : IDisplayer
    {
        private string m_mmf_name;

        private char[] m_pp_buffer = new char[1024];
        private char[] m_hit_buffer = new char[1024];
        private int m_hit_str_len = 0;
        private MemoryMappedFile m_mmf;

        private bool m_output = false;

        private double m_target_pp=0.0;
        private double m_current_pp=0.0;

        private double m_max_pp = 0.0;
        private double m_if_fc_pp = 0.0;

        private double m_speed=0.0;

        private double m_smooth_time = 0.2;

        public MmfDisplayer(int? id)
        {
            m_mmf_name = id == null ? "rtpp" : $"rtpp{id}";
            m_mmf = MemoryMappedFile.CreateOrOpen(m_mmf_name, 1024);

            m_smooth_time = Setting.SmoothTime / 1000.0;
        }

        ~MmfDisplayer()
        {
            m_mmf.Dispose();
        }

        public void Clear()
        {
            m_output = false;
            m_target_pp = 0;
            m_current_pp = 0;
            m_speed = 0;

            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                stream.WriteByte(0);
            }
        }

        private bool _init = false;

        public void OnUpdatePP(double cur_pp, double if_fc_pp, double max_pp)
        {
            m_output = true;

            if (double.IsNaN(cur_pp)) cur_pp = 0;
            if (double.IsNaN(if_fc_pp)) if_fc_pp = 0;
            if (double.IsNaN(max_pp)) max_pp = 0;
            m_target_pp = cur_pp;
            m_if_fc_pp = if_fc_pp;
            m_max_pp = max_pp;
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

            m_hit_str_len=StringFormatter.HitCountFormat.CopyTo(0,m_hit_buffer,0);
        }

        public void Display()
        {
            if (!_init)
            {
                Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, m_mmf_name), ConsoleColor.DarkGreen);
                _init = true;
            }
        }

        public void FixedDisplay(double time)
        {
            if (!m_output) return;
            if (double.IsNaN(m_current_pp)) m_current_pp = 0;
            if (double.IsNaN(m_speed)) m_speed = 0;

            m_current_pp = SmoothMath.SmoothDamp(m_current_pp, m_target_pp, ref m_speed, m_smooth_time, time);
            StringFormatter.PPFormatter.Clear();
            foreach (var arg in StringFormatter.PPFormatter)
            {
                switch (arg)
                {
                    case "rtpp":
                        StringFormatter.PPFormatter.Fill(arg, $"{m_current_pp:F2}"); break;
                    case "if_fc_pp":
                        StringFormatter.PPFormatter.Fill(arg, $"{m_if_fc_pp:F2}"); break;
                    case "max_pp":
                        StringFormatter.PPFormatter.Fill(arg, $"{m_max_pp:F2}"); break;
                }
            }

            int len=StringFormatter.PPFormatter.CopyTo(0,m_pp_buffer,0);

            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write(m_pp_buffer,0,len);
                    sw.Write('\n');
                    sw.Write(m_hit_buffer, 0, m_hit_str_len);
                    sw.Write('\0');
                }
            }
        }
    }
}
