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

        private string[] m_filenames=new string[2];

        private bool m_splited = false;

        public TextDisplayer(string filename,bool splited=false)
        {
            m_splited = splited;

            if (!Path.IsPathRooted(filename))
                m_filenames[0] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            else
                m_filenames[0] = filename;

            if (m_splited)
            {
                string ext = Path.GetExtension(m_filenames[0]);
                string path = Path.GetDirectoryName(m_filenames[0]);
                string file = Path.GetFileNameWithoutExtension(m_filenames[0]);
                m_filenames[0] = $"{path}{Path.DirectorySeparatorChar}{file}-pp{ext}";
                m_filenames[1] = $"{path}{Path.DirectorySeparatorChar}{file}-hit{ext}";
            }
            Clear();//Create File
        }

        public override void Clear()
        {
            using (var fp = File.Open(m_filenames[0], FileMode.Create, FileAccess.Write, FileShare.Read))
            if(m_splited)
            using (var fp2 = File.Open(m_filenames[1], FileMode.Create, FileAccess.Write, FileShare.Read))
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
                foreach(var filename in m_filenames)
                    if(filename!=null)
                        Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.TEXT_MODE_OUTPUT_PATH_FORMAT, filename), ConsoleColor.DarkGreen);
                _init = true;
            }

            StreamWriter[] stream_writers = new StreamWriter[2];

            if (m_splited)
            {
                stream_writers[0] = new StreamWriter(File.Open(m_filenames[0], FileMode.Create, FileAccess.Write, FileShare.Read));
                stream_writers[1] = new StreamWriter(File.Open(m_filenames[1], FileMode.Create, FileAccess.Write, FileShare.Read));
            }
            else
            {
                stream_writers[0] = new StreamWriter(File.Open(m_filenames[0], FileMode.Create, FileAccess.Write, FileShare.Read));
                stream_writers[1] = stream_writers[0];
            }

            stream_writers[0].Write(m_pp_buffer, 0, m_pp_str_len);
            if (!m_splited)
                stream_writers[0].Write(Environment.NewLine);

            stream_writers[1].Write(m_hit_buffer, 0, m_hit_str_len);

            for (int i=0; i < m_filenames.Length; i++)
                if(m_filenames[i]!=null)
                    stream_writers[i].Dispose();
        }
    }
}
