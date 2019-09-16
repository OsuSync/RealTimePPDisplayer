using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Displayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace RealTimePPDisplayer.Formatter
{
    public abstract class FormatterBase
    {
        public DisplayerBase Displayer { get; set; }

        public abstract string Format { get; set; }
        public abstract string GetFormattedString();


        public FormatterBase()
        {
        }

        public static FormatterBase GetPPFormatter()
        {
            var ppFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.PPFormat);
            Setting.OnFormatterChanged += ()=> ppFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.PPFormat);
            Setting.OnPPFormatChanged += () => ppFormatter.Format = Setting.PPFormat;

            return ppFormatter;
        }

        public static FormatterBase GetHitCountFormatter()
        {
            var hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter,Setting.HitCountFormat);
            Setting.OnFormatterChanged += () => hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.HitCountFormat);
            Setting.OnHitCountFormatChanged += () => hitCountFormatter.Format = Setting.HitCountFormat;

            return hitCountFormatter;
        }
    }
}
