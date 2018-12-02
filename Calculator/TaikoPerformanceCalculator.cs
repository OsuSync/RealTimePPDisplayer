using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;

namespace RealTimePPDisplayer.Calculator
{
    public sealed class TaikoPerformanceCalculator : OppaiPerformanceCalculator
    {
        private const int s_mode = 1;//taiko

        public override PPTuple GetPerformance()
        {
            return GetPPFromOppai(Mods, s_mode);
        }

        public override double Accuracy => Oppai.taiko_acc_calc(Count300, Count100, CountMiss) * 100;


        public override void AccuracyRound(double acc, int object_count, int nmiss, out int n300, out int n100, out int n50)
        {
            Oppai.taiko_acc_round(acc, object_count, CountMiss, out n300, out n100);
            n50 = 0;
        }
    }
}
