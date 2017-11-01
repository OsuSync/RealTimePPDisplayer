using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryReader.Listen;
using RealTimePPDisplayer.View;
using MemoryReader.Mods;
using RealTimePPDisplayer.Beatmap;
using System.Threading;
using static MemoryReader.Listen.OSUListenerManager;
using System.IO;
using System.Windows.Media;

namespace RealTimePPDisplayer
{
    class PPDisplayer
    {
        private static int s_counter = 0;

        private int _id = 0;

        private OSUListenerManager m_listener_manager;

        private PPWindow m_win;
        private Thread m_pp_window_thread;

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

        string _filename = Path.GetFileNameWithoutExtension(Setting.TextOutputPath);
        string _ext = Path.GetExtension(Setting.TextOutputPath);

        public PPDisplayer(OSUListenerManager mamger)
        {
            m_listener_manager = mamger;

            m_listener_manager.OnCurrentMods += (mods) => m_cur_mods = mods;
            m_listener_manager.On300HitChanged += c => m_n300 = c;
            m_listener_manager.On100HitChanged += c => m_n100 = c;
            m_listener_manager.On50HitChanged += c => m_n50 = c;
            m_listener_manager.OnMissHitChanged += c => m_nmiss = c;
            m_listener_manager.OnStatusChanged += (last, cur) =>
            {
                m_status = cur;
                if (cur == OsuStatus.Listening)//换歌 重置变量
                {
                    m_max_combo = 0;
                    m_n100 = 0;
                    m_n50 = 0;
                    m_nmiss = 0;
                    if (Setting.UseText)
                    {
                        string str = "";
                        if (Setting.DisplayHitObject)
                            str += "";
                        File.WriteAllText(Setting.TextOutputPath, str);
                    }
                    else
                    {
                        m_win.Dispatcher.Invoke(() =>
                        {
                            m_win.pp_label.Content = "";
                            m_win.hit_label.Content = "";
                        });
                    }
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

                if (m_time > time)//Retry 重置变量
                {
                    m_max_combo = 0;
                    m_n100 = 0;
                    m_n50 = 0;
                    m_nmiss = 0;
                }

                var subb = m_beatmap_reader.SubBeatmap(time);
                byte[] bytes = Encoding.ASCII.GetBytes(subb);

                double pp = PP.Oppai.get_ppv2(bytes, (uint)bytes.Length, (uint)m_cur_mods.Mod, m_n50, m_n100, m_nmiss, m_max_combo);

                if (pp > 10000000.0) pp = 0.0;

                if (Setting.UseText)
                {
                    string str = $"{pp:F2}pp";
                    if (Setting.DisplayHitObject)
                        str += $"\n{m_n100}x100 {m_n50}x50 {m_nmiss}xMiss";

                    File.WriteAllText($"{_filename}-0{_ext}", str);
                }
                else
                {
                    m_win?.Dispatcher.Invoke(() =>
                    {
                        m_win.pp_label.Content = $"{pp:F2}pp";
                        m_win.hit_label.Content = $"{m_n100}x100 {m_n50}x50 {m_nmiss}xMiss";
                    });
                }
                m_time = time;
            };

            if (!Setting.UseText)
            {
                m_pp_window_thread = new Thread(ShowPPWindow);
                m_pp_window_thread.SetApartmentState(ApartmentState.STA);
                m_pp_window_thread.Start();
            }

            _id=s_counter++;
        }

        private void ShowPPWindow()
        {
            m_win = new PPWindow();
            m_win.Width = Setting.WindowWidth;
            m_win.Height = Setting.WindowHeight;

            m_win.Title += $"-{_id}";

            m_win.SizeChanged += (o, e) =>
            {
                Setting.WindowHeight = (int)e.NewSize.Height;
                Setting.WindowWidth = (int)e.NewSize.Width;
            };

            if (!Setting.DisplayHitObject)
                m_win.hit_label.Visibility = System.Windows.Visibility.Hidden;

            m_win.pp_label.Foreground = new SolidColorBrush()
            {
                Color = Setting.PPFontColor
            };
            m_win.pp_label.FontSize = Setting.PPFontSize;

            m_win.hit_label.Foreground = new SolidColorBrush()
            {
                Color = Setting.HitObjectFontColor
            };
            m_win.hit_label.FontSize = Setting.HitObjectFontSize;

            m_win.Background = new SolidColorBrush()
            {
                Color = Setting.BackgroundColor
            };

            m_win.ShowDialog();
        }
    }
}
