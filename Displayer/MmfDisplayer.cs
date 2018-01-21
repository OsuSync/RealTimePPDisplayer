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
    class MmfDisplayer : DisplayerBase
    {
        private string m_mmf_name;

        private char[] m_pp_buffer = new char[1024];
        private char[] m_hit_buffer = new char[1024];
        private int m_hit_str_len = 0;
        private MemoryMappedFile m_mmf;

        private bool m_output = false;

        PPTuple m_current_pp;
        PPTuple m_target_pp;
        PPTuple m_speed;

        public MmfDisplayer(int? id)
        {
            m_mmf_name = id == null ? "rtpp" : $"rtpp{id}";
            m_mmf = MemoryMappedFile.CreateOrOpen(m_mmf_name, 1024);
        }

        public override void Clear()
        {
            m_output = false;
            m_speed = PPTuple.Empty;
            m_current_pp = PPTuple.Empty;
            m_target_pp = PPTuple.Empty;

            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                stream.WriteByte(0);
            }
        }

        private bool _init = false;

        public override void OnUpdatePP(PPTuple tuple)
        {
            m_output = true;

            m_target_pp = tuple;
        }

        public override void OnUpdateHitCount(HitCountTuple tuple)
        {
            var formatter = GetFormattedHitCount(tuple);

            m_hit_str_len= formatter.CopyTo(0,m_hit_buffer,0);
        }

        public override void Display()
        {
            if (!_init)
            {
                Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, m_mmf_name), ConsoleColor.DarkGreen);
                _init = true;
            }
        }

        public override void FixedDisplay(double time)
        {
            if (!m_output) return;
            if (double.IsNaN(m_current_pp.RealTimePP)) m_current_pp.RealTimePP = 0;
            if (double.IsNaN(m_current_pp.FullComboPP)) m_current_pp.FullComboPP = 0;
            if (double.IsNaN(m_speed.RealTimePP)) m_speed.RealTimePP = 0;
            if (double.IsNaN(m_speed.FullComboPP)) m_speed.FullComboPP = 0;

            m_current_pp = SmoothMath.SmoothDampPPTuple(m_current_pp, m_target_pp, ref m_speed, Setting.SmoothTime * 0.001, time);

            var formatter = GetFormattedPP(m_current_pp);

            int len= formatter.CopyTo(0,m_pp_buffer,0);

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

        public override void OnDestroy()
        {
            m_mmf.Dispose();
        }
    }
}
