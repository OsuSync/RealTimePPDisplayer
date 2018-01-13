using OsuRTDataProvider;
using RealTimePPDisplayer.Displayer;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Linq;

namespace RealTimePPDisplayer
{
    [SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Version = "^1.1.1", Require = true)]
    [SyncPluginID("8eb9e8e0-7bca-4a96-93f7-6408e76898a9", VERSION)]
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";
        public const string VERSION= "1.1.2";

        private OsuRTDataProviderPlugin m_memory_reader;
        private PPControl[] m_osu_pp_displayers = new PPControl[16];

        public int TourneyWindowSize => m_memory_reader.TourneyListenerManagersCount;
        public bool TourneyMode => m_memory_reader.TourneyListenerManagers != null;

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR){}

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
        }

        public bool RegisterDisplayer(string name,IDisplayer displayer)
        {
            return m_osu_pp_displayers[0].RegisterDisplayer(name, displayer);
        }

        public bool RegisterTourneyDisplayer(int id,string name, IDisplayer displayer)
        {
            return m_osu_pp_displayers[id].RegisterDisplayer(name, displayer);
        }
    }
}