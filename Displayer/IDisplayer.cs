using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    public interface IDisplayer
    {
        void OnEnable();
        void OnDisable();

        void OnUpdatePP(double cur_pp, double if_fc_pp, double max_pp);
        void OnUpdateHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo);
        void Display();
        void FixedDisplay(double time);
        void Clear();
    }
}
