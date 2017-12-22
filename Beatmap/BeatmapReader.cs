using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    class BeatmapReader
    {
        struct BeatmapHeader
        {
            public int Offset;
            public int Length;
        }

        private BeatmapHeader m_beatmap_header;
        private int _position = 0;

        private byte[] m_beatmap_raw;
        private List<BeatmapObject> m_object_list = new List<BeatmapObject>();

        public byte[] BeatmapRaw => m_beatmap_raw;
        

        public BeatmapReader(string file)
        {
            m_beatmap_header.Offset = 0;
            m_beatmap_header.Length = 0;

            using (var fs = File.OpenRead(file))
            {
                using (var reader = new StreamReader(fs))
                {
                    m_beatmap_raw=Encoding.UTF8.GetBytes(reader.ReadToEnd());
                }
            }
            Parser();
        }

        void ReadLine(out int offset,out int length)
        {
            int count = 0;
            while((_position+count)<m_beatmap_raw.Length)
            {
                if (m_beatmap_raw[_position + count] == '\n')
                {
                    count++;
                    break;
                };
                count++;
            }
            length = count;
            offset = _position;
            _position = offset + count;
        }

        public void Parser()
        {
            int len=Array.LastIndexOf<byte>(m_beatmap_raw,(byte)']');
            m_beatmap_header.Length = len+3;
            _position = m_beatmap_header.Length;

            while(_position<m_beatmap_raw.Length)
            {
                ReadLine(out int offset, out int length);
                string line = Encoding.UTF8.GetString(m_beatmap_raw, offset, length);
                var obj = new BeatmapObject(line, offset, length);
                m_object_list.Add(obj);
            }
        }

        public int GetPosition(int end_time)
        {
            int pos = m_beatmap_header.Length;
            foreach(var obj in m_object_list)
            {
                if (obj.Time > end_time) break;
                pos+=(obj.Length);
            }

            return pos;
        }
    }
}
