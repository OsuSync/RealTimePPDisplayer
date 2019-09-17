using PublicOsuBotTransfer;
using Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Warpper
{
    class PublicOsuBotTransferWarpper
    {
        private PublicOsuBotTransferPlugin s_pobt;
        public string Token => s_pobt.Token;
        public string Username => s_pobt.Username;

        public bool Init()
        {
            if (SyncHost.Instance.EnumPluings().FirstOrDefault(p => p.Name == "PublicOsuBotTransferPlugin") is PublicOsuBotTransferPlugin pobt)
            {
                if (pobt.GetType().Assembly.GetName().Version >= Version.Parse("1.3.0"))
                {
                    s_pobt = pobt;
                    return true;
                }
                else
                {
                    Sync.Tools.IO.DefaultIO.WriteColor(DefaultLanguage.HINT_POBT_VERSION_LOWER, ConsoleColor.Yellow);
                    return false;
                }
            }

            return false;
        }
    }
}
