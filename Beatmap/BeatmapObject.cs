using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    public class BeatmapObject
    {
        public int Time { get; set; }
        public string ObjectStr { get; set; }

        public BeatmapObject(string line)
        {
            string[] t = line.Split(',');
            Time = int.Parse(t[2]);
            ObjectStr = line;
        }
    }
}
