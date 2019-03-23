using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Expression;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace RealTimePPDisplayer.Displayer
{
    public struct BeatmapTuple
    {
        public int ObjectsCount;
        public double Duration;
    }

    public struct PPTuple
    {
        public static readonly PPTuple Empty;

        public double RealTimePP;
        public double RealTimeAimPP;
        public double RealTimeSpeedPP;
        public double RealTimeAccuracyPP;

        public double FullComboPP;
        public double FullComboAimPP;
        public double FullComboSpeedPP;
        public double FullComboAccuracyPP;

        public double MaxPP;
        public double MaxAimPP;
        public double MaxSpeedPP;
        public double MaxAccuracyPP;
    }

    public struct HitCountTuple
    {
        public int Count300;
        public int Count100;
        public int Count50;
        public int CountGeki;
        public int CountKatu;
        public int CountMiss;

        public int Combo;
        public int PlayerMaxCombo;
        public int FullCombo;
        public int CurrentMaxCombo;
    }

    public class DisplayerBase
    {
        public HitCountTuple HitCount { get; set; } = new HitCountTuple();
        public PPTuple Pp { get; set; } = new PPTuple();
        public BeatmapTuple BeatmapTuple { get; set; } = new BeatmapTuple();
        public double Playtime { get; set; }
        public OsuStatus Status { get; set; }
        public OsuPlayMode Mode { get; set; }
        public ModsInfo Mods { get; set; }

        /// <summary>
        /// Clear Output
        /// </summary>
        public virtual void Clear()
        {
            HitCount = new HitCountTuple();
            Pp = new PPTuple();
        }

        /// <summary>
        /// Displayer(ORTDP Thread[call interval=ORTDP.IntervalTime])
        /// </summary>
        public virtual void Display() { }

        /// <summary>
        /// Displayer(call interval = 1000/Setting.FPS)
        /// </summary>
        /// <param name="time">1000/Setting.FPS</param>
        public virtual void FixedDisplay(double time) { }

        /// <summary>
        /// Called when the Displayer is destroyed.
        /// </summary>
        public virtual void OnDestroy() { }

        public virtual void OnReady() { }

        public void SetFormatterArgs(StringFormatterBase fmt)
        {
            fmt.HitCount = HitCount;
            fmt.Pp = Pp;
            fmt.BeatmapTuple = BeatmapTuple;
            fmt.Playtime = Playtime;
            fmt.Mode = Mode;
            fmt.Mods = Mods;
            fmt.Status = Status;
        }

        [Obsolete]
        public StringFormatter FormatPp(PPTuple? pp=null)
        {
            var formatter = StringFormatter.GetPPFormatter();
            SetFormatterArgs(formatter);
            formatter.Pp = pp ?? Pp;

            return formatter;
        }

        [Obsolete]
        public StringFormatter FormatHitCount(HitCountTuple? hitCount=null)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            SetFormatterArgs(formatter);
            formatter.HitCount = hitCount??HitCount;

            return formatter;
        }
    }
}
