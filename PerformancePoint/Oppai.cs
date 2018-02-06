using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.PerformancePoint
{
    class Oppai
    {
        private pp_params m_real_time_data = new pp_params();
        private pp_params m_cache = new pp_params();

        public int RealTimeMaxCombo => m_real_time_data.max_combo;
        public int FullCombo => m_cache.max_combo;

        public BeatmapReader Beatmap;

        private int GetPosition(int end_time, out int nline)
        {
            int pos = Beatmap.BeatmapHeaderSpan.Length;
            nline = 0;
            foreach (var obj in Beatmap.ObjectList)
            {
                if (obj.StartTime > end_time) break;
                pos += (obj.Length);
                nline++;
            }

            return pos;
        }

        private ModsInfo _max_mods = ModsInfo.Empty;
        private pp_calc _max_result;

        public pp_calc GetMaxPP(ModsInfo mods, OsuPlayMode mode)
        {
            bool need_update = false;
            need_update = need_update || mods != _max_mods;

            if (need_update)
            {
                _max_mods = mods;

                Oppai.rtpp_params args;
                args.combo = Oppai.FULL_COMBO;
                args.mods = (uint)mods.Mod;
                args.n100 = 0;
                args.n50 = 0;
                args.nmiss = 0;
                args.mode = (uint)mode;

                //Cache Beatmap
                Oppai.get_ppv2(Beatmap.RawData, (uint)Beatmap.RawData.Length, ref args, false, m_cache, ref _max_result);
            }
            return _max_result;
        }

        private int _fc_n100 = -1;
        private int _fc_n50 = -1;
        private Oppai.pp_calc _fc_result;

        public Oppai.pp_calc GetIfFcPP(ModsInfo mods, int n300, int n100, int n50, OsuPlayMode mode)
        {
            bool need_update = false;
            need_update = need_update || _fc_n100 != n100;
            need_update = need_update || _fc_n50 != n50;


            if (need_update)
            {
                _fc_n100 = n100;
                _fc_n50 = n50;

                Oppai.rtpp_params args;
                args.combo = Oppai.FULL_COMBO;
                args.mods = (uint)mods.Mod;
                args.n100 = n100;
                args.n50 = n50;
                args.nmiss = 0;
                args.mode = (uint)mode;

                Oppai.get_ppv2(Beatmap.RawData, (uint)Beatmap.RawData.Length, ref args, true, m_cache, ref _fc_result);
            }

            return _fc_result;
        }

        private int _pos = -1;
        private int _n100 = -1;
        private int _n50 = -1;
        private int _nmiss = -1;
        private int _max_combo = -1;
        private Oppai.pp_calc _rtpp_result;

        public Oppai.pp_calc GetRealTimePP(int end_time, ModsInfo mods, int n100, int n50, int nmiss, int max_combo, OsuPlayMode mode)
        {
            int pos = GetPosition(end_time, out int nobject);

            bool need_update = false;
            need_update = need_update || _pos != pos;
            need_update = need_update || _n100 != n100;
            need_update = need_update || _n50 != n50;
            need_update = need_update || _nmiss != nmiss;
            need_update = need_update || _max_combo != max_combo;

            if (need_update)
            {
                _pos = pos;
                _n100 = n100;
                _n50 = n50;
                _nmiss = nmiss;
                _max_combo = max_combo;

                Oppai.rtpp_params args;
                args.combo = max_combo;
                args.mods = (uint)mods.Mod;
                args.n100 = n100;
                args.n50 = n50;
                args.nmiss = nmiss;
                args.mode = (uint)mode;

                if (!Oppai.get_ppv2(Beatmap.RawData, (uint)pos, ref args, false, m_real_time_data, ref _rtpp_result))
                {
                    return Oppai.pp_calc.Empty;
                }
            }

            return _rtpp_result;
        }

        public void Clear()
        {
            _pos = -1;
            _n100 = -1;
            _n50 = -1;
            _nmiss = -1;
            _max_combo = -1;
            _rtpp_result = Oppai.pp_calc.Empty;

            _fc_n100 = -1;
            _fc_n50 = -1;
            _fc_result = Oppai.pp_calc.Empty;

            _max_mods = ModsInfo.Empty;
            _max_result = Oppai.pp_calc.Empty;
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
        public extern static bool get_ppv2(byte[] data, UInt32 data_size,ref rtpp_params args, Boolean use_cache,pp_params cache,ref pp_calc result);
        #endregion

        #region oppai-ng function

        private static double round_oppai(double x)
        {
            return Math.Floor((x) + 0.5);
        }

        public static double acc_calc(int n300, int n100, int n50,int misses)
        {
            int total_hits = n300 + n100 + n50 + misses;
            double acc = 100.0;

            if (total_hits > 0)
            {
                acc = (
                    n50 * 50.0 + n100 * 100.0 + n300 * 300.0) /
                    (total_hits * 300.0);
            }

            return acc;
        }

        public static void acc_round(double acc_percent, int nobjects,
            int misses, out int n300, out int n100,out int n50)
        {
            int max300;
            double maxacc;

            misses = Math.Min(nobjects, misses);
            max300 = nobjects - misses;
            maxacc = acc_calc(max300, 0, 0, misses) * 100.0;
            acc_percent = Math.Max(0.0, Math.Min(maxacc, acc_percent));

            n50 = 0;

            /* just some black magic maths from wolfram alpha */
            n100 = (int)round_oppai(-3.0 * ((acc_percent * 0.01 - 1.0) *
            nobjects + misses) * 0.5);
            if (n100 > nobjects - misses)
            {
                /* acc lower than all 100s, use 50s */
                n100 = 0;
                n50 = (int)round_oppai(-6.0 * ((acc_percent * 0.01 - 1.0) *nobjects + misses) * 0.2);

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
            int total_hits = n300 + n150 + nmiss;
            double acc = 0;

            if (total_hits > 0)
            {
                acc = (n150 * 150.0 + n300 * 300.0) / (total_hits * 300.0);
            }

            return acc;
        }

        public static void taiko_acc_round(double acc_percent, int nobjects, int nmisses, out int n300, out int n150)
        {
            int max300;
            double maxacc;

            nmisses = Math.Min(nobjects, nmisses);
            max300 = nobjects - nmisses;
            maxacc = acc_calc(max300, 0, 0, nmisses) * 100.0;
            acc_percent = Math.Max(0.0, Math.Min(maxacc, acc_percent));

            /* just some black magic maths from wolfram alpha */
            n150 = (int)
                round_oppai(-2.0 * ((acc_percent * 0.01 - 1.0) *
                    nobjects + nmisses));

            n150 = Math.Min(max300, n150);
            n300 = nobjects - n150 - nmisses;
        }

        #endregion
    }
}
