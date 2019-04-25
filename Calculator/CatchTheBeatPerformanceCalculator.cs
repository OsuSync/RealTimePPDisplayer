using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Utility;

namespace RealTimePPDisplayer.Calculator
{
    public sealed class CatchTheBeatPerformanceCalculator : PerformanceCalculatorBase
    {
        private const int KEEP_ALIVE = 0;
        private const int KEEP_ALIVE_OK = 1;
        private const int CALCULATE_CTB_PP = 2;

        private const int FULL_COMBO = int.MaxValue;
        private static Process s_ctbServer;
        public  static bool CtbServerRunning => !(s_ctbServer?.HasExited??true);

        private TcpClient _tcpClient;
        private Timer _timer;

        public int FullCombo { get; private set; }
        public int RealTimeMaxCombo { get; private set; }

        private double _stars = 0.0;
        private double _rt_stars = 0.0;

        public override double Stars => _stars;
        public override double RealTimeStars => _rt_stars;

        public class CtbServerResult
        {
            public double Stars { get; set; }
            public int FullCombo { get; set; }
            public double ApproachRate { get; set; }
        }

        #region static
        static CatchTheBeatPerformanceCalculator()
        {
            StartCtbServer();
        }

        private static void StartCtbServer()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ctb-server\pypy3\pypy3-rtpp.exe")))
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RTPPD::CTB]Please download ctb-server to the Sync root directory.", ConsoleColor.Red);
                return;
            }

            s_ctbServer = new Process();
            s_ctbServer.StartInfo.Arguments = @"/c .\run_ctb_server.bat";
            s_ctbServer.StartInfo.FileName = "cmd.exe";
            s_ctbServer.StartInfo.CreateNoWindow = true;
            s_ctbServer.StartInfo.UseShellExecute = false;
            s_ctbServer.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\ctb-server");
            s_ctbServer.Start();
        }

        private static void StopCtbServer()
        {
            foreach (var process in Process.GetProcessesByName("pypy3-rtpp"))
            {
                process.Kill();
            }
        }

        private static void RestartCtbServer()
        {
            StopCtbServer();
            StartCtbServer();
        }
        #endregion

        public CatchTheBeatPerformanceCalculator()
        {
            ConnectCtbServer();
        }

        private void ConnectCtbServer()
        {
            if (!CtbServerRunning)
            {
                RestartCtbServer();
                return;
            }

            _tcpClient = new TcpClient("127.0.0.1", 11800);
            _timer?.Dispose();
            _timer = new Timer((_) =>SendKeepAlive(),null,TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            //_tcpClient.ReceiveTimeout = 3 * 1000;
        }

        private void SendKeepAlive()
        {
            if (!CtbServerRunning)
            {
                RestartCtbServer();
                return;
            }

            try
            {
                lock (_tcpClient)
                {
                    using (var sw = new BinaryWriter(_tcpClient.GetStream(), Encoding.UTF8, true))
                    {
                        sw.Write(KEEP_ALIVE);
                    }

                    using (var sr = new BinaryReader(_tcpClient.GetStream(), Encoding.UTF8, true))
                    {
                        int cmd = sr.ReadInt32();
                        if (cmd != KEEP_ALIVE_OK)
                            throw new SocketException();
                    }
                }
            }
            catch (Exception)
            {
                Sync.Tools.IO.CurrentIO.WriteColor("[RTPPD::CTB]Reconnect ctb-server", ConsoleColor.Green);
                _tcpClient.Close();
                ConnectCtbServer();
            }

        }

        public CtbServerResult SendCalculateCtb(ArraySegment<byte> content, uint mods)
        {
            if (!CtbServerRunning)
            {
                RestartCtbServer();
                return new CtbServerResult();
            }

            if (content.Count == 0) return new CtbServerResult();

            try
            {
                lock (_tcpClient)
                {
                    var stream = _tcpClient.GetStream();
                    using (var sw = new BinaryWriter(stream, Encoding.UTF8, true))
                    {
                        sw.Write(CALCULATE_CTB_PP);
                        sw.Write(content.Count);
                        stream.Write(content.Array, content.Offset, content.Count);
                        sw.Write(mods); //mods
                    }

                    using (var br = new BinaryReader(stream, Encoding.UTF8, true))
                    {
                        var ret = new CtbServerResult();
                        ret.Stars = br.ReadDouble();
                        ret.FullCombo = br.ReadInt32();
                        ret.ApproachRate = br.ReadDouble();
                        return ret;
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Sync.Tools.IO.CurrentIO.WriteColor($"[RTPPD::CTB]:{e.Message}", ConsoleColor.Yellow);
#endif
                _tcpClient.Close();
                ConnectCtbServer();
                return null;
            }
        }

        /// <summary>
        /// Calculates the pp.
        /// </summary>
        /// <param name="serverResult">The server result.</param>
        /// <param name="ar">The ar.</param>
        /// <param name="mods">The mods.</param>
        /// <param name="acc">The acc (0-100).</param>
        /// <param name="maxCombo">The maximum combo.</param>
        /// <param name="nmiss">The nmiss.</param>
        /// <returns></returns>
        public static double CalculatePp(CtbServerResult serverResult, uint mods, double acc, int maxCombo, int nmiss)
        {
            acc /= 100.0;

            double pp = Math.Pow(((5 * serverResult.Stars / 0.0049) - 4), 2) / 100000;
            double length_bonus = 0.95 + 0.4 * Math.Min(1, maxCombo / 3000.0);
            if (maxCombo > 3000)
                length_bonus += Math.Log10(maxCombo / 3000.0) * 0.5;

            pp *= length_bonus;
            pp *= Math.Pow(0.97, nmiss);
            pp *= Math.Min(Math.Pow(maxCombo, 0.8) / Math.Pow(serverResult.FullCombo, 0.8), 1);

            if (serverResult.ApproachRate > 9)
                pp *= 1 + 0.1 * (serverResult.ApproachRate - 9);
            if (serverResult.ApproachRate < 8)
                pp *= 1 + 0.025 * (8 - serverResult.ApproachRate);

            if (mods.HasMod(ModsInfo.Mods.Hidden))
                pp *= 1.05 + 0.075 * (10 - Math.Min(10, serverResult.ApproachRate));
            if (mods.HasMod(ModsInfo.Mods.Flashlight))
                pp *= 1.35 * length_bonus;

            pp *= Math.Pow(acc, 5.5);

            if (mods.HasMod(ModsInfo.Mods.NoFail))
                pp *= 0.9;
            if (mods.HasMod(ModsInfo.Mods.SpunOut))
                pp *= 0.95;

            return pp;
        }

        private bool _cleared = true;
        private double _lastAcc = 0;
        private int _last_max_combo = 0;
        private int _last_nmiss = 0;
        private PPTuple _ppTuple = new PPTuple();
        private CtbServerResult _maxPpResult;

        public override PPTuple GetPerformance()
        {
            int pos = Beatmap.GetPosition(Time, out int nobject);

            if (_cleared == true)
            {
                _maxPpResult = SendCalculateCtb(new ArraySegment<byte>(Beatmap.RawData), Mods);
                if (_maxPpResult != null)
                {
                    _ppTuple.MaxPP = CalculatePp(_maxPpResult, Mods, 100, _maxPpResult.FullCombo, 0);
                    _ppTuple.MaxAccuracyPP = 0;
                    _ppTuple.MaxSpeedPP = 0;
                    _ppTuple.MaxAimPP = 0;

                    _stars = _maxPpResult.Stars;
                }

                FullCombo = _maxPpResult.FullCombo;

                _cleared = false;
            }

            if (_lastAcc != Accuracy)
            {

                if (_maxPpResult != null)
                {
                    double fcpp = CalculatePp(_maxPpResult, Mods, Accuracy, _maxPpResult.FullCombo, 0);
                    _ppTuple.FullComboPP = fcpp;
                    _ppTuple.FullComboAccuracyPP = 0;
                    _ppTuple.FullComboSpeedPP = 0;
                    _ppTuple.FullComboAimPP = 0;
                }
            }

            _lastAcc = Accuracy;

            bool needUpdate = _last_max_combo != MaxCombo;
            needUpdate |= _last_nmiss != CountMiss;


            if (needUpdate)
            {
                if (nobject > 0)
                {
                    CtbServerResult ctbServerResult;
                    ctbServerResult = SendCalculateCtb(new ArraySegment<byte>(Beatmap.RawData, 0, pos), Mods);
                    if (ctbServerResult != null)
                    {
                        _ppTuple.RealTimePP = CalculatePp(ctbServerResult, Mods, Accuracy, MaxCombo, CountMiss);
                        _ppTuple.RealTimeAccuracyPP = 0;
                        _ppTuple.RealTimeSpeedPP = 0;
                        _ppTuple.RealTimeAimPP = 0;
                        RealTimeMaxCombo = ctbServerResult.FullCombo;
                        _rt_stars = ctbServerResult.Stars;
                    }
                }
            }

            return _ppTuple;
        }

        public override void ClearCache()
        {
            base.ClearCache();
            _ppTuple = new PPTuple();
            _cleared = true;
            _lastAcc = 0;
            _last_max_combo = 0;
            _last_nmiss = 0;
        }

        public override double Accuracy
        {
            get
            {
                int total = Count50 + Count100 + Count300 + CountMiss + CountKatu;
                double acc = 1.0;
                if (total > 0)
                    acc = (double)(Count50 + Count100 + Count300) / total;
                return acc * 100;
            }
        }
    }
}
