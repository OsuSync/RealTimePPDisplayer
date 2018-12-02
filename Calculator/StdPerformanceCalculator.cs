using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;

namespace RealTimePPDisplayer.Calculator
{
    public sealed class StdPerformanceCalculator : OppaiPerformanceCalculator
    {
        private const int s_mode = 0;//Std

        public override PPTuple GetPerformance()
        {
            return GetPPFromOppai(Mods, s_mode);
        }

        public override double Accuracy=>Oppai.acc_calc(Count300, Count100, Count50, CountMiss) * 100;

        public override void AccuracyRound(double acc, int object_count, int nmiss, out int n300, out int n100, out int n50)
        {
            Oppai.acc_round(acc, object_count, nmiss, out n300, out n100, out n50);
        }
    }
}
