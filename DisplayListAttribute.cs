using Sync.Tools.ConfigGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{
    class DisplayListAttribute:ListAttribute
    {
        public override string[] ValueList => RealTimePPDisplayerPlugin.Instance.DisplayerNames.ToArray();
    }
}
