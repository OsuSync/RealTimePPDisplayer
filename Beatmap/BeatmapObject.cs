using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    public class BeatmapObject
    {
        public int Offset { get; protected set; }
        public int Length { get; protected set; }
        public int StartTime { get; protected set; }
        public int EndTime { get; protected set; }
        public int Type { get; protected set; }

        public int X { get; protected set; }
        public int Y { get; protected set; }

        protected BeatmapReader Beatmap { get; private set;}

        public BeatmapObject(string line,int offset,int len,BeatmapReader beatmap)
        {
            Offset = offset;
            Length = len;
            Beatmap = beatmap;

            string[] breaked = line.Split(',');

            X = int.Parse(breaked[0]);
            Y = int.Parse(breaked[1]);
            StartTime = int.Parse(breaked[2]);
            EndTime = StartTime;
            Type = int.Parse(breaked[3]);
        }

    }

    class ManiaBeatmapObject : BeatmapObject
    {
        public int Key { get; private set; }
        public double OverallStrain { get; private set; } = 1;

        public double IndividualStrain
        {
            get => IndividualStrains[Key];
            set => IndividualStrains[Key] = value;
        }

        public double[] IndividualStrains { get; private set; }
        public int[] HeldUntil { get; private set; }

        public ManiaBeatmapObject(string line, int offset, int len, BeatmapReader beatmap) : base(line, offset, len, beatmap)
        {
            IndividualStrains = new double[Beatmap.KeyCount];
            HeldUntil = new int[Beatmap.KeyCount];

            string[] breaked = line.Split(',');
            ManiaParse(breaked);
        }

        public void ClearStrainsValue()
        {
            OverallStrain = 1;
            for(int i=0;i<Beatmap.KeyCount;++i)
            {
                IndividualStrains[i] = 0;
                HeldUntil[i] = 0;
            }
        }

        internal static readonly double INDIVIDUAL_DECAY_BASE = 0.125;
        internal static readonly double OVERALL_DECAY_BASE = 0.30;


        private void ManiaParse(string[] breaked)
        {
            string[] addition = breaked[5].Split(':');

            if (Type == 128)
                EndTime = int.Parse(addition[0]);

            int column_width = 512 / Beatmap.KeyCount;
            Key = X / column_width;
        }

        public void ManiaCalculateStrains(ManiaBeatmapObject prev, double time_rate)
        {
            double addition = 1;
            double time_elapsed = (StartTime - prev.StartTime) / time_rate;
            double individual_decay = Math.Pow(INDIVIDUAL_DECAY_BASE, time_elapsed / 1000);
            double overall_decay = Math.Pow(OVERALL_DECAY_BASE, time_elapsed / 1000);

            double hold_factor = 1;
            double hold_addition = 0;

            for (int i = 0; i < Beatmap.KeyCount; i++)
            {
                HeldUntil[i] = prev.HeldUntil[i];

                if (StartTime < HeldUntil[i] && EndTime > HeldUntil[i])
                    hold_addition = 1;

                if (EndTime == HeldUntil[i])
                    hold_addition = 0;

                if (HeldUntil[i] > EndTime)
                    hold_factor = 1.25;

                IndividualStrains[i] = prev.IndividualStrains[i] * individual_decay;
            }
            HeldUntil[Key] = EndTime;
            IndividualStrain += 2.0 * hold_factor;

            OverallStrain = prev.OverallStrain * overall_decay + (addition + hold_addition) * hold_factor;
        }
    }
}
