using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    interface IDisplayer
    {
        void Display(double pp, int n300, int n100, int n50, int nmiss);
        void Clear();
    }
}
