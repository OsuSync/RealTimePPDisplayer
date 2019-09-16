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
            Setting.OnPPFormatChanged += () => {
                var ppForamter_saved = ppFormatter;
                ppForamter_saved.Format = Setting.PPFormat;
            };

            return ppFormatter;
        }

        public static FormatterBase GetHitCountFormatter()
        {
            var hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter,Setting.HitCountFormat);
            Setting.OnHitCountFormatChanged += () =>
            {
                var hitCountFormatter_saved = hitCountFormatter;
                hitCountFormatter_saved.Format = Setting.HitCountFormat;
            };

            return hitCountFormatter;
        }
    }
}
