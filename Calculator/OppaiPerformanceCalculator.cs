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
    public abstract class OppaiPerformanceCalculator:PerformanceCalculatorBase
    {
        public override double Stars => Oppai.Stars;
        public override double RealTimeStars => Oppai.RealTimeStars;

        public Oppai Oppai { get; private set; } = new Oppai();

        public override void ClearCache()
        {
            base.ClearCache();
            Oppai.Clear();
        }

        protected PPTuple GetPPFromOppai(uint mods,int mode)
        {
            if (Beatmap == null) return PPTuple.Empty;

            Oppai.Beatmap = Beatmap;

            PPTuple ppTuple;
            var result = Oppai.GetMaxPP(mods, mode);
            ppTuple.MaxPP = result.total;
            ppTuple.MaxAimPP = result.aim;
            ppTuple.MaxSpeedPP = result.speed;
            ppTuple.MaxAccuracyPP = result.acc;

            AccuracyRound(Accuracy, Beatmap.ObjectsCount, CountMiss, out int n300, out int n100, out int n50);

            result = Oppai.GetIfFcPP(mods, n300, n100, n50, mode);
            ppTuple.FullComboPP = result.total;
            ppTuple.FullComboAimPP = result.aim;
            ppTuple.FullComboSpeedPP = result.speed;
            ppTuple.FullComboAccuracyPP = result.acc;

            result = Oppai.GetRealTimePP(Time, mods, Count100, Count50, CountMiss, MaxCombo, mode);
            ppTuple.RealTimePP = result.total;
            ppTuple.RealTimeAimPP = result.aim;
            ppTuple.RealTimeSpeedPP = result.speed;
            ppTuple.RealTimeAccuracyPP = result.acc;

            return ppTuple;
        }
    }
}
