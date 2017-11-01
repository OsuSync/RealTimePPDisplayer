using Sync.Plugins;
using System;

namespace RealTimePPDisplayer
{
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";

        private MemoryReader.MemoryReader m_memory_reader;

        private PPDisplayer[] m_osu_pp_displayers = new PPDisplayer[16];

        public override void OnEnable()
        {
            base.OnEnable();
            Sync.Tools.IO.CurrentIO.WriteColor(PLUGIN_NAME + " By " + PLUGIN_AUTHOR, ConsoleColor.DarkCyan);
        }

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            base.EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(InitPlugin);
        }

        private void InitPlugin(PluginEvents.LoadCompleteEvent e)
        {
            Setting.PluginInstance = this;

            foreach (var p in e.Host.EnumPluings())
            {
                if (p.Name == "MemoryReader")
                {
                    m_memory_reader = p as MemoryReader.MemoryReader;
                    break;
                }
            }

            if (m_memory_reader.TourneyListenerManagers == null)
            {
                m_osu_pp_displayers[0] = new PPDisplayer(m_memory_reader.ListenerManager);
            }
            else
            {
                for (int i=0; i < m_memory_reader.TourneyListenerManagersCount;i++)
                {
                    m_osu_pp_displayers[i] = new PPDisplayer(m_memory_reader.ListenerManager);
                }
            }
        }
    }
}