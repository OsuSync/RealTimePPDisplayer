using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Calculator;
using RealTimePPDisplayer.PerformancePoint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    class BeatmapReader
    {
        public struct BeatmapHeader
        {
            public int Offset;
            public int Length;
        }

        public OsuRTDataProvider.BeatmapInfo.Beatmap OrtdpBeatmap { get; private set; }

        private BeatmapHeader m_beatmap_header_span;
        public BeatmapHeader BeatmapHeaderSpan => m_beatmap_header_span;

        public byte[] RawData { get; private set; }
        public List<BeatmapObject> Objects { get; private set; } = new List<BeatmapObject>();

        public int ObjectsCount => Objects.Count;
        public int BeatmapDuration => Objects.LastOrDefault().StartTime;

        public OsuPlayMode Mode { get; set; }
        public double OverallDifficulty { get; private set; }
        public double HPDrainRate { get; private set; }
        public double CircleSize { get; private set; }
        public int KeyCount { get; private set; }

        public BeatmapReader(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap,OsuPlayMode mode=OsuPlayMode.Unknown)
        {
            OrtdpBeatmap = beatmap;
            m_beatmap_header_span.Offset = 0;
            m_beatmap_header_span.Length = 0;
            Mode = mode;

            StringBuilder sb=new StringBuilder();

            foreach (var line in File.ReadAllLines(beatmap.FilenameFull))
            {
                sb.Append($"{line}\r\n");
            }

            RawData = Encoding.UTF8.GetBytes(sb.ToString());
            Parse();
        }

        public void Parse()
        {
            int bias = 2;
            int pos = 0;

            using (var ms = new MemoryStream(RawData))
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
                                m_beatmap_header_span.Length = pos + raw_line_len;
                        }
                        else if (!string.IsNullOrEmpty(line) && (block_name == "General" || block_name == "Difficulty"))
                        {
                            GetPropertyString(line, out var prop, out var val);

                            switch (prop)
                            {
                                case "Mode":
                                    OsuPlayMode mode = (OsuPlayMode)int.Parse(val);
                                    if (mode != OsuPlayMode.Mania && Mode == OsuPlayMode.Mania)
                                    {
                                        Sync.Tools.IO.CurrentIO.WriteColor($"[RTPPD::Beatmap]Only support mania beatmap.", ConsoleColor.Yellow);
                                        Mode = mode;
                                    }
                                    else if (mode == OsuPlayMode.Mania && Mode != OsuPlayMode.Mania)
                                    {
                                        Mode = mode;
                                    }
                                    break;
                                case "OverallDifficulty":
                                    OverallDifficulty = double.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "HPDrainRate":
                                    HPDrainRate = double.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "CircleSize":
                                    CircleSize = double.Parse(val,CultureInfo.InvariantCulture);
                                    if (Mode == OsuPlayMode.Mania)
                                        KeyCount = int.Parse(val);
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

                            Objects.Add(obj);
                        }

                        pos += raw_line_len;
                    }
                }
            }

            if (Mode == OsuPlayMode.Mania)
                Objects.Sort((a,b)=>a.StartTime-b.StartTime);
        }

        #region Tool Function
        private void GetPropertyString(string str, out string prop, out string val)
        {
            var strs = str.Split(':');
            prop = strs[0].Trim();
            val = strs[1].Trim();
        }
        #endregion
    }
}
