using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OsuRTDataProvider.Helper;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Calculator;
using RealTimePPDisplayer.Utility;
using Sync.MessageFilter;
using Sync;
using Sync.Source;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using static Sync.Tools.IO;

namespace RealTimePPDisplayer
{
    class DisplayerController
    {
        public Dictionary<string, DisplayerBase> Displayers { get; } = new Dictionary<string, DisplayerBase>();

        private BeatmapReader _beatmapReader;

        private readonly Mutex _playStatusLocker = new Mutex();
        private OsuPlayMode _mode = OsuPlayMode.Osu;
        private PerformanceCalculatorBase _stdPpCalculator;
        private PerformanceCalculatorBase _taikoPpCalculator;
        private PerformanceCalculatorBase _maniaPpCalculator;
        private PerformanceCalculatorBase _ctbPpCalculator;

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
        private ErrorStatisticsResult _error_statistics;
        private string _playername = string.Empty;


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
            listenerManager.OnErrorStatisticsChanged += es => _error_statistics = es;
            listenerManager.OnPlayerChanged += p => _playername = p;

            listenerManager.OnComboChanged += (combo) =>
            {
                if (_status != OsuStatus.Playing) return;
                int fullCombo = GetFullCombo(GetCalculator(_mode));
                if(combo <= (fullCombo == 0 ? 20000 : fullCombo))
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
                    GetCalculator(_mode).ClearCache();
            };

            listenerManager.OnPlayModeChanged += RtppOnPlayModeChanged;
            listenerManager.OnStatusChanged += RtppOnStatusChanged;
            listenerManager.OnBeatmapChanged += RtppOnBeatmapChanged;
            listenerManager.OnPlayingTimeChanged += RtppOnPlayingTimeChanged;
        }

        #region RTPP Listener
        private void RtppOnStatusChanged(OsuStatus last,OsuStatus cur)
        {
            if (cur == OsuStatus.Playing)
                _playStatusLocker.WaitOne();

            if (OsuStatusHelper.IsListening(cur)||
                cur == OsuStatus.NoFoundProcess)
            {
                try
                {
                    _playStatusLocker.ReleaseMutex();
                }
                catch (ApplicationException)
                {}
            }

            var cal = GetCalculator(_mode);

            if ((cur == OsuStatus.Rank && last == OsuStatus.Playing))
            {
                var beatmap = cal.Beatmap.OrtdpBeatmap;
                var mods = cal.Mods;
                string songs = $"{beatmap.Artist} - {beatmap.Title}[{beatmap.Difficulty}]";
                if (Setting.UseUnicodePerformanceInformation)
                    if(!string.IsNullOrEmpty(beatmap.ArtistUnicode) && !string.IsNullOrEmpty(beatmap.TitleUnicode))
                        songs = $"{beatmap.ArtistUnicode} - {beatmap.TitleUnicode}[{beatmap.Difficulty}]";
                string acc = $"{cal.Accuracy:F2}%";
                
                ModsInfo m = mods.ToModsInfo();
                string modsStr = $"{(m != ModsInfo.Mods.None ? "+" + m.ShortName : "")}";
                string pp = $"{cal.GetPerformance().RealTimePP:F2}pp";
                string msg = $"[RTPPD]{songs} {modsStr} | {acc} => {pp} ({_mode})";

                CurrentIO.Write(msg);
                if (SyncHost.Instance.ClientWrapper.Client.CurrentStatus == SourceStatus.CONNECTED_WORKING &&
                    Setting.RankingSendPerformanceToChat)
                {
                    if (beatmap.BeatmapID != 0)
                    {
                        string dlUrl = beatmap.DownloadLink;
                        SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, $"[RTPPD][{dlUrl} {songs}] {modsStr} | {acc} => {pp} ({_mode})"));
                    }
                    else
                    {
                        SyncHost.Instance.ClientWrapper.Client.SendMessage(new IRCMessage(SyncHost.Instance.ClientWrapper.Client.NickName, msg));
                    }
                }
            }

            cal.ClearCache();

            if (OsuStatusHelper.IsListening(cur) || cur == OsuStatus.Editing)//Clear output and reset
            {
                _combo = 0;
                _maxCombo = 0;
                _n100 = 0;
                _n50 = 0;
                _nmiss = 0;
                foreach (var p in Displayers)
                    p.Value?.Clear();
            }

            _status = cur;
        }

        private void RtppOnPlayModeChanged(OsuPlayMode last,OsuPlayMode mode)
        {
            Task.Run(() =>
            {
                bool @lock = _time > _beatmapReader?.BeatmapDuration / 4;
                if (@lock)
                    _playStatusLocker.WaitOne(5000);

                _mode = mode;

                try
                {
                    if (@lock)
                        _playStatusLocker.ReleaseMutex();
                }
                catch (ApplicationException)
                { }
            });

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
            _beatmapReader = new BeatmapReader(beatmap,(int)_mode);
            GetCalculator(_mode).ClearCache();
        }

        private void RtppOnPlayingTimeChanged(int time)
        {
            var cal = GetCalculator(_mode);
            if (cal == null) return;
            if (_status != OsuStatus.Playing) return;
            if (_curMods == ModsInfo.Mods.Unknown) return;

            int totalhit = _n300 + _n100 + _n50 + _nkatu + _ngeki + _nmiss;
            if (time > cal.Beatmap?.BeatmapDuration &&
                totalhit == 0) return;

            if (_time > time)//Reset
            {
                cal.ClearCache();
                _combo = 0;
                _maxCombo = 0;
                _n100 = 0;
                _n50 = 0;
                _nmiss = 0;
                _score = 0;
                foreach (var p in Displayers)
                    p.Value?.Clear();
            }

            if (_beatmapReader == null)
            {
                CurrentIO.WriteColor(DefaultLanguage.HINT_BEATMAP_NO_FOUND, ConsoleColor.Red);
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
            cal.Mods = (uint)_curMods.Mod;

            var ppTuple = cal.GetPerformance();

            ppTuple.RealTimePP = F(ppTuple.RealTimePP, ppTuple.MaxPP);
            ppTuple.RealTimeSpeedPP = F(ppTuple.RealTimeSpeedPP, ppTuple.MaxPP);
            ppTuple.RealTimeAimPP = F(ppTuple.RealTimeAimPP, ppTuple.MaxPP);
            ppTuple.RealTimeAccuracyPP = F(ppTuple.RealTimeAccuracyPP, ppTuple.MaxPP);

            ppTuple.RealTimePP = F(ppTuple.RealTimePP, double.NaN);
            ppTuple.RealTimeSpeedPP = F(ppTuple.RealTimeSpeedPP, double.NaN);
            ppTuple.RealTimeAimPP = F(ppTuple.RealTimeAimPP, double.NaN);
            ppTuple.RealTimeAccuracyPP = F(ppTuple.RealTimeAccuracyPP, double.NaN);

            int fullCombo = GetFullCombo(cal);
            int rtMaxCombo = GetRtMaxCombo(cal);

            HitCountTuple hitTuple;
            hitTuple.Count300 = _n300;
            hitTuple.Count100 = _n100;
            hitTuple.Count50 = _n50;
            hitTuple.CountMiss = _nmiss;
            hitTuple.Combo = _combo;
            hitTuple.FullCombo = fullCombo;
            hitTuple.PlayerMaxCombo = _maxCombo;
            hitTuple.CurrentMaxCombo = rtMaxCombo;
            hitTuple.CountGeki = _ngeki;
            hitTuple.CountKatu = _nkatu;
            hitTuple.ErrorStatistics = _error_statistics;

            int duration = cal.Beatmap?.BeatmapDuration??-1;
            int objectsCount = cal.Beatmap?.ObjectsCount??-1;

            BeatmapTuple beatmapTuple;
            beatmapTuple.Duration = duration;
            beatmapTuple.ObjectsCount = objectsCount;
            beatmapTuple.RealTimeStars = cal.RealTimeStars;
            beatmapTuple.Stars = cal.Stars;

            if (_maxCombo > (fullCombo == 0 ? 20000 : fullCombo)) _maxCombo = 0;

            foreach(var p in Displayers)
            {
                if (p.Value == null) continue;
                p.Value.Pp=ppTuple;
                p.Value.HitCount=hitTuple;
                p.Value.BeatmapTuple = beatmapTuple;
                p.Value.Playtime = time;
                p.Value.Mode = _mode;
                p.Value.Mods = _curMods;
                p.Value.Status = _status;
                p.Value.Playername = _playername;
                p.Value.Accuracy = cal.Accuracy;
                p.Value.Score = _score;
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
            Displayers[name]=displayer;
            displayer.OnReady();
        }

        /// <summary>
        /// Remove a displayer from update list
        /// </summary>
        /// <param name="name"></param>
        public void RemoveDisplayer(string name)
        {
            if (Displayers.ContainsKey(name))
            {
                Displayers[name].OnDestroy();
                Displayers.Remove(name);
            }
        }

        public void RemoveAllDisplayer()
        {
            foreach(var displayer in Displayers)
            {
                displayer.Value.OnDestroy();
            }

            Displayers.Clear();
        }

        #region helper function
        private double F(double val, double max)
        {
            return Math.Abs(val) > max ? 0.0 : val;
        }

        private int GetFullCombo(PerformanceCalculatorBase cal)
        {
            int fullCombo = 0;
            if (cal is OppaiPerformanceCalculator oppai)
            {
                fullCombo = oppai.Oppai.FullCombo;
            }
            else if (cal is CatchTheBeatPerformanceCalculator ctb)
            {
                fullCombo = ctb.FullCombo;
            }

            return fullCombo;
        }

        private int GetRtMaxCombo(PerformanceCalculatorBase cal)
        {
            int rtMaxCombo = 0;
            if (cal is OppaiPerformanceCalculator oppai)
            {
                rtMaxCombo = oppai.Oppai.RealTimeMaxCombo;
            }
            else if (cal is CatchTheBeatPerformanceCalculator ctb)
            {
                rtMaxCombo = ctb.RealTimeMaxCombo;
            }

            return rtMaxCombo;
        }

        private PerformanceCalculatorBase GetCalculator(OsuPlayMode mode)
        {
            switch (mode)
            {
                case OsuPlayMode.Osu:
                    _stdPpCalculator = _stdPpCalculator??new StdPerformanceCalculator();
                    return _stdPpCalculator;
                case OsuPlayMode.Taiko:
                    _taikoPpCalculator = _taikoPpCalculator??new TaikoPerformanceCalculator();
                    return _taikoPpCalculator;
                case OsuPlayMode.Mania:
                    _maniaPpCalculator = _maniaPpCalculator??new ManiaPerformanceCalculator();
                    return _maniaPpCalculator;
                case OsuPlayMode.CatchTheBeat:
                    _ctbPpCalculator = _ctbPpCalculator??new CatchTheBeatPerformanceCalculator();
                    return _ctbPpCalculator;
                default:
                    CurrentIO.WriteColor($"[RealTimePPDisplay]Unknown Mode! Mode:0x{(int)mode:X8}", ConsoleColor.Red);
                    return null;
            }
        }
        #endregion
    }
}
