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
        [DllImport(@"oppai.dll")]
        public extern static double get_ppv2(byte[] data, UInt32 data_size,UInt32 mods,int n50,int n100,int nmiss,int combo);

        [DllImport(@"oppai.dll")]
        public extern static double get_ppv2_acc(byte[] data, UInt32 data_size, UInt32 mods, double acc, int nmiss, int combo);
    }
}
