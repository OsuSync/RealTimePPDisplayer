using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
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
        public ModsInfo Mods { get; set; }

        public abstract PPTuple GetPerformance();
        public virtual void ClearCache(){}

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
    }
}
