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


        private static FormatterBase s_hitCountFormatter;
        private static FormatterBase s_ppFormatter;

        public FormatterBase()
        {
        }

        public static FormatterBase GetPPFormatter()
        {
            if (s_ppFormatter == null)
            {
                s_ppFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.PPFormat);
                Setting.OnFormatterChanged += ()=> s_ppFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.PPFormat);
                Setting.OnPPFormatChanged += () => s_ppFormatter.Format = Setting.PPFormat;
            }
            var t = s_ppFormatter;
            return t;
        }

        public static FormatterBase GetHitCountFormatter()
        {
            if (s_hitCountFormatter == null)
            {
                s_hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter,Setting.HitCountFormat);
                Setting.OnFormatterChanged += () => s_hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.HitCountFormat);
                Setting.OnHitCountFormatChanged += () => s_hitCountFormatter.Format = Setting.HitCountFormat;
            }
            var t = s_hitCountFormatter;
            return t;
        }
    }
}
