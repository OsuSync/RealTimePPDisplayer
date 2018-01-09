using OsuRTDataProvider;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Linq;

namespace RealTimePPDisplayer
{
    [SyncPluginDependency("18d8a3eb-d8d7-4c6c-9d8b-901b9957d6b0", Version = "^1.1.0", Require = true)]
    [SyncPluginID("58390cf7-b61e-4800-b7a3-ba909eb8ab25", VERSION)]
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";
        public const string VERSION= "1.1.0";

        private OsuRTDataProvider.OsuRTDataProviderPlugin m_memory_reader;

        private PPControl[] m_osu_pp_displayers = new PPControl[16];

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
    }
}