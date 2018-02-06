using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.PerformancePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Calculator
{
    abstract class OppaiPerformanceCalculator:PerformanceCalculatorBase
    {
        public Oppai Oppai { get;private set; } = new Oppai();

        public override void ClearCache()
        {
            Oppai.Clear();
        }

        protected abstract double AccuracyCalculate(int n300, int n100, int n50,int ngeki,int nkatu, int nmiss);
        protected abstract void AccuracyRound(double acc, int object_count, int nmiss, out int n300, out int n100, out int n50);

        protected PPTuple GetPPFromOppai(ModsInfo mods,OsuPlayMode mode)
        {
            if (Beatmap == null) return PPTuple.Empty;

            Oppai.Beatmap = Beatmap;

            PPTuple pp_tuple;
            var result = Oppai.GetMaxPP(mods, mode);
            pp_tuple.MaxPP = result.total;
            pp_tuple.MaxAimPP = result.aim;
            pp_tuple.MaxSpeedPP = result.speed;
            pp_tuple.MaxAccuracyPP = result.acc;

            double acc = AccuracyCalculate(Count300, Count100, Count50,CountGeki,CountKatu,CountMiss) * 100.0;
            AccuracyRound(acc, Beatmap.ObjectCount, CountMiss, out int n300, out int n100, out int n50);

            result = Oppai.GetIfFcPP(mods, n300, n100, n50, mode);
            pp_tuple.FullComboPP = result.total;
            pp_tuple.FullComboAimPP = result.aim;
            pp_tuple.FullComboSpeedPP = result.speed;
            pp_tuple.FullComboAccuracyPP = result.acc;

            result = Oppai.GetRealTimePP(Time, mods, Count100, Count50, CountMiss, MaxCombo, mode);
            pp_tuple.RealTimePP = result.total;
            pp_tuple.RealTimeAimPP = result.aim;
            pp_tuple.RealTimeSpeedPP = result.speed;
            pp_tuple.RealTimeAccuracyPP = result.acc;

            return pp_tuple;
        }
    }
}
