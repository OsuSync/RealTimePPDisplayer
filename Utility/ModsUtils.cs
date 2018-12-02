using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Mods;
using static OsuRTDataProvider.Mods.ModsInfo;

namespace RealTimePPDisplayer.Utility
{
    public static class ModsUtils
    {
        public static ModsInfo ToModsInfo(this uint mods)
        {
            ModsInfo m = new ModsInfo();
            m.Mod = (ModsInfo.Mods) mods;
            return m;
        }

        public static bool HasMod(this uint mods,Mods mod)
        {
            return (mods & (uint)mod) > 0;
        }

        public static double GetTimeRate(uint mods)
        {
            if ((mods & (uint)Mods.Nightcore) > 0 || (mods & (uint)Mods.DoubleTime) > 0)
                return 1.5;
            else if ((mods & (uint)Mods.HalfTime) > 0)
                return 0.75;
            return 1.0;
        }
    }
}
