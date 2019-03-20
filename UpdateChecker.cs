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
    static class UpdateChecker
    {
        private static readonly Regex NAME_REGEX = new Regex(@"""name"":\s*""v(\d+\.\d+\.\d+)(?:\((\d+\.\d+\.\d+)\))?""");

        private const string LATEST_RELEASE_URL = "https://api.github.com/repos/OsuSync/RealTimePPDisplayer/releases/latest";

        [DllImport("oppai.dll",EntryPoint = "oppai_version")]
        static extern void GetOppaiVersion(out int major, out int minor, out int patch);

        public static void CheckUpdate()
        {
            try
            {
                string data = GetHttpData(LATEST_RELEASE_URL);
                var groups = NAME_REGEX.Match(data).Groups;
                string rtpp_version = groups[1].Value;
                bool has_update = CheckRtppUpdate(rtpp_version);
                if (!string.IsNullOrEmpty(groups[2].Value))
                {
                    string oppai_version = groups[2].Value;
                    has_update = has_update || CheckOppaiUpdate(oppai_version);
                }

                if(has_update)
                {
                    Sync.Tools.IO.DefaultIO.WriteColor(DefaultLanguage.CHECK_GOTO_RELEASE_PAGE_HINT,ConsoleColor.Yellow);
                }
            }
            catch (Exception e)
            {
                Sync.Tools.IO.DefaultIO.WriteColor(e.ToString(), ConsoleColor.Red);
            }
        }

        private static bool CheckOppaiUpdate(string version)
        {
            Version ver = Version.Parse(version);
            GetOppaiVersion(out int major, out int minor, out int patch);
            Version selfVer = new Version(major, minor, patch);
            if (ver > selfVer)
            {
                Sync.Tools.IO.DefaultIO.WriteColor(
                    string.Format(DefaultLanguage.CHECK_OPPAI_UPDATE, ver),
                    ConsoleColor.Yellow);
                return true;
            }
            return false;
        }

        private static bool CheckRtppUpdate(string tag)
        {
            Version ver = Version.Parse(tag);
            Version selfVer = Version.Parse(RealTimePPDisplayerPlugin.VERSION);
            if (ver > selfVer)
            {
                Sync.Tools.IO.DefaultIO.WriteColor(
                    string.Format(DefaultLanguage.CHECK_RTPPD_UPDATE, ver),
                    ConsoleColor.Yellow);
                return true;
            }
            return false;
        }

        private static string GetHttpData(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            HttpWebRequest wReq = (HttpWebRequest)WebRequest.Create(url);
            wReq.UserAgent = "OsuSync";
            WebResponse wResp = wReq.GetResponse();
            Stream respStream = wResp.GetResponseStream();

            using (StreamReader reader = new StreamReader(respStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
