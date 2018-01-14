using OsuRTDataProvider.Mods;
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

        private byte[] m_beatmap_raw;
        public byte[] BeatmapRaw => m_beatmap_raw;

        private List<BeatmapObject> m_object_list = new List<BeatmapObject>();

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

        void ReadLine(out int offset,out int length,ref int position)
        {
            int count = 0;
            while((position+count)<m_beatmap_raw.Length)
            {
                if (m_beatmap_raw[position + count] == '\n')
                {
                    count++;
                    break;
                };
                count++;
            }
            length = count;
            offset = position;
            position = offset + count;
        }

        public void Parser()
        {
            int position = 0;
            int len=Array.LastIndexOf(m_beatmap_raw,(byte)']');
            m_beatmap_header.Length=(m_beatmap_raw[len + 1] == '\n') ? len+2 : len+3;

            position = m_beatmap_header.Length;

            while(position<m_beatmap_raw.Length)
            {
                ReadLine(out int offset, out int length,ref position);
                string line = Encoding.UTF8.GetString(m_beatmap_raw, offset, length);
                var obj = new BeatmapObject(line, offset, length);
                m_object_list.Add(obj);
            }
        }

        private int GetPosition(int end_time)
        {
            int pos = m_beatmap_header.Length;
            foreach(var obj in m_object_list)
            {
                if (obj.Time > end_time) break;
                pos+=(obj.Length);
            }

            return pos;
        }

        private ModsInfo _max_mods = ModsInfo.Empty;
        private double _max_pp = 0.0;
        public double GetMaxPP(ModsInfo mod)
        {
            bool need_update = false;
            need_update = need_update || mod != _max_mods;

            if (need_update)
            {
                _max_mods = mod;
                _max_pp = PP.Oppai.get_ppv2(m_beatmap_raw, (uint)m_beatmap_raw.Length, (uint)mod.Mod, 0, 0, 0, -1);
            }
            return _max_pp;
        }

        private int _fc_n100 = -1;
        private int _fc_n50 = -1;
        private double _fc_pp = 0;

        public double GetIfFcPP(ModsInfo mods,int n100,int n50)
        {
            bool need_update = false;
            need_update = need_update || _fc_n100 != n100;
            need_update = need_update || _fc_n50 != n50;

            if (need_update)
            {
                _fc_n100 = n100;
                _fc_n50 = n50;
                _fc_pp = PP.Oppai.get_ppv2(m_beatmap_raw, (uint)m_beatmap_raw.Length, (uint)mods.Mod, n50, n100, 0, -1);
            }

            return _fc_pp;
        }

        private int _pos = -1;
        private int _n100 = -1;
        private int _n50 = -1;
        private int _nmiss = -1;
        private int _max_combo = -1;
        private double _pp = 0;

        public double GetCurrentPP(int end_time,ModsInfo mods,int n100,int n50,int nmiss,int max_combo)
        {
            int pos = GetPosition(end_time);

            bool need_update = false;
            need_update = need_update || _pos != pos;
            need_update = need_update || _n100 != n100;
            need_update = need_update || _n50 != n50;
            need_update = need_update || _nmiss != nmiss;
            need_update = need_update || _max_combo != max_combo;

            if (need_update)
            {
                _pos = pos;
                _n100 = n100;
                _n50 = n50;
                _nmiss = nmiss;
                _max_combo = max_combo;
                _pp = PP.Oppai.get_ppv2(m_beatmap_raw, (uint)pos, (uint)mods.Mod, n50, n100, nmiss, max_combo);
            }

            return _pp;
        }
    }
}
