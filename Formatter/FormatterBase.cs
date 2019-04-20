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
    }
}
