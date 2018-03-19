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
        private MemoryMappedFile[] m_mmfs=new MemoryMappedFile[2];

        private bool m_output = false;

        PPTuple m_current_pp;
        PPTuple m_target_pp;
        PPTuple m_speed;

        private bool m_splited = false;

        public MmfDisplayer(int? id,bool splited = false)
        {
            m_splited = splited;
            m_mmf_name = id == null ? "rtpp" : $"rtpp{id}";

            if (m_splited)
            {
                m_mmfs[0] = MemoryMappedFile.CreateOrOpen($"{m_mmf_name}-pp", 1024);
                m_mmfs[1] = MemoryMappedFile.CreateOrOpen($"{m_mmf_name}-hit", 1024);
            }
            else
            {
                m_mmfs[0] = MemoryMappedFile.CreateOrOpen(m_mmf_name, 1024);
            }
        }

        public override void Clear()
        {
            m_output = false;
            m_speed = PPTuple.Empty;
            m_current_pp = PPTuple.Empty;
            m_target_pp = PPTuple.Empty;

            foreach (var mmf in m_mmfs)
            {
                if (mmf != null)
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
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
                if(m_splited)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, $"{m_mmf_name}-pp"), ConsoleColor.DarkGreen);
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, $"{m_mmf_name}-hit"), ConsoleColor.DarkGreen);
                }
                else
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

            m_current_pp = SmoothMath.SmoothDampPPTuple(m_current_pp, m_target_pp, ref m_speed, time);

            var formatter = GetFormattedPP(m_current_pp);

            int len= formatter.CopyTo(0,m_pp_buffer,0);

            StreamWriter[] stream_writers = new StreamWriter[2];

            if (m_splited)
            {
                stream_writers[0] = new StreamWriter(m_mmfs[0].CreateViewStream());
                stream_writers[1] = new StreamWriter(m_mmfs[1].CreateViewStream());
            }
            else
            {
                stream_writers[0] = new StreamWriter(m_mmfs[0].CreateViewStream());
                stream_writers[1] = stream_writers[0];
            }

            stream_writers[0].Write(m_pp_buffer, 0, len);
            if (!m_splited) stream_writers[0].Write('\n');
            else stream_writers[0].Write('\0');

            stream_writers[1].Write(m_hit_buffer, 0, m_hit_str_len);
            stream_writers[1].Write('\0');

            for (int i = 0; i < m_mmfs.Length; i++)
                if (m_mmfs[i] != null)
                    stream_writers[i].Dispose();
        }

        public override void OnDestroy()
        {
            foreach(var mmf in m_mmfs)
                if(mmf!=null)
                    mmf.Dispose();
        }
    }
}
