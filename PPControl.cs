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
        private ModsInfo m_cur_mods = new ModsInfo();

        private OsuStatus m_status;

        private int m_combo = 0;
        private int m_max_combo = 0;
        private int m_n300 = 0;
        private int m_n100 = 0;
        private int m_n50 = 0;
        private int m_nmiss = 0;
        private int m_time = 0;

        List<IDisplayer> m_displayers = new List<IDisplayer>();

        public PPControl(OsuListenerManager mamger,int? id)
        {
            m_listener_manager = mamger;
            if (Setting.UseText||Setting.OutputMethods.Contains("text"))
                m_displayers.Add(new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString())));
            if (Setting.OutputMethods.Contains("wpf"))
                m_displayers.Add(new WpfDisplayer(id));

            m_listener_manager.OnModsChanged += (mods) => m_cur_mods = mods;
            m_listener_manager.On300HitChanged += c => m_n300 = c;
            m_listener_manager.On100HitChanged += c => m_n100 = c;
            m_listener_manager.On50HitChanged += c => m_n50 = c;
            m_listener_manager.OnMissHitChanged += c => m_nmiss = c;
            m_listener_manager.OnStatusChanged += (last, cur) =>
            {
                m_status = cur;
                if (cur == OsuStatus.Listening)//Reset(Change Song)
                {
                    m_max_combo = 0;
                    m_n100 = 0;
                    m_n50 = 0;
                    m_nmiss = 0;
                    m_displayers.ForEach(d=>d.Clear());
                }
            };

            m_listener_manager.OnComboChanged += (combo) =>
            {
                if (m_status != OsuStatus.Playing) return;
                m_combo = combo;
                m_max_combo = Math.Max(m_max_combo, m_combo);
            };

            m_listener_manager.OnBeatmapChanged += (beatmap) =>
            {
                if (string.IsNullOrWhiteSpace(beatmap.Diff))
                {
                    m_beatmap_reader = null;
                    return;
                }

                string file = beatmap.LocationFile;
                if (string.IsNullOrWhiteSpace(file))
                {
                    Sync.Tools.IO.CurrentIO.Write("[RealTimePPDisplayer]No found .osu file");
                    m_beatmap_reader = null;
                    return;
                }
#if DEBUG
                Sync.Tools.IO.CurrentIO.Write($"[RealTimePPDisplayer]File:{file}");
#endif
                m_beatmap_reader = new BeatmapReader(file);
            };

            m_listener_manager.OnPlayingTimeChanged += time =>
            {
                if (time < 0) return;
                if (m_beatmap_reader == null) return;
                if (m_status != OsuStatus.Playing) return;
                if (m_cur_mods == ModsInfo.Mods.Unknown) return;

                if (m_time > time)//Reset
                {
                    m_max_combo = 0;
                    m_n100 = 0;
                    m_n50 = 0;
                    m_nmiss = 0;
                }

                int pos = m_beatmap_reader.GetPosition(time);
                
                double pp = PP.Oppai.get_ppv2(m_beatmap_reader.BeatmapRaw, (uint)pos, (uint)m_cur_mods.Mod, m_n50, m_n100, m_nmiss, m_max_combo);

                if (pp > 100000.0) pp = 0.0;

                m_displayers.ForEach(d=>d.Display(pp,m_n300,m_n100,m_n50,m_nmiss));

                m_time = time;
            };
        }
    }
}
