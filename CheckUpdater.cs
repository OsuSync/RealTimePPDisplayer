using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{
    static class CheckUpdater
    {
        private const string RtppVersionURL = "https://raw.githubusercontent.com/OsuSync/RealTimePPDisplayer/master/RealTimePPDisplayerPlugin.cs";
        private const string OppaiVersionURL = "https://raw.githubusercontent.com/OsuSync/RealTimePPDisplayer/master/oppai_version";

        [DllImport("oppai.dll",EntryPoint = "oppai_version")]
        static extern void GetOppaiVersion(out int major, out int minor, out int patch);

        public static void CheckOppaiUpdate()
        {
            try
            {
                string version = GetHttpData(OppaiVersionURL);
                Version ver = Version.Parse(version);
                GetOppaiVersion(out int major, out int minor, out int patch);
                Version selfVer = new Version(major, minor, patch);
                if (ver > selfVer)
                {
                    Sync.Tools.IO.DefaultIO.WriteColor(
                        string.Format(DefaultLanguage.CHECK_OPPAI_UPDATE, ver),
                        ConsoleColor.Yellow);
                }
            }
            catch (Exception e)
            {
                Sync.Tools.IO.DefaultIO.WriteColor(e.ToString(), ConsoleColor.Red);
            }
        }

        public static void CheckRtppUpdate()
        {
            try
            {
                string data = GetHttpData(RtppVersionURL);
                Regex regex = new Regex(@"string\s*VERSION\s*=\s*""(\d+\.\d+\.\d+)""");
                string version = regex.Match(data).Groups[1].Value;
                Version ver = Version.Parse(version);
                Version selfVer = Version.Parse(RealTimePPDisplayerPlugin.VERSION);
                if (ver > selfVer)
                {
                    Sync.Tools.IO.DefaultIO.WriteColor(
                        string.Format(DefaultLanguage.CHECK_RTPPD_UPDATE, ver),
                        ConsoleColor.Yellow);
                }
            }
            catch (Exception e)
            {
                Sync.Tools.IO.DefaultIO.WriteColor(e.ToString(),ConsoleColor.Red);
            }
        }

        private static string GetHttpData(string url)
        {
            WebRequest wReq = WebRequest.Create(url);
            WebResponse wResp = wReq.GetResponse();
            Stream respStream = wResp.GetResponseStream();
            // Dim reader As StreamReader = New StreamReader(respStream)
            using (StreamReader reader = new StreamReader(respStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
