using OsuRTDataProvider.Listen;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Calculator
{
    public abstract class PerformanceCalculatorBase
    {
        public BeatmapReader Beatmap { get; set; }
        public int Count300 { get; set; }
        public int Count100 { get; set; }
        public int Count50 { get; set; }
        public int CountGeki { get; set; }
        public int CountKatu { get; set; }
        public int CountMiss { get; set; }
        public int Time { get; set; }
        public int MaxCombo { get; set; }
        public int Score { get; set; }
        
        public uint Mods { get; set; }

        public abstract double Stars { get; }
        public abstract double RealTimeStars { get; }

        public abstract PPTuple GetPerformance();

        public virtual void ClearCache()
        {
            Count300 = Count100 = Count50 = CountGeki = CountKatu = CountMiss = 0;
            Time = 0;
            MaxCombo = 0;
            Score = 0;
        }

        /// <summary>
        /// Gets the accuracy(0 ~ 100).
        /// </summary>
        /// <value>
        /// The accuracy.
        /// </value>
        public abstract double Accuracy { get; }

        public virtual void AccuracyRound(double acc, int object_count, int nmiss, out int n300, out int n100, out int n50)
        {
            n300 = n100 = n50 = 0;
        }

        public void AccuracyRound(double acc,int nmiss)
        {
            AccuracyRound(acc, Beatmap.ObjectsCount, nmiss, out int n300, out int n100, out int n50);
            Count300 = n300;
            Count100 = n100;
            Count50 = n50;
            CountMiss = nmiss;
        }
    }
}
