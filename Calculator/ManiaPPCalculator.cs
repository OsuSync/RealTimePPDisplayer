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
    class ManiaPPCalculator : PPCalculatorBase
    {
        private const OsuPlayMode s_mode = OsuPlayMode.Mania;

        public override PPTuple GetPP(ModsInfo mods)
        {
            throw new NotImplementedException();
        }
    }
}
