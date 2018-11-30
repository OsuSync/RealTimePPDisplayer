using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Attribute
{
    class CustomColorAttribute : ColorAttribute
    {
        public override bool Check(string color)
        {
            if (base.Check(color))
                return true;
            if (color.Length == 8
                && byte.TryParse(color.Substring(0, 2), NumberStyles.HexNumber, null, out var _)
                && byte.TryParse(color.Substring(2, 2), NumberStyles.HexNumber, null, out var _)
                && byte.TryParse(color.Substring(4, 2), NumberStyles.HexNumber, null, out var _)
                && byte.TryParse(color.Substring(6, 2), NumberStyles.HexNumber, null, out var _))
                return true;
            return false;
        }
    }
}
