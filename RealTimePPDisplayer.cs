using OsuRTDataProvider;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Linq;

namespace RealTimePPDisplayer
{
    [SyncRequirePlugin(typeof(OsuRTDataProviderPlugin))]
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";

        private OsuRTDataProvider.OsuRTDataProviderPlugin m_memory_reader;

        private PPControl[] m_osu_pp_displayers = new PPControl[16];

        public override void OnEnable()
        {
            I18n.Instance.ApplyLanguage(new DefaultLanguage());
            Setting.PluginInstance = this;

            m_memory_reader = getHoster().EnumPluings().Where(p => p.Name == "OsuRTDataProvider").FirstOrDefault() as OsuRTDataProviderPlugin;

            if (m_memory_reader.TourneyListenerManagers == null)
            {
                m_osu_pp_displayers[0] = new PPControl(m_memory_reader.ListenerManager, null);
            }
            else
            {
                for (int i = 0; i < m_memory_reader.TourneyListenerManagersCount; i++)
                {
                    m_osu_pp_displayers[i] = new PPControl(m_memory_reader.TourneyListenerManagers[i], i);
                }
            }
            Sync.Tools.IO.CurrentIO.WriteColor(PLUGIN_NAME + " By " + PLUGIN_AUTHOR, ConsoleColor.DarkCyan);
        }

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
        }
        
    }
}