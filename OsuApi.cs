using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Warpper;
using Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{

    public class BeatPerformance
    {
        public int BeatmapID;
        public string ScoreID;
        public int Score;
        public int MaxCombo;
        public int Count50;
        public int Count100;
        public int Count300;
        public int CountMiss;
        public int CountKatu;
        public int CountGeki;

        public bool Perfect;
        public ModsInfo EnabledMods;
        public string UserID;
        public DateTime Date;
        public string Rank;
        public double PP;
    }

    static class OsuApi
    {
        public static PublicOsuBotTransferWarpper publicOsuBotTransferWarpper;

        public static List<BeatPerformance> GetBp(string player,OsuPlayMode mode)
        {
            HttpWebRequest req;
            if(Setting.ByCuteSyncProxy)
            {
                if (publicOsuBotTransferWarpper == null)
                {
                    publicOsuBotTransferWarpper = new PublicOsuBotTransferWarpper();
                    if (!publicOsuBotTransferWarpper.Init())
                        return null;
                }
                if(publicOsuBotTransferWarpper.Username != player)
                {
                    Sync.Tools.IO.DefaultIO.WriteColor(DefaultLanguage.HINT_CANNOT_WATCH_OTHER_PLAYER, ConsoleColor.Yellow);
                    return null;
                }

                if (string.IsNullOrEmpty(publicOsuBotTransferWarpper.Token))
                    return null;

                req = (HttpWebRequest)WebRequest.Create($"https://osubot.kedamaovo.moe/osuapi/bp?k={publicOsuBotTransferWarpper.Token}&u={player}&type=string&limit=100&m={(uint)mode}");
            }
            else
            {
                req = (HttpWebRequest)WebRequest.Create($"https://osu.ppy.sh/api/get_user_best?k={Setting.ApiKey}&u={player}&type=string&limit=100&m={(uint)mode}");
            }

            req.Timeout = 5000;
            List<BeatPerformance> result = new List<BeatPerformance>();
            Stream stream = null;
            try
            {
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                stream = resp.GetResponseStream();
                using (StreamReader sr = new StreamReader(stream))
                {
                    var json = sr.ReadToEnd();
                    var objs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string,string>>>(json);
                    foreach(var obj in objs)
                    {
                        BeatPerformance bp = new BeatPerformance();
                        bp.BeatmapID = int.Parse(obj["beatmap_id"]);
                        bp.Score = int.Parse(obj["score"]);
                        bp.ScoreID = obj["score_id"];
                        bp.MaxCombo = int.Parse(obj["maxcombo"]);
                        bp.Count50 = int.Parse(obj["count50"]);
                        bp.Count100 = int.Parse(obj["count100"]);
                        bp.Count300 = int.Parse(obj["count300"]);
                        bp.CountMiss = int.Parse(obj["countmiss"]);
                        bp.CountKatu = int.Parse(obj["countkatu"]);
                        bp.CountGeki = int.Parse(obj["countgeki"]);
                        bp.Perfect = obj["perfect"] == "1";
                        Enum.TryParse<ModsInfo.Mods>(obj["enabled_mods"],out var mods);
                        bp.EnabledMods = new ModsInfo() { Mod = mods };

                        bp.UserID = obj["user_id"];
                        bp.Date = DateTime.Parse(obj["date"]);
                        bp.Rank = obj["rank"];
                        bp.PP = double.Parse(obj["pp"]);
    
                        result.Add(bp);
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                stream?.Close();
            }
            
            result.Sort((a, b) => b.PP.CompareTo(a.PP));
            return result;
        }
    }
}
