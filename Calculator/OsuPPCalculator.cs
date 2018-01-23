using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Displayer;

namespace RealTimePPDisplayer.Calculator
{
    class StdPPCalculator : PPCalculatorBase
    {
        public override PPTuple GetPP(ModsInfo mods)
        {
            if (Beatmap == null) return PPTuple.Empty;

            PPTuple pp_tuple;
            var result = Beatmap.GetMaxPP(mods);
            pp_tuple.MaxPP = result.total;
            pp_tuple.MaxAimPP = result.aim;
            pp_tuple.MaxSpeedPP = result.speed;
            pp_tuple.MaxAccuracyPP = result.acc;

            result = Beatmap.GetIfFcPP(mods, Count300, Count100, Count50, CountMiss);
            pp_tuple.FullComboPP = result.total;
            pp_tuple.FullComboAimPP = result.aim;
            pp_tuple.FullComboSpeedPP = result.speed;
            pp_tuple.FullComboAccuracyPP = result.acc;

            result = Beatmap.GetRealTimePP(Time, mods, Count100, Count50, CountMiss, MaxCombo);
            pp_tuple.RealTimePP = result.total;
            pp_tuple.RealTimeAimPP = result.aim;
            pp_tuple.RealTimeSpeedPP = result.speed;
            pp_tuple.RealTimeAccuracyPP = result.acc;

            return pp_tuple;
        }
    }
}
