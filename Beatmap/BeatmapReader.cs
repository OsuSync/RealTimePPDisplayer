using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Calculator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    public class BeatmapReader
    {
        public struct BeatmapHeader
        {
            public int Offset;
            public int Length;
        }

        public OsuRTDataProvider.BeatmapInfo.Beatmap OrtdpBeatmap { get; }

        private BeatmapHeader _beatmapHeaderSpan;
        public BeatmapHeader BeatmapHeaderSpan => _beatmapHeaderSpan;

        public byte[] RawData { get; }
        public List<BeatmapObject> Objects { get; } = new List<BeatmapObject>();

        public int ObjectsCount => Objects.Count;
        public int BeatmapDuration => Objects.LastOrDefault()?.StartTime??-1;

        public OsuPlayMode Mode { get; set; }
        public double OverallDifficulty { get; private set; }
        public double HpDrainRate { get; private set; }
        public double CircleSize { get; private set; }
        public int KeyCount { get; private set; }

        public BeatmapReader(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap,OsuPlayMode mode=OsuPlayMode.Unknown)
        {
            OrtdpBeatmap = beatmap;
            _beatmapHeaderSpan.Offset = 0;
            _beatmapHeaderSpan.Length = 0;
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
                    string blockName = "";
                    while (!sr.EndOfStream)
                    {
                        string rawLine = sr.ReadLine();
                        Debug.Assert(rawLine != null, nameof(rawLine) + " != null");
                        int rawLineLen = Encoding.UTF8.GetByteCount(rawLine) + bias;

                        string line = rawLine.Trim();

                        if (line.StartsWith("["))
                        {
                            blockName = line.Substring(1, line.Length - 2).Trim();
                            if (blockName == "HitObjects")
                                _beatmapHeaderSpan.Length = pos + rawLineLen;
                        }
                        else if (!string.IsNullOrEmpty(line) && (blockName == "General" || blockName == "Difficulty"))
                        {
                            GetPropertyString(line, out var prop, out var val);

                            switch (prop)
                            {
                                case "Mode":
                                    OsuPlayMode mode = (OsuPlayMode)int.Parse(val);
                                    if (mode != OsuPlayMode.Mania && Mode == OsuPlayMode.Mania)
                                    {
                                        Sync.Tools.IO.CurrentIO.WriteColor("[RTPPD::Beatmap]Only support mania beatmap.", ConsoleColor.Yellow);
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
                                    HpDrainRate = double.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "CircleSize":
                                    CircleSize = double.Parse(val,CultureInfo.InvariantCulture);
                                    if (Mode == OsuPlayMode.Mania)
                                        KeyCount = int.Parse(val);
                                    break;
                            }
                        }
                        else if (!string.IsNullOrEmpty(line) && blockName == "HitObjects")
                        {
                            BeatmapObject obj;

                            switch (Mode)
                            {
                                case OsuPlayMode.Mania:
                                    obj = new ManiaBeatmapObject(line, pos, rawLineLen, this);
                                    break;
                                case OsuPlayMode.Osu:
                                case OsuPlayMode.Taiko:
                                    obj =  new BeatmapObject(line, pos, rawLineLen, this);
                                    break;
                                default:
                                    obj = null;
                                    break;
                            }

                            Objects.Add(obj);
                        }

                        pos += rawLineLen;
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
