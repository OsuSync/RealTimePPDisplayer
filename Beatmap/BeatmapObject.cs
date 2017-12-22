using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    public class BeatmapObject
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public int Time { get; set; }

        public BeatmapObject(string line,int offset,int len)
        {
            Offset = offset;
            Length = len;

            string[] t = line.Split(',');
            Time = int.Parse(t[2]);
        }
    }
}
