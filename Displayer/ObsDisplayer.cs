using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class ObsDisplayer : IDisplayer
    {
        private MemoryMappedFile m_mmf;
        private StringBuilder m_str_builder = new StringBuilder(1024);
        private byte[] m_str_buffer = new byte[1024];

        public ObsDisplayer(int? id)
        {
            m_mmf = MemoryMappedFile.CreateOrOpen(id==null?"rtpp":$"rtpp{id}", 1024);
        }

        ~ObsDisplayer()
        {
            m_mmf.Dispose();
        }

        public void Clear()
        {
            m_str_buffer[0] = 0;
            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                stream.Write(m_str_buffer, 0, 1);
            }
        }

        public void Display(double pp, int n300, int n100, int n50, int nmiss)
        {
            m_str_builder.Clear();
            m_str_builder.AppendFormat("{0:F2}pp", pp);

            if (Setting.DisplayHitObject)
                m_str_builder.AppendFormat("\n{0}x100 {1}x50 {2}xMiss", n100, n50, nmiss);

            for (int i = 0; i < m_str_builder.Length; i++)
                m_str_buffer[i] = (byte)m_str_builder[i];
            m_str_buffer[m_str_builder.Length] = 0;

            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                stream.Write(m_str_buffer,0,m_str_builder.Length+1);
            }
        }
    }
}
