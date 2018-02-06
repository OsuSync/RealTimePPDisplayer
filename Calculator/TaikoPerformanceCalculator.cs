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
    class TaikoPerformanceCalculator : OppaiPerformanceCalculator
    {
        private const OsuPlayMode s_mode = OsuPlayMode.Taiko;

        public override PPTuple GetPP(ModsInfo mods)
        {
            return GetPPFromOppai(mods, s_mode);
        }

        protected override double AccuracyCalculate(int n300, int n100, int n50, int ngeki, int nkatu, int nmiss)
        {
            return Oppai.taiko_acc_calc(n300, n100, nmiss);
        }

        protected override void AccuracyRound(double acc, int object_count, int nmiss, out int n300, out int n100, out int n50)
        {
            Oppai.taiko_acc_round(acc, object_count, CountMiss, out n300, out n100);
            n50 = 0;
        }
    }
}
