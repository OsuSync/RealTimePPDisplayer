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

        private double m_acc = 0.0;

        private int m_n300 = 0;
        private int m_n100 = 0;
        private int m_n50 = 0;
        private int m_nmiss = 0;
        private int m_time = 0;

        private List<IDisplayer> m_displayers = new List<IDisplayer>();

        private static bool s_stop_fixed_update=false;
        private static List<IDisplayer> s_all_displayers = new List<IDisplayer>();
        private static int s_fixed_interval = 33;
        private static double s_fixed_interval_s = 0.033;

        private static Task s_fixed_update_thread;

        static PPControl()
        {
            s_fixed_interval = 1000 / Setting.FPS;
            s_fixed_interval_s = 1.0 / Setting.FPS;

            s_fixed_update_thread = Task.Run(() =>
            {
                while(!s_stop_fixed_update)
                {
                    s_all_displayers.ForEach(d => d.FixedDisplay(s_fixed_interval_s));
                    Thread.Sleep(s_fixed_interval);
                }
            });
        }

        public PPControl(OsuListenerManager mamger,int? id)
        {
            m_listener_manager = mamger;

            RegisterDisplayer("wpf", ()=>new WpfDisplayer(id));
            RegisterDisplayer("mmf", ()=>new MmfDisplayer(id));
            RegisterDisplayer("text", ()=>new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString())));

            m_listener_manager.OnAccuracyChanged += (acc) => m_acc = acc;
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
                    m_displayers.ForEach(d => d.Clear());
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
                        pp = m_beatmap_reader.GetCurrentPP(time, m_cur_mods, m_n50, m_n100, m_nmiss, m_max_combo);break;
                    case "if_fc_pp":
                        if_fc_pp = m_beatmap_reader.GetIfFcPP(m_cur_mods, m_acc);break;
                    case "max_pp":
                        max_pp = m_beatmap_reader.GetMaxPP(m_cur_mods);break;
                }
            }
            
            if (double.IsNaN(pp)) pp = 0.0;
            if (pp > 100000.0) pp = 0.0;

            m_displayers.ForEach(d => {
                d.OnUpdatePP(pp, if_fc_pp, max_pp);
                if(Setting.DisplayHitObject)
                    d.OnUpdateHitCount(m_n300, m_n100, m_n50, m_nmiss, m_combo, m_max_combo);
                d.Display();
            });

            m_time = time;
        }

        public bool RegisterDisplayer<T>(string name,Func<T> creator)where T:IDisplayer
        {
            if (Setting.OutputMethods.Contains(name))
            {
                var displayer = creator();
                m_displayers.Add(displayer);
                s_all_displayers.Add(displayer);
                return true;
            }
            return false;
        }
    }


    
}
