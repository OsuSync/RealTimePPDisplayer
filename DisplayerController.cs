using System;
using System.Collections.Generic;
using OsuRTDataProvider.Helper;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Calculator;
using Sync.MessageFilter;
using Sync;
using Sync.Source;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using static Sync.Tools.IO;

namespace RealTimePPDisplayer
{
    class DisplayerController
    {
        private readonly Dictionary<string,DisplayerBase> _displayers = new Dictionary<string,DisplayerBase>();

        private BeatmapReader _beatmapReader;

        private OsuPlayMode _mode = OsuPlayMode.Osu;
        private PerformanceCalculatorBase _tmpLastPpCalculator;
        private PerformanceCalculatorBase _ppCalculator=new StdPerformanceCalculator();
        private ModsInfo _curMods = ModsInfo.Empty;
        private OsuStatus _status;

        private int _combo;
        private int _maxCombo;

        private int _n300;
        private int _n100;
        private int _n50;
        private int _ngeki;
        private int _nkatu;
        private int _nmiss;
        private int _time;
        private int _score;


        public DisplayerController(OsuListenerManager mamger)
        {
            var listenerManager = mamger;

            listenerManager.OnCount300Changed += c => _n300 = c;
            listenerManager.OnCountGekiChanged += c => _ngeki = c;
            listenerManager.OnCountKatuChanged += c => _nkatu = c;
            listenerManager.OnCount100Changed += c => _n100 = c;
            listenerManager.OnCount50Changed += c => _n50 = c;
            listenerManager.OnCountMissChanged += c => _nmiss = c;
            listenerManager.OnScoreChanged += s => _score = s;
            listenerManager.OnComboChanged += (combo) =>
            {
                if (_status != OsuStatus.Playing) return;
                if(combo<= ((_ppCalculator as OppaiPerformanceCalculator)?.Oppai.FullCombo??20000))
                {
                    _combo = combo;
                    _maxCombo = Math.Max(_maxCombo, _combo);
                }
            };

            listenerManager.OnModsChanged += (mods) =>
            {
                if (Setting.IgnoreTouchScreenDecrease)
                    mods.Mod = (mods.Mod & ~ModsInfo.Mods.TouchDevice);
                _curMods = mods;
                if(_status!=OsuStatus.Playing)
                    _ppCalculator.ClearCache();
            };

            listenerManager.OnPlayModeChanged += RtppOnPlayModeChanged;
            listenerManager.OnStatusChanged += RtppOnStatusChanged;
            listenerManager.OnBeatmapChanged += RtppOnBeatmapChanged;
            listenerManager.OnPlayingTimeChanged += RtppOnPlayingTimeChanged;
        }

        #region RTPP Listener
        private void RtppOnStatusChanged(OsuStatus last,OsuStatus cur)
        {
            _status = cur;
            if ((cur == OsuStatus.Rank && last == OsuStatus.Playing))
            {
                var cal = _tmpLastPpCalculator??_ppCalculator;

                var beatmap = cal.Beatmap.OrtdpBeatmap;
                var mods = cal.Mods;
                string songs = $"{beatmap.Artist} - {beatmap.Title}[{beatmap.Difficulty}]";
                string acc = $"{cal.Accuracy:F2}%";
                string modsStr = $"{(mods != ModsInfo.Mods.None ? "+" + mods.ShortName : "")}";
                string pp = $"{cal.GetPerformance().RealTimePP:F2}pp";
                string msg = $"[RTPPD]{songs} {modsStr} | {acc} => {pp}";

                CurrentIO.Write($"[RTPPD]{songs}{acc}{modsStr} -> {pp}");
                if (SyncHost.Instance.ClientWrapper.Client.CurrentStatus == SourceStatus.CONNECTED_WORKING &&
                    Setting.RankingSendPerformanceToChat)
                {
                    if (beatmap.BeatmapID != 0)
                    {
                        string dlUrl = beatmap.DownloadLink;
                        SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, $"[RTPPD][{dlUrl} {songs}] {modsStr} | {acc} => {pp}"));
                    }
                    else
                    {
                        SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, msg));
                    }
                }
            }

            if (cur != OsuStatus.Rank)
            {
                _tmpLastPpCalculator = null;
            }

            if (OsuStatusHelper.IsListening(cur) || cur == OsuStatus.Editing)//Clear Output and reset
            {
                _combo = 0;
                _maxCombo = 0;
                _n100 = 0;
                _n50 = 0;
                _nmiss = 0;
                foreach (var p in _displayers)
                    p.Value.Clear();
            }

            _ppCalculator.ClearCache();
        }

        private void RtppOnPlayModeChanged(OsuPlayMode last,OsuPlayMode mode)
        {
            if (_status == OsuStatus.Playing)
                _tmpLastPpCalculator = _ppCalculator;

            switch (mode)
            {
                case OsuPlayMode.Osu:
                    _ppCalculator = new StdPerformanceCalculator(); break;
                case OsuPlayMode.Taiko:
                    _ppCalculator = new TaikoPerformanceCalculator(); break;
                case OsuPlayMode.Mania:
                    _ppCalculator = new ManiaPerformanceCalculator(); break;
                default:
                    CurrentIO.WriteColor("[RealTimePPDisplayer]Unsupported Mode", ConsoleColor.Red);
                    _ppCalculator = null; break;
            }
            _mode = mode;
        }

        private void RtppOnBeatmapChanged(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            string file = beatmap.FilenameFull;
            if (string.IsNullOrWhiteSpace(file))
            {
                CurrentIO.WriteColor($"[RealTimePPDisplayer]No found .osu file(Set:{beatmap.BeatmapSetID} Beatmap:{beatmap.BeatmapID}])", ConsoleColor.Yellow);
                _beatmapReader = null;
                return;
            }

            if (Setting.DebugMode)
                CurrentIO.WriteColor($"[RealTimePPDisplayer]File:{file}", ConsoleColor.Blue);
            _beatmapReader = new BeatmapReader(beatmap,_mode);
            _ppCalculator.ClearCache();
        }

        private void RtppOnPlayingTimeChanged(int time)
        {
            if (_ppCalculator == null) return;
            if (_status != OsuStatus.Playing) return;
            if (_curMods == ModsInfo.Mods.Unknown) return;

            var cal = _tmpLastPpCalculator ?? _ppCalculator;

            int totalhit = _n300 + _n100 + _n50 + _n50 + _nkatu + _ngeki + _nmiss;
            if (time > cal.Beatmap?.BeatmapDuration &&
                totalhit == 0) return;

            if (_time > time)//Reset
            {
                _ppCalculator.ClearCache();
                _combo = 0;
                _maxCombo = 0;
                _n100 = 0;
                _n50 = 0;
                _nmiss = 0;
                foreach (var p in _displayers)
                    p.Value.Clear();
            }

            if (Setting.DebugMode && _beatmapReader == null)
            {
                CurrentIO.WriteColor("[RealTimePPDisplayer]Can\'t get beatmap information!", ConsoleColor.Yellow);
                return;
            }

            cal.Beatmap = _beatmapReader;
            cal.Time = time;
            cal.MaxCombo = _maxCombo;
            cal.Count300 = _n300;
            cal.Count100 = _n100;
            cal.Count50 = _n50;
            cal.CountMiss = _nmiss;
            cal.CountGeki = _ngeki;
            cal.CountKatu= _nkatu;
            cal.Score = _score;
            cal.Mods = _curMods;

            var ppTuple = cal.GetPerformance();

            ppTuple.RealTimePP = F(ppTuple.RealTimePP, ppTuple.MaxPP);
            ppTuple.RealTimeSpeedPP = F(ppTuple.RealTimeSpeedPP, ppTuple.MaxPP);
            ppTuple.RealTimeAimPP = F(ppTuple.RealTimeAimPP, ppTuple.MaxPP);
            ppTuple.RealTimeAccuracyPP = F(ppTuple.RealTimeAccuracyPP, ppTuple.MaxPP);

            ppTuple.RealTimePP = F(ppTuple.RealTimePP, double.NaN);
            ppTuple.RealTimeSpeedPP = F(ppTuple.RealTimeSpeedPP, double.NaN);
            ppTuple.RealTimeAimPP = F(ppTuple.RealTimeAimPP, double.NaN);
            ppTuple.RealTimeAccuracyPP = F(ppTuple.RealTimeAccuracyPP, double.NaN);

            if (_maxCombo > ((cal as OppaiPerformanceCalculator)?.Oppai.FullCombo ?? 20000)) _maxCombo = 0;

            foreach(var p in _displayers)
            {
                p.Value.OnUpdatePP(ppTuple);
                if (Setting.DisplayHitObject)
                {
                    HitCountTuple hitTuple;
                    hitTuple.Count300 = _n300;
                    hitTuple.Count100 = _n100;
                    hitTuple.Count50 = _n50;
                    hitTuple.CountMiss = _nmiss;
                    hitTuple.Combo = _combo;
                    hitTuple.FullCombo = (cal as OppaiPerformanceCalculator)?.Oppai.FullCombo ?? 0;
                    hitTuple.PlayerMaxCombo = _maxCombo;
                    hitTuple.CurrentMaxCombo = (cal as OppaiPerformanceCalculator)?.Oppai.RealTimeMaxCombo??0;
                    hitTuple.CountGeki = _ngeki;
                    hitTuple.CountKatu = _nkatu;
                    hitTuple.ObjectsCount = cal.Beatmap.ObjectsCount;
                    hitTuple.PlayTime = time;
                    hitTuple.Duration = cal.Beatmap.BeatmapDuration;
                    p.Value.OnUpdateHitCount(hitTuple);
                }
                p.Value.Display();
            }

            _time = time;
        }
        #endregion

        /// <summary>
        /// Add a displayer to update list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayer"></param>
        public void AddDisplayer(string name,DisplayerBase displayer)
        {
            _displayers[name]=displayer;
        }

        /// <summary>
        /// Remove a displayer from update list
        /// </summary>
        /// <param name="name"></param>
        public void RemoveDisplayer(string name)
        {
            if (_displayers.ContainsKey(name))
            {
                _displayers.Remove(name);
            }
        }

        #region helper function
        private double F(double val, double max)
        {
            return Math.Abs(val) > max ? 0.0 : val;
        }
        #endregion
    }



}
