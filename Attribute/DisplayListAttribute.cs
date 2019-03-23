using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Attribute
{
    class DisplayListAttribute:ListAttribute
    {
        public override string[] ValueList => RealTimePPDisplayerPlugin.Instance.DisplayerTypes.ToArray();
    }
}
