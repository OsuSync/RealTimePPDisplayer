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
        private const uint UNKNOWN_MODS = 0xffffffffu;

        private IntPtr fc_handle = ezpp_new();
        private IntPtr rt_handle = ezpp_new();

        public int RealTimeMaxCombo => ezpp_max_combo(rt_handle);
        public int FullCombo => ezpp_max_combo(fc_handle);
        public double RealTimeStars => ezpp_stars(rt_handle);
        public double Stars => ezpp_stars(fc_handle);

        public BeatmapReader Beatmap { get; set; }

        private uint _lastMods = UNKNOWN_MODS;
        private pp_calc _maxResult;

        private static void CopyResultsFromHandle(IntPtr handle,ref pp_calc pp)
        {
            pp.total = ezpp_pp(handle);
            pp.aim = ezpp_aim_pp(handle);
            pp.speed = ezpp_speed_pp(handle);
            pp.acc = ezpp_acc_pp(handle);
            pp.accuracy = ezpp_accuracy_percent(handle) / 100.0f;
        }

        public pp_calc GetMaxPP(uint mods, int mode)
        {
            bool needUpdate = mods != _lastMods;

            if (needUpdate)
            {
                _lastMods = mods;

                ezpp_set_mods(fc_handle, (int)mods);
                ezpp_set_mode_override(fc_handle, mode);
                ezpp_set_accuracy(fc_handle, 0, 0);
                ezpp_set_nmiss(fc_handle, 0);
                ezpp_set_combo(fc_handle, FULL_COMBO);
                ezpp_data(fc_handle, Beatmap.RawData, Beatmap.RawData.Length);

                CopyResultsFromHandle(fc_handle,ref _maxResult);
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

                ezpp_set_mods(fc_handle, (int)mods);
                ezpp_set_mode_override(fc_handle, mode);
                ezpp_set_accuracy(fc_handle, n100, n50);
                ezpp_set_nmiss(fc_handle, 0);
                ezpp_set_combo(fc_handle, FULL_COMBO);
                ezpp_data(fc_handle, Beatmap.RawData, Beatmap.RawData.Length);

                CopyResultsFromHandle(fc_handle,ref _fcResult);
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

                ezpp_set_mods(rt_handle, (int)mods);
                ezpp_set_mode_override(rt_handle, mode);
                ezpp_set_accuracy(rt_handle, n100, n50);
                ezpp_set_nmiss(rt_handle, nmiss);
                ezpp_set_combo(rt_handle, maxCombo);

                if (nobject != 0)
                {
                    if (ezpp_data(rt_handle, Beatmap.RawData, pos) < 0)
                    {
                        return pp_calc.Empty;
                    }

                    CopyResultsFromHandle(rt_handle,ref _rtppResult);
                }
            }

            return _rtppResult;
        }

        public void Clear()
        {
            _pos = -1;
            _n100 = -1;
            _n50 = -1;
            _nmiss = -1;
            _maxCombo = -1;
            _rtppResult = pp_calc.Empty;

            _fcN100 = -1;
            _fcN50 = -1;
            _fcResult = pp_calc.Empty;

            _lastMods = UNKNOWN_MODS;
            _maxResult = pp_calc.Empty;

            ClearOppai(fc_handle);
            ClearOppai(rt_handle);
        }

        private static void ClearOppai(IntPtr handle)
        {
            // force map re-parse without reallocating handle	
            ezpp_set_base_cs(handle, -1);
            ezpp_set_base_ar(handle, -1);
            ezpp_set_base_od(handle, -1);
            ezpp_set_base_hp(handle, -1);
        }

        public const Int32 FULL_COMBO = -1;

        public struct pp_calc
        {
            /* ppv2 will store results here */
            public float total, aim, speed, acc;
            public float accuracy; /* 0.0f - 1.0f */
            public static pp_calc Empty;
        };

        #region oppai P/Invoke
        [DllImport(@"oppai.dll")] public static extern IntPtr ezpp_new();
        [DllImport(@"oppai.dll")] public static extern void ezpp_free(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern int ezpp_data(IntPtr handle, byte[] data, int data_size);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_base_cs(IntPtr handle, float cs);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_base_od(IntPtr handle, float cs);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_base_ar(IntPtr handle, float cs);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_base_hp(IntPtr handle, float cs);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_mods(IntPtr handle, int mods);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_accuracy(IntPtr handle, int n100, int n50);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_nmiss(IntPtr handle, int mods);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_combo(IntPtr handle, int combo);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_mode_override(IntPtr handle, int mode);
        [DllImport(@"oppai.dll")] public static extern void ezpp_set_end(IntPtr ez, int end);
        [DllImport(@"oppai.dll")] public static extern float ezpp_pp(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern float ezpp_aim_pp(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern float ezpp_speed_pp(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern float ezpp_acc_pp(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern float ezpp_accuracy_percent(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern int ezpp_combo(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern int ezpp_max_combo(IntPtr handle);
        [DllImport(@"oppai.dll")] public static extern float ezpp_stars(IntPtr handle);
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
