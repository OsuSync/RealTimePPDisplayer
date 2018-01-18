using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.PP
{
    public static class Oppai
    {
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
        };

        public const Int32 FullCombo = -1; 

        [DllImport(@"oppai.dll")]
        public extern static double get_ppv2(byte[] data, UInt32 data_size,
            UInt32 mods,Int32 n50, Int32 n100, Int32 nmiss, Int32 combo,Boolean use_cache,pp_params @params);
    }
}
