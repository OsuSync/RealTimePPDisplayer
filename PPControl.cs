using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Listen;
using RealTimePPDisplayer.Displayer.View;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using System.Threading;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using System.IO;
using System.Windows.Media;
using System.Windows;
using RealTimePPDisplayer.Displayer;

namespace RealTimePPDisplayer
{
    class PPControl
    {
        private OsuListenerManager m_listener_manager;

        private BeatmapReader m_beatmap_reader;
        private ModsInfo m_cur_mods = ModsInfo.Empty;

        private OsuStatus m_status;

        private int m_combo = 0;
        private int m_max_combo = 0;

        private int m_n300 = 0;
        private int m_n100 = 0;
        private int m_n50 = 0;
        private int m_nmiss = 0;
        private int m_time = 0;

        private Dictionary<string,IDisplayer> m_displayers = new Dictionary<string,IDisplayer>();

        public PPControl(OsuListenerManager mamger,int? id)
        {
            m_listener_manager = mamger;

            m_listener_manager.OnModsChanged += (mods) => m_cur_mods = mods;
            m_listener_manager.On300HitChanged += (n300) => m_n300 = n300;
            m_listener_manager.On100HitChanged += c => m_n100 = c;
            m_listener_manager.On50HitChanged += c => m_n50 = c;
            m_listener_manager.OnMissHitChanged += c => m_nmiss = c;
            m_listener_manager.OnStatusChanged += (last, cur) =>
            {
                m_status = cur;
                if (cur == OsuStatus.Listening)//Reset(Change Song)
                {
                    m_combo = 0;
                    m_max_combo = 0;
                    m_n100 = 0;
                    m_n50 = 0;
                    m_nmiss = 0;
                    foreach (var p in m_displayers)
                        p.Value.Clear();
                }
            };

            m_listener_manager.OnComboChanged += (combo) =>
            {
                if (m_status != OsuStatus.Playing) return;
                //combo maybe wrong.(small probability).
                //jhlee0133's max kps is 70kps(7k).
                //so,10*2*10s=200.
                //10s is the assumed interval.
                if (combo - m_max_combo > 200) return;

                m_combo = combo;
                m_max_combo = Math.Max(m_max_combo, m_combo);
            };

            m_listener_manager.OnBeatmapChanged += RTPPOnBeatmapChanged;
            m_listener_manager.OnPlayingTimeChanged += RTPPOnPlayingTimeChanged;
        }

        private void RTPPOnBeatmapChanged(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            if (string.IsNullOrWhiteSpace(beatmap.Diff))
            {
                m_beatmap_reader = null;
                return;
            }

            string file = beatmap.LocationFile;
            if (string.IsNullOrWhiteSpace(file))
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]No found .osu file({beatmap.Set.Artist} - {beatmap.Set.Title}[{beatmap.Diff}])", ConsoleColor.Yellow);
                if (beatmap.Set.AllLocationPath != null)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]All beatmap folder(s)", ConsoleColor.Yellow);
                    int i = 0;
                    foreach (var folder in beatmap.Set.AllLocationPath)
                    {
                        Sync.Tools.IO.CurrentIO.WriteColor($"\t({i++}){folder}", ConsoleColor.Yellow);
                    }
                }
                m_beatmap_reader = null;
                return;
            }

            if (Setting.DebugMode)
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]File:{file}", ConsoleColor.Blue);
            m_beatmap_reader = new BeatmapReader(file);
        }
        private void RTPPOnPlayingTimeChanged(int time)
        {
            if (time < 0) return;
            if (m_beatmap_reader == null) return;
            if (m_status != OsuStatus.Playing) return;
            if (m_cur_mods == ModsInfo.Mods.Unknown) return;

            if (m_time > time)//Reset
            {
                m_combo = 0;
                m_max_combo = 0;
                m_n100 = 0;
                m_n50 = 0;
                m_nmiss = 0;
            }

            double if_fc_pp=0.0,pp=0.0,max_pp=0.0;
            foreach(var arg in StringFormatter.GetPPFormatter())
            {
                switch(arg)
                {
                    case "rtpp":
                        pp = m_beatmap_reader.GetCurrentPP(time, m_cur_mods, m_n100, m_n50, m_nmiss, m_max_combo);break;
                    case "if_fc_pp":
                        if_fc_pp = m_beatmap_reader.GetIfFcPP(m_cur_mods,m_n100,m_n50);break;
                    case "max_pp":
                        max_pp = m_beatmap_reader.GetMaxPP(m_cur_mods);break;
                }
            }
            
            if (double.IsNaN(pp)) pp = 0.0;
            if (pp > 100000.0) pp = 0.0;

            foreach(var p in m_displayers)
            {
                p.Value.OnUpdatePP(pp, if_fc_pp, max_pp);
                if (Setting.DisplayHitObject)
                    p.Value.OnUpdateHitCount(m_n300, m_n100, m_n50, m_nmiss, m_combo, m_max_combo);
                p.Value.Display();
            }

            m_time = time;
        }

        /// <summary>
        /// Add a displayer to update list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayer"></param>
        public void AddDisplayer(string name,IDisplayer displayer)
        {
            m_displayers[name]=displayer;
        }

        /// <summary>
        /// Remove a displayer from update list
        /// </summary>
        /// <param name="name"></param>
        public void RemoveDisplayer(string name)
        {
            if (m_displayers.ContainsKey(name))
            {
                m_displayers.Remove(name);
            }
        }
    }


    
}
