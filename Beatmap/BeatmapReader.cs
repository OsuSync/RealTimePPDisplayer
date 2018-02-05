using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.PP;
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

        public byte[] BeatmapRaw { get; private set; }
        public List<BeatmapObject> ObjectList { get; private set; } = new List<BeatmapObject>();

        public int RealTimeMaxCombo => m_real_time_data.max_combo;
        public int FullCombo => m_cache.max_combo;
        public int ObjectCount => ObjectList.Count;

        public OsuPlayMode Mode { get; set; }
        public double OverallDifficulty { get; private set; }
        public double HPDrainRate { get; private set; }
        public double CircleSize { get; private set; }
        public int KeyMode { get; private set; }

        private BeatmapHeader m_beatmap_header;
        private Oppai.pp_params m_real_time_data = new Oppai.pp_params();
        private Oppai.pp_params m_cache=new Oppai.pp_params();

        public BeatmapReader(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            m_beatmap_header.Offset = 0;
            m_beatmap_header.Length = 0;

            using (var fs = File.OpenRead(beatmap.FilenameFull))
            {
                BeatmapRaw = new byte[fs.Length];
                fs.Read(BeatmapRaw, 0, (int)fs.Length);
            }
            Parse();
        }

        public void Parse()
        {
            int bias = 2;

            int pos = Array.IndexOf(BeatmapRaw, (byte)'\n');
            if (BeatmapRaw[pos - 1] != '\r')
                bias = 1;

            pos = 0;

            using (var ms = new MemoryStream(BeatmapRaw))
            {
                using (var sr = new StreamReader(ms))
                {
                    string block_name = "";
                    while (!sr.EndOfStream)
                    {
                        string raw_line = sr.ReadLine();
                        int raw_line_len = Encoding.UTF8.GetByteCount(raw_line) + bias;

                        string line = raw_line.Trim();

                        if (line.StartsWith("["))
                        {
                            block_name = line.Substring(1, line.Length - 2).Trim();
                            if (block_name == "HitObjects")
                                m_beatmap_header.Length = pos + raw_line_len;
                        }
                        else if (!string.IsNullOrEmpty(line) && (block_name == "General" || block_name == "Difficulty"))
                        {
                            GetPropertyString(line, out var prop, out var val);

                            switch (prop)
                            {
                                case "Mode":
                                    Mode = (OsuPlayMode)int.Parse(val);
                                    break;
                                case "OverallDifficulty":
                                    OverallDifficulty = double.Parse(val);
                                    break;
                                case "HPDrainRate":
                                    HPDrainRate = double.Parse(val);
                                    break;
                                case "CircleSize":
                                    CircleSize = double.Parse(val);
                                    if (Mode == OsuPlayMode.Mania)
                                        KeyMode = int.Parse(val);
                                    break;
                            }

                        }
                        else if (!string.IsNullOrEmpty(line) && block_name == "HitObjects")
                        {
                            BeatmapObject obj;
                            if (Mode != OsuPlayMode.Mania)
                                obj = new BeatmapObject(line, pos, raw_line_len, this);
                            else
                                obj = new ManiaBeatmapObject(line, pos, raw_line_len, this);

                            ObjectList.Add(obj);
                        }

                        pos += raw_line_len;
                    }
                }
            }

            if (Mode == OsuPlayMode.Mania)
                ObjectList.Sort();
        }

        #region Tool Function
        private void GetPropertyString(string str,out string prop,out string val)
        {
            var strs=str.Split(':');
            prop = strs[0].Trim();
            val = strs[1].Trim();
        }
        #endregion

        #region Oppai PP calculate
        private int GetPosition(int end_time,out int nline)
        {
            int pos = m_beatmap_header.Length;
            nline = 0;
            foreach(var obj in ObjectList)
            {
                if (obj.StartTime > end_time) break;
                pos+=(obj.Length);
                nline++;
            }

            return pos;
        }

        private ModsInfo _max_mods = ModsInfo.Empty;
        private Oppai.pp_calc _max_result;

        public Oppai.pp_calc GetMaxPP(ModsInfo mods,OsuPlayMode mode)
        {
            bool need_update = false;
            need_update = need_update || mods != _max_mods;

            if (need_update)
            {
                _max_mods = mods;

                Oppai.rtpp_params args;
                args.combo = Oppai.FullCombo;
                args.mods = (uint)mods.Mod;
                args.n100 = 0;
                args.n50 = 0;
                args.nmiss = 0;
                args.mode = (uint)mode;

                //Cache Beatmap
                Oppai.get_ppv2(BeatmapRaw, (uint)BeatmapRaw.Length,ref args, false,m_cache,ref _max_result);
            }
            return _max_result;
        }

        private int _fc_n100 = -1;
        private int _fc_n50 = -1;
        private Oppai.pp_calc _fc_result;

        public Oppai.pp_calc GetIfFcPP(ModsInfo mods,int n300,int n100,int n50, OsuPlayMode mode)
        {
            bool need_update = false;
            need_update = need_update || _fc_n100 != n100;
            need_update = need_update || _fc_n50 != n50;


            if (need_update)
            {
                _fc_n100 = n100;
                _fc_n50 = n50;

                Oppai.rtpp_params args;
                args.combo = Oppai.FullCombo;
                args.mods = (uint)mods.Mod;
                args.n100 = n100;
                args.n50 = n50;
                args.nmiss = 0;
                args.mode = (uint)mode;

                Oppai.get_ppv2(BeatmapRaw, (uint)BeatmapRaw.Length,ref args,true,m_cache,ref _fc_result);
            }

            return _fc_result;
        }

        private int _pos = -1;
        private int _n100 = -1;
        private int _n50 = -1;
        private int _nmiss = -1;
        private int _max_combo = -1;
        private Oppai.pp_calc _rtpp_result;

        public Oppai.pp_calc GetRealTimePP(int end_time,ModsInfo mods,int n100,int n50,int nmiss,int max_combo, OsuPlayMode mode)
        {
            int pos = GetPosition(end_time,out int nobject);

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

                Oppai.rtpp_params args;
                args.combo = max_combo;
                args.mods = (uint)mods.Mod;
                args.n100 = n100;
                args.n50 = n50;
                args.nmiss = nmiss;
                args.mode = (uint)mode;

                if (!Oppai.get_ppv2(BeatmapRaw, (uint)pos, ref args, false,m_real_time_data, ref _rtpp_result))
                {
                    return Oppai.pp_calc.Empty;
                }
            }

            return _rtpp_result;
        }

        public void Clear()
        {
            _pos = -1;
            _n100 = -1;
            _n50 = -1;
            _nmiss = -1;
            _max_combo = -1;
            _rtpp_result = Oppai.pp_calc.Empty;

            _fc_n100 = -1;
            _fc_n50 = -1;
            _fc_result = Oppai.pp_calc.Empty;

            _max_mods = ModsInfo.Empty;
            _max_result = Oppai.pp_calc.Empty;
        }
    }
    #endregion
}
