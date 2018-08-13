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

namespace RealTimePPDisplayer.Calculator
{
    public class CatchTheBeatPerformanceCalculator:PerformanceCalculatorBase
    {
        private const int c_keepServerRun = 0;
        private const int c_getPp = 1;
        private const int c_fullCombo = int.MaxValue;
        private static Timer _timer;
        private static Process _ctbServer;
        public static bool CtbServerRunning => !_ctbServer.HasExited;

        public int FullCombo { get; private set; }
        public int RealTimeMaxCombo { get; private set; }

        class CtbPp
        {
            public double Stars { get; set; }
            public double Pp { get; set; }
            public int FullCombo { get; set; }
        }

        static CatchTheBeatPerformanceCalculator()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ctb-server\pypy3\pypy3-rtpp.exe")))
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RTPPD::CTB]Please download ctb-server to the Sync root directory.",ConsoleColor.Red);
                return;
            }

            StartCtbServer();

            _timer = new Timer((_) => SendKeepServerRun(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private static void StartCtbServer()
        {
            _ctbServer = new Process();
            _ctbServer.StartInfo.Arguments = @"/c .\run_ctb_server.bat";
            _ctbServer.StartInfo.FileName = "cmd.exe";
            _ctbServer.StartInfo.CreateNoWindow = true;
            _ctbServer.StartInfo.UseShellExecute = false;
            _ctbServer.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\ctb-server");
            _ctbServer.Start();
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

        private static void SendKeepServerRun()
        {
            if (!CtbServerRunning)
            {
                RestartCtbServer();
                return;
            }

            try
            {
                using (TcpClient client = new TcpClient("127.0.0.1", 11800))
                {
                    using (var sw = new BinaryWriter(client.GetStream()))
                    {
                        sw.Write(c_keepServerRun);
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("[RTPPD::CTB]Restart ctb-server");
                RestartCtbServer();
            }

        }

        private static CtbPp SendGetPp(ArraySegment<byte> content,ModsInfo mods,int maxCombo,int nmiss,double acc)
        {
            if (!CtbServerRunning)
            {
                RestartCtbServer();
                return new CtbPp();
            }

            if(content.Count==0)return new CtbPp();
            acc /= 100; // from 0~100 to 0~1

            try
            {
                using (TcpClient client = new TcpClient("127.0.0.1", 11800))
                {
                    var stream = client.GetStream();
                    using (var sw = new BinaryWriter(stream, Encoding.UTF8, true))
                    {
                        sw.Write(c_getPp);
                        sw.Write(content.Count);
                        stream.Write(content.Array, content.Offset, content.Count);
                        sw.Write((int) mods.Mod); //mods
                        sw.Write(maxCombo); //max_combo
                        sw.Write(nmiss); //miss
                        sw.Write(acc); //acc(0-1)
                    }

                    using (var br = new BinaryReader(stream))
                    {
                        var ret = new CtbPp();
                        ret.Stars = br.ReadDouble();
                        ret.Pp = br.ReadDouble();
                        ret.FullCombo = br.ReadInt32();

                        return ret;
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Sync.Tools.IO.CurrentIO.WriteColor($"[RTPPD::CTB]:{e.Message}",ConsoleColor.Yellow);
#endif
                return null;
            }
        }

        private bool _cleared = true;
        private double _last_acc = 0;
        private int _last_max_combo = 0;
        private int _last_nmiss = 0;
        private PPTuple _ppTuple = new PPTuple();

        public override PPTuple GetPerformance()
        {
            int pos = Beatmap.GetPosition(Time, out int nobject);
            
            CtbPp ctbPp;


            if (_cleared == true)
            {
                ctbPp = SendGetPp(new ArraySegment<byte>(Beatmap.RawData), Mods, c_fullCombo, 0, 100);
                if (ctbPp != null)
                {
                    _ppTuple.MaxPP = ctbPp.Pp;
                    _ppTuple.MaxAccuracyPP = 0;
                    _ppTuple.MaxSpeedPP = 0;
                    _ppTuple.MaxAimPP = 0;
                }

                FullCombo = ctbPp.FullCombo;

                _cleared = false;
            }

            if (_last_acc != Accuracy)
            {
                ctbPp = SendGetPp(new ArraySegment<byte>(Beatmap.RawData), Mods, c_fullCombo, 0, Accuracy);
                if (ctbPp != null)
                {
                    _ppTuple.FullComboPP = ctbPp.Pp;
                    _ppTuple.FullComboAccuracyPP = 0;
                    _ppTuple.FullComboSpeedPP = 0;
                    _ppTuple.FullComboAimPP = 0;
                }
            }

            _last_acc = Accuracy;

            bool needUpdate = _last_max_combo!=MaxCombo;
            needUpdate |= _last_nmiss != CountMiss;


            if (needUpdate)
            {
                if (nobject > 0)
                {
                    ctbPp = SendGetPp(new ArraySegment<byte>(Beatmap.RawData, 0, pos), Mods, MaxCombo, CountMiss,
                        Accuracy);
                    if (ctbPp != null)
                    {
                        _ppTuple.RealTimePP = ctbPp.Pp;
                        _ppTuple.RealTimeAccuracyPP = 0;
                        _ppTuple.RealTimeSpeedPP = 0;
                        _ppTuple.RealTimeAimPP = 0;
                        RealTimeMaxCombo = ctbPp.FullCombo;
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
            _last_acc = 0;
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
                    acc = (double) (Count50 + Count100 + Count300) / total;
                return acc * 100;
            }
        }
    }
}
