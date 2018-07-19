using System;
using System.Collections.Generic;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Calculator;
using Sync.Tools;
using Sync.MessageFilter;
using Sync;
using Sync.Source;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using static OsuRTDataProvider.Mods.ModsInfo;

namespace RealTimePPDisplayer
{
    class DisplayerController
    {
        private OsuListenerManager m_listener_manager;
        private BeatmapReader m_beatmap_reader;

        private OsuPlayMode m_mode = OsuPlayMode.Osu;
        private PerformanceCalculatorBase m_tmp_last_pp_calculator = null;
        private PerformanceCalculatorBase m_pp_calculator=new StdPerformanceCalculator();
        private ModsInfo m_cur_mods = ModsInfo.Empty;

        private OsuStatus m_status;

        private Dictionary<string,DisplayerBase> m_displayers = new Dictionary<string,DisplayerBase>();

        private int m_combo = 0;
        private int m_max_combo = 0;

        private int m_n300 = 0;
        private int m_n100 = 0;
        private int m_n50 = 0;
        private int m_ngeki = 0;
        private int m_nkatu = 0;
        private int m_nmiss = 0;
        private int m_time = 0;
        private int m_score = 0;


        public DisplayerController(OsuListenerManager mamger,int? id)
        {
            m_listener_manager = mamger;

            m_listener_manager.OnCount300Changed += c => m_n300 = c;
            m_listener_manager.OnCountGekiChanged += c => m_ngeki = c;
            m_listener_manager.OnCountKatuChanged += c => m_nkatu = c;
            m_listener_manager.OnCount100Changed += c => m_n100 = c;
            m_listener_manager.OnCount50Changed += c => m_n50 = c;
            m_listener_manager.OnCountMissChanged += c => m_nmiss = c;
            m_listener_manager.OnScoreChanged += s => m_score = s;
            m_listener_manager.OnComboChanged += (combo) =>
            {
                if (m_status != OsuStatus.Playing) return;
                if(combo<= ((m_pp_calculator as OppaiPerformanceCalculator)?.Oppai.FullCombo??20000))
                {
                    m_combo = combo;
                    m_max_combo = Math.Max(m_max_combo, m_combo);
                }
            };

            m_listener_manager.OnModsChanged += (mods) =>
            {
                if (Setting.IgnoreTouchScreenDecrease)
                    mods.Mod = (mods.Mod & ~ModsInfo.Mods.TouchScreen);
                m_cur_mods = mods;
                if(m_status!=OsuStatus.Playing)
                    m_pp_calculator.ClearCache();
            };

            m_listener_manager.OnPlayModeChanged += RTPPOnPlayModeChanged;
            m_listener_manager.OnStatusChanged += RTPPOnStatusChanged;
            m_listener_manager.OnBeatmapChanged += RTPPOnBeatmapChanged;
            m_listener_manager.OnPlayingTimeChanged += RTPPOnPlayingTimeChanged;
        }

        #region RTPP Listener
        private void RTPPOnStatusChanged(OsuStatus last,OsuStatus cur)
        {
            m_status = cur;
            if ((cur == OsuStatus.Rank && last == OsuStatus.Playing))
            {
                var cal = m_tmp_last_pp_calculator??m_pp_calculator;

                var beatmap = cal.Beatmap.OrtdpBeatmap;
                var mods = cal.Mods;
                string songs = $"{beatmap.Artist} - {beatmap.Title}[{beatmap.Difficulty}]";
                string acc = $"{cal.Accuracy:F2}%";
                string mods_str = $"{(mods != Mods.None ? "+" + mods.ShortName : "")}";
                string pp = $"{cal.GetPerformance().RealTimePP:F2}pp";
                string msg = $"[RTPPD]{songs} {mods_str} | {acc} => {pp}";

                IO.CurrentIO.Write($"[RTPPD]{songs}{acc}{mods_str} -> {pp}");
                if (SyncHost.Instance.ClientWrapper.Client.CurrentStatus == SourceStatus.CONNECTED_WORKING &&
                    Setting.RankingSendPerformanceToChat)
                {
                    if (beatmap.BeatmapID != 0)
                    {
                        string dlUrl = beatmap.DownloadLink;
                        SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, $"[RTPPD][{dlUrl} {songs}] {mods_str} | {acc} => {pp}"));
                    }
                    else
                    {
                        SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, msg));
                    }
                }
            }

            if (cur != OsuStatus.Rank)
            {
                m_tmp_last_pp_calculator = null;
            }

            if (cur == OsuStatus.Listening || cur == OsuStatus.Editing)//Clear Output and reset
            {
                m_combo = 0;
                m_max_combo = 0;
                m_n100 = 0;
                m_n50 = 0;
                m_nmiss = 0;
                foreach (var p in m_displayers)
                    p.Value.Clear();
            }

            m_pp_calculator.ClearCache();
        }

        private void RTPPOnPlayModeChanged(OsuPlayMode last,OsuPlayMode mode)
        {
            if (m_status == OsuStatus.Playing)
                m_tmp_last_pp_calculator = m_pp_calculator;

            switch (mode)
            {
                case OsuPlayMode.Osu:
                    m_pp_calculator = new StdPerformanceCalculator(); break;
                case OsuPlayMode.Taiko:
                    m_pp_calculator = new TaikoPerformanceCalculator(); break;
                case OsuPlayMode.Mania:
                    m_pp_calculator = new ManiaPerformanceCalculator(); break;
                default:
                    Sync.Tools.IO.CurrentIO.WriteColor("[RealTimePPDisplayer]Unsupported Mode", ConsoleColor.Red);
                    m_pp_calculator = null; break;
            }
            m_mode = mode;
        }

        private void RTPPOnBeatmapChanged(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            string file = beatmap.FilenameFull;
            if (string.IsNullOrWhiteSpace(file))
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]No found .osu file(Set:{beatmap.BeatmapSetID} Beatmap:{beatmap.BeatmapID}])", ConsoleColor.Yellow);
                m_beatmap_reader = null;
                return;
            }

            if (Setting.DebugMode)
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]File:{file}", ConsoleColor.Blue);
            m_beatmap_reader = new BeatmapReader(beatmap,m_mode);
            m_pp_calculator.ClearCache();
        }

        private void RTPPOnPlayingTimeChanged(int time)
        {
            if (m_pp_calculator == null) return;
            if (m_status != OsuStatus.Playing) return;
            if (m_cur_mods == ModsInfo.Mods.Unknown) return;

            var cal = m_tmp_last_pp_calculator ?? m_pp_calculator;

            int totalhit = m_n300 + m_n100 + m_n50 + m_n50 + m_nkatu + m_ngeki + m_nmiss;
            if (time > cal.Beatmap?.BeatmapDuration &&
                totalhit == 0) return;

            if (m_time > time)//Reset
            {
                m_combo = 0;
                m_max_combo = 0;
                m_n100 = 0;
                m_n50 = 0;
                m_nmiss = 0;
                foreach (var p in m_displayers)
                    p.Value.Clear();
            }

            if (Setting.DebugMode && m_beatmap_reader == null)
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]Can't get beatmap information!", ConsoleColor.Yellow);
                return;
            }

            cal.Beatmap = m_beatmap_reader;
            cal.Time = time;
            cal.MaxCombo = m_max_combo;
            cal.Count300 = m_n300;
            cal.Count100 = m_n100;
            cal.Count50 = m_n50;
            cal.CountMiss = m_nmiss;
            cal.CountGeki = m_ngeki;
            cal.CountKatu= m_nkatu;
            cal.Score = m_score;
            cal.Mods = m_cur_mods;

            var pp_tuple = cal.GetPerformance();

            pp_tuple.RealTimePP = F(pp_tuple.RealTimePP, pp_tuple.MaxPP, 0.0);
            pp_tuple.RealTimeSpeedPP = F(pp_tuple.RealTimeSpeedPP, pp_tuple.MaxPP, 0.0);
            pp_tuple.RealTimeAimPP = F(pp_tuple.RealTimeAimPP, pp_tuple.MaxPP, 0.0);
            pp_tuple.RealTimeAccuracyPP = F(pp_tuple.RealTimeAccuracyPP, pp_tuple.MaxPP, 0.0);

            pp_tuple.RealTimePP = F(pp_tuple.RealTimePP, double.NaN, 0.0);
            pp_tuple.RealTimeSpeedPP = F(pp_tuple.RealTimeSpeedPP, double.NaN, 0.0);
            pp_tuple.RealTimeAimPP = F(pp_tuple.RealTimeAimPP, double.NaN, 0.0);
            pp_tuple.RealTimeAccuracyPP = F(pp_tuple.RealTimeAccuracyPP, double.NaN, 0.0);

            if (m_max_combo > ((cal as OppaiPerformanceCalculator)?.Oppai.FullCombo ?? 20000)) m_max_combo = 0;

            foreach(var p in m_displayers)
            {
                p.Value.OnUpdatePP(pp_tuple);
                if (Setting.DisplayHitObject)
                {
                    HitCountTuple hit_tuple;
                    hit_tuple.Count300 = m_n300;
                    hit_tuple.Count100 = m_n100;
                    hit_tuple.Count50 = m_n50;
                    hit_tuple.CountMiss = m_nmiss;
                    hit_tuple.Combo = m_combo;
                    hit_tuple.FullCombo = (cal as OppaiPerformanceCalculator)?.Oppai.FullCombo ?? 0;
                    hit_tuple.PlayerMaxCombo = m_max_combo;
                    hit_tuple.RealTimeMaxCombo = (cal as OppaiPerformanceCalculator)?.Oppai.RealTimeMaxCombo??0;
                    hit_tuple.CountGeki = m_ngeki;
                    hit_tuple.CountKatu = m_nkatu;
                    p.Value.OnUpdateHitCount(hit_tuple);
                }
                p.Value.Display();
            }

            m_time = time;
        }
        #endregion

        /// <summary>
        /// Add a displayer to update list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayer"></param>
        public void AddDisplayer(string name,DisplayerBase displayer)
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

        #region helper function
        private double F(double val, double max, double default_val)
        {
            return Math.Abs(val) > max ? 0.0 : val;
        }
        #endregion
    }



}
