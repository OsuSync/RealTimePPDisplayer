using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Calculator
{
    public class Oppai
    {
        private const uint c_unknown_mods = 0xffffffffu;

        private pp_params _realTimeData = new pp_params();
        private readonly pp_params _cache = new pp_params();

        public int RealTimeMaxCombo => _realTimeData.max_combo;
        public int FullCombo => _cache.max_combo;

        public BeatmapReader Beatmap { get; set; }

        private uint _lastMods = c_unknown_mods;
        private pp_calc _maxResult;

        public pp_calc GetMaxPP(uint mods, int mode)
        {
            bool needUpdate = mods != _lastMods;

            if (needUpdate)
            {
                _lastMods = mods;

                rtpp_params args;
                args.combo = FULL_COMBO;
                args.mods = mods;
                args.n100 = 0;
                args.n50 = 0;
                args.nmiss = 0;
                args.mode = (uint)mode;

                //Cache Beatmap
                get_ppv2(Beatmap.RawData, (uint)Beatmap.RawData.Length, ref args, false, _cache, ref _maxResult);
            }
            return _maxResult;
        }

        private int _fcN100 = -1;
        private int _fcN50 = -1;
        private pp_calc _fcResult;

        public pp_calc GetIfFcPP(uint mods, int n300, int n100, int n50, int mode)
        {
            var needUpdate = _fcN100 != n100;
            needUpdate = needUpdate || _fcN50 != n50;


            if (needUpdate)
            {
                _fcN100 = n100;
                _fcN50 = n50;

                rtpp_params args;
                args.combo = FULL_COMBO;
                args.mods = mods;
                args.n100 = n100;
                args.n50 = n50;
                args.nmiss = 0;
                args.mode = (uint)mode;

                get_ppv2(Beatmap.RawData, (uint)Beatmap.RawData.Length, ref args, true, _cache, ref _fcResult);
            }

            return _fcResult;
        }

        private int _pos = -1;
        private int _n100 = -1;
        private int _n50 = -1;
        private int _nmiss = -1;
        private int _maxCombo = -1;
        private pp_calc _rtppResult;

        public pp_calc GetRealTimePP(int endTime, uint mods, int n100, int n50, int nmiss, int maxCombo, int mode)
        {
            int pos = Beatmap.GetPosition(endTime, out int nobject);

            var needUpdate = false;
            needUpdate = _pos != pos;
            needUpdate = needUpdate || _n100 != n100;
            needUpdate = needUpdate || _n50 != n50;
            needUpdate = needUpdate || _nmiss != nmiss;
            needUpdate = needUpdate || _maxCombo != maxCombo;

            if (needUpdate)
            {
                _pos = pos;
                _n100 = n100;
                _n50 = n50;
                _nmiss = nmiss;
                _maxCombo = maxCombo;

                rtpp_params args;
                args.combo = maxCombo;
                args.mods = mods;
                args.n100 = n100;
                args.n50 = n50;
                args.nmiss = nmiss;
                args.mode = (uint)mode;

                if (!get_ppv2(Beatmap.RawData, (uint)pos, ref args, false, _realTimeData, ref _rtppResult))
                {
                    return pp_calc.Empty;
                }
            }

            return _rtppResult;
        }

        public void Clear()
        {
            _realTimeData = new pp_params();

            _pos = -1;
            _n100 = -1;
            _n50 = -1;
            _nmiss = -1;
            _maxCombo = -1;
            _rtppResult = pp_calc.Empty;

            _fcN100 = -1;
            _fcN50 = -1;
            _fcResult = pp_calc.Empty;

            _lastMods = c_unknown_mods;
            _maxResult = pp_calc.Empty;
        }

        public const Int32 FULL_COMBO = -1;

        #region oppai struct
        [StructLayout(LayoutKind.Sequential)]
        public class pp_params
        {
            /* required parameters */
            public double aim, speed;
            public float base_ar, base_od;
            public Int32 max_combo;
            public UInt16 nsliders; /* required for scorev1 only */
            public UInt16 ncircles; /* ^ */
            public UInt16 nobjects;

            /* optional parameters */
            public UInt32 mode; /* defaults to MODE_STD */
            public UInt32 mods; /* defaults to MODS_NOMOD */
            public Int32 combo; /* defaults to FC */
            public UInt16 n300, n100, n50; /* defaults to SS */
            public UInt16 nmiss; /* defaults to 0 */
            public UInt16 score_version; /* defaults to PP_DEFAULT_SCORING */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct pp_calc
        {
            /* ppv2 will store results here */
            public double total, aim, speed, acc;
            public double accuracy; /* 0.0 - 1.0 */
            public static pp_calc Empty;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct rtpp_params
        {
            public UInt32 mods;
            public int n50,n100,nmiss;
            public int combo;
            public UInt32 mode;
        }
        #endregion

        #region oppai P/Ivoke
        [DllImport(@"oppai.dll")]
        public static extern bool get_ppv2(byte[] data, UInt32 dataSize,ref rtpp_params args, Boolean useCache,pp_params cache,ref pp_calc result);
        #endregion

        #region oppai-ng function

        private static double round_oppai(double x)
        {
            return Math.Floor((x) + 0.5);
        }

        public static double acc_calc(int n300, int n100, int n50,int misses)
        {
            int totalHits = n300 + n100 + n50 + misses;
            double acc = 1.0;

            if (totalHits > 0)
            {
                acc = (
                    n50 * 50.0 + n100 * 100.0 + n300 * 300.0) /
                    (totalHits * 300.0);
            }

            return acc;
        }

        public static void acc_round(double accPercent, int nobjects,
            int misses, out int n300, out int n100,out int n50)
        {
            misses = Math.Min(nobjects, misses);
            var max300 = nobjects - misses;
            var maxacc = acc_calc(max300, 0, 0, misses) * 100.0;
            accPercent = Math.Max(0.0, Math.Min(maxacc, accPercent));

            n50 = 0;

            /* just some black magic maths from wolfram alpha */
            n100 = (int)round_oppai(-3.0 * ((accPercent * 0.01 - 1.0) *
            nobjects + misses) * 0.5);
            if (n100 > nobjects - misses)
            {
                /* acc lower than all 100s, use 50s */
                n100 = 0;
                n50 = (int)round_oppai(-6.0 * ((accPercent * 0.01 - 1.0) *nobjects + misses) * 0.2);

                n50 = Math.Min(max300, n50);
            }
            else
            {
                n100 = Math.Min(max300, n100);
            }

            n300 = nobjects - n100 - n50 - misses;
        }

        public static double taiko_acc_calc(int n300, int n150, int nmiss)
        {
            int totalHits = n300 + n150 + nmiss;
            double acc = 0;

            if (totalHits > 0)
            {
                acc = (n150 * 150.0 + n300 * 300.0) / (totalHits * 300.0);
            }

            return acc;
        }

        public static void taiko_acc_round(double accPercent, int nobjects, int nmisses, out int n300, out int n150)
        {
            nmisses = Math.Min(nobjects, nmisses);
            var max300 = nobjects - nmisses;
            var maxacc = acc_calc(max300, 0, 0, nmisses) * 100.0;
            accPercent = Math.Max(0.0, Math.Min(maxacc, accPercent));

            /* just some black magic maths from wolfram alpha */
            n150 = (int)
                round_oppai(-2.0 * ((accPercent * 0.01 - 1.0) *
                    nobjects + nmisses));

            n150 = Math.Min(max300, n150);
            n300 = nobjects - n150 - nmisses;
        }

        #endregion
    }
}
