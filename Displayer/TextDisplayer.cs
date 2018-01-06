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
        private string m_filename;
        private StringBuilder m_str_builder=new StringBuilder(1024);
        private byte[] m_str_buffer = new byte[1024];

        public TextDisplayer(string filename)
        {
            if (Path.IsPathRooted(filename))
                m_filename = filename;
            else
                m_filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        }

        public void Clear()
        {
            using (var fp = File.Open(m_filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
            }
        }

        public void Display(double pp, int n300, int n100, int n50, int nmiss)
        {
            m_str_builder.Clear();
            m_str_builder.AppendFormat("{0:F2}pp", pp);

            if (Setting.DisplayHitObject)
                m_str_builder.AppendFormat("\n{0}x100 {1}x50 {2}xMiss",n100,n50,nmiss);

            for (int i = 0; i < m_str_builder.Length; i++)
                m_str_buffer[i] = (byte)m_str_builder[i];

            using (var fp=File.Open(m_filename,FileMode.Create,FileAccess.Write,FileShare.Read))
            {
                fp.Write(m_str_buffer, 0, m_str_builder.Length);
            }
        }
    }
}
