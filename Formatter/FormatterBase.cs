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
        public HitCountTuple HitCount { get; set; } = new HitCountTuple();
        public PPTuple Pp { get; set; } = new PPTuple();
        public BeatmapTuple BeatmapTuple { get; set; } = new BeatmapTuple();
        public double Playtime { get; set; }
        public OsuStatus Status { get; set; }
        public OsuPlayMode Mode { get; set; }
        public ModsInfo Mods { get; set; }

        public abstract string Format { get; set; }
        public abstract string GetFormattedString();


        private static FormatterBase s_hitCountFormatter;
        private static FormatterBase s_ppFormatter;
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
                s_hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter);
                Setting.OnFormatterChanged += () => s_hitCountFormatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(Setting.Formatter, Setting.HitCountFormat);
                Setting.OnPPFormatChanged += () => s_hitCountFormatter.Format = Setting.HitCountFormat;
            }
            var t = s_hitCountFormatter;
            return t;
        }
    }
}
