using MemoryReader.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.View;
using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using static MemoryReader.Listen.OSUListenerManager;

namespace RealTimePPDisplayer
{
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";

        PPWindow m_win;
        Thread m_pp_window_thread;

        BeatmapReader m_beatmap_reader;
        ModsInfo m_cur_mods=new ModsInfo();
        double m_acc = 0.0;
        int m_combo = 0;
        int m_max_combo = 0;
        int m_n300 = 0;
        int m_n100=0;
        int m_n50=0;
        int m_nmiss=0;
        int m_time = 0;

        OsuStatus m_status;

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            base.EventBus.BindEvent<PluginEvents.InitPluginEvent>((e)=> Sync.Tools.IO.CurrentIO.WriteColor(PLUGIN_NAME + " By " + PLUGIN_AUTHOR, ConsoleColor.DarkCyan));
            base.EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(InitPlugin);
           // base.EventBus.BindEvent<PluginEvents. += StopPlugin;
        }

        private void InitPlugin(PluginEvents.LoadCompleteEvent e)
        {
            Setting.PluginInstance = this;

            foreach (var p in e.Host.EnumPluings())
            {
                if (p.Name == "MemoryReader")
                {
                    MemoryReader.MemoryReader reader = p as MemoryReader.MemoryReader;

                    reader.ListenerManager.OnCurrentMods += (mods) => m_cur_mods = mods;
                    reader.ListenerManager.OnAccuracyChanged += (acc) => m_acc = acc;
                    reader.ListenerManager.On300HitChanged += c => m_n300 = c;
                    reader.ListenerManager.On100HitChanged += c => m_n100 = c;
                    reader.ListenerManager.On50HitChanged += c => m_n50 = c;
                    reader.ListenerManager.OnMissHitChanged += c => m_nmiss = c;
                    reader.ListenerManager.OnStatusChanged += (last, cur) =>
                    {
                        m_status = cur;
                        if (last == OsuStatus.Playing && cur == OsuStatus.Listening)//换歌 重置变量
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
                                    m_win.pp_label.Content="";
                                    m_win.hit_label.Content = "";
                                });
                            }
                        }
                    };

                    reader.ListenerManager.OnComboChanged += (combo) =>
                    {
                        if (m_status != OsuStatus.Playing) return;
                        m_combo = combo;
                        m_max_combo = Math.Max(m_max_combo, m_combo);
                    };

                    reader.ListenerManager.OnBeatmapChanged += (beatmap) =>
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
                        Sync.Tools.IO.CurrentIO.Write($"map file:{file}");
                        m_beatmap_reader = new BeatmapReader(file);
                    };

                    reader.ListenerManager.OnPlayingTimeChanged += time =>
                    {
                        if (time < 0) return;
                        if (m_beatmap_reader == null) return;
                        if (m_status != OsuStatus.Playing) return;

                        if(m_time>time)//Retry 重置变量
                        {
                            m_max_combo = 0;
                            m_n100 = 0;
                            m_n50 = 0;
                            m_nmiss = 0;
                        }

                        var subb = m_beatmap_reader.SubBeatmap(time);
                        byte[] bytes = Encoding.ASCII.GetBytes(subb);

                        double pp = PP.Oppai.get_ppv2(bytes, (uint)bytes.Length, (uint)m_cur_mods.Mod, m_n50, m_n100, m_nmiss, m_max_combo);

                        if (pp > 5000.0) pp = double.NaN;
                        if (Setting.UseText)
                        {
                            string str = $"{pp:F2}pp";
                            if (Setting.DisplayHitObject)
                                str += $"\n{m_n100}x100 {m_n50}x50 {m_nmiss}xMiss";
                            File.WriteAllText(Setting.TextOutputPath, str);
                        }
                        else
                        {
                            m_win.Dispatcher.Invoke(() =>
                            {
                                m_win.pp_label.Content=$"{pp:F2}pp";
                                m_win.hit_label.Content = $"{m_n100}x100 {m_n50}x50 {m_nmiss}xMiss";
                            });
                        }
                        m_time = time;
                    };
                }
            }

            if (!Setting.UseText)
            {
                m_pp_window_thread = new Thread(ShowPPWindow);
                m_pp_window_thread.SetApartmentState(ApartmentState.STA);
                m_pp_window_thread.Start();
            }
        }

        private void StopPlugin()
        {
            Setting.WindowHeight = (int)m_win.Height;
            Setting.WindowWidth = (int)m_win.Width;

            m_win?.Dispatcher.Invoke(() => m_win?.Close());
        }

        private void ShowPPWindow()
        {
            m_win = new PPWindow();
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

            m_win.Width = Setting.WindowWidth;
            m_win.Height = Setting.WindowHeight;


            m_win.ShowDialog();
        }
    }
}
