using OsuRTDataProvider.Listen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Beatmap
{
    class BeatmapObject
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
            Type = int.Parse(breaked[3]);
            EndTime = StartTime;
        }

    }

    class ManiaBeatmapObject : BeatmapObject
    {
        public int Key { get; private set; }
        public double OverallStrain { get; private set; } = 1;

        public double[] IndividuaStrains { get; private set; } = new double[18];
        public int[] HeldUntil { get; private set; } = new int[18];

        public ManiaBeatmapObject(string line, int offset, int len, BeatmapReader beatmap) : base(line, offset, len, beatmap)
        {
            string[] breaked = line.Split(',');
            ManiaParse(breaked);
        }

        private const double INDIVIDUAL_DECAY_BASE = 0.125;
        private const double OVERALL_DECAY_BASE = 0.30;


        private void ManiaParse(string[] breaked)
        {
            string[] addition = breaked[6].Split();

            if (Type == 128)
                EndTime = int.Parse(addition[0]);

            double interval = 512.0 / Beatmap.KeyMode;

            for (int i = 1; i <= Beatmap.KeyMode; i++)
            {
                if (X > interval * (i - 1) && X < i * interval)
                    Key = i;
            }
        }

        private void ManiaCalculateStrains(ManiaBeatmapObject prev, double time_rate)
        {
            double addition = 1;
            double time_elapsed = (StartTime - prev.StartTime) / time_rate / 1000.0;
            double individual_decay = Math.Pow(INDIVIDUAL_DECAY_BASE, time_elapsed);
            double overall_decay = Math.Pow(OVERALL_DECAY_BASE, time_elapsed);

            double hold_factor = 1;
            double hold_addition = 0;

            for (int i = 0; i < Beatmap.KeyMode; i++)
            {
                HeldUntil[i] = prev.HeldUntil[i];

                if (StartTime < HeldUntil[i] && EndTime > HeldUntil[i])
                    hold_addition = 1;
                if (EndTime == HeldUntil[i])
                    hold_addition = 0;
                if (HeldUntil[i] > EndTime)
                    hold_factor = 1.25;

                IndividuaStrains[i] = prev.IndividuaStrains[i] * individual_decay;
            }

            HeldUntil[Key] = EndTime;
            IndividuaStrains[Key] = IndividuaStrains[Key] + 2 * hold_factor;

            OverallStrain = prev.OverallStrain * overall_decay + (addition + hold_addition) * hold_factor;
        }
    }
}
