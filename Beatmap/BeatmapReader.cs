using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Listen;

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

        public int Mode { get; set; }
        public double ApproachRate { get; set; } = -1;
        public double OverallDifficulty { get; private set; }
        public double HpDrainRate { get; private set; }
        public double CircleSize { get; private set; }
        public int KeyCount { get; private set; }

        public BeatmapReader(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap,int mode)
        {
            OrtdpBeatmap = beatmap;
            _beatmapHeaderSpan.Offset = 0;
            _beatmapHeaderSpan.Length = 0;
            Mode = mode;

            StringBuilder sb=new StringBuilder();

            foreach (var line in File.ReadAllLines(beatmap.FilenameFull))
            {
                sb.Append($"{line}\n");
            }

            RawData = Encoding.UTF8.GetBytes(sb.ToString());
            Parse();
        }

        public void Parse()
        {
            int bias = 1;
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
                                    if (mode != OsuPlayMode.Mania && Mode == (int)OsuPlayMode.Mania)
                                    {
                                        Sync.Tools.IO.CurrentIO.WriteColor("[RTPPD::Beatmap]Only support mania beatmap.", ConsoleColor.Yellow);
                                        Mode = (int)mode;
                                    }
                                    else if (mode == OsuPlayMode.Mania && Mode != (int)OsuPlayMode.Mania)
                                    {
                                        Mode = (int)mode;
                                    }
                                    break;
                                case "ApproachRate":
                                    ApproachRate = double.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "OverallDifficulty":
                                    OverallDifficulty = double.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "HPDrainRate":
                                    HpDrainRate = double.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "CircleSize":
                                    CircleSize = double.Parse(val,CultureInfo.InvariantCulture);
                                    if (Mode == (int)OsuPlayMode.Mania)
                                        KeyCount = int.Parse(val);
                                    break;
                            }
                        }
                        else if (!string.IsNullOrEmpty(line) && blockName == "HitObjects")
                        {
                            BeatmapObject obj;

                            switch (Mode)
                            {
                                case (int)OsuPlayMode.Mania:
                                    obj = new ManiaBeatmapObject(line, pos, rawLineLen, this);
                                    break;
                                case (int)OsuPlayMode.CatchTheBeat:
                                case (int)OsuPlayMode.Osu:
                                case (int)OsuPlayMode.Taiko:
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

            if (Mode == (int)OsuPlayMode.Mania)
                Objects.Sort((a,b)=>a.StartTime-b.StartTime);
        }

        public int GetPosition(int endTime, out int nobject)
        {
            int pos = BeatmapHeaderSpan.Length;
            nobject = 0;
            foreach (var obj in Objects)
            {
                if (obj.StartTime > endTime) break;
                pos += obj.Length;
                nobject++;
            }

            return pos;
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
