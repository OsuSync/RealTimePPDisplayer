using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.PerformancePoint;

namespace RealTimePPDisplayer.Calculator
{
    class StdPPCalculator : OppaiPerformanceCalculator
    {
        private const OsuPlayMode s_mode = OsuPlayMode.Osu;

        public override PPTuple GetPP()
        {
            return GetPPFromOppai(Mods, s_mode);
        }

        public override double Accuracy=>Oppai.acc_calc(Count300, Count100, Count50, CountMiss);

        public override void AccuracyRound(double acc, int object_count, int nmiss, out int n300, out int n100, out int n50)
        {
            Oppai.acc_round(acc, object_count, nmiss, out n300, out n100, out n50);
        }
    }
}
