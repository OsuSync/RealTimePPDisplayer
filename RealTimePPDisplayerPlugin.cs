using OsuRTDataProvider;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Gui;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{
    [SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Version = "^1.4.0", Require = true)]
    [SyncPluginID("8eb9e8e0-7bca-4a96-93f7-6408e76898a9", VERSION)]
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";
        public const string VERSION= "1.4.1";

        private OsuRTDataProviderPlugin m_memory_reader;
        private DisplayerController[] m_osu_pp_controls = new DisplayerController[16];

        private PluginConfigurationManager m_config_manager;

        public int TourneyWindowCount => m_memory_reader.TourneyListenerManagersCount;
        public bool TourneyMode => m_memory_reader.TourneyListenerManagers != null;

        #region FixedDisplay Field
        public static RealTimePPDisplayerPlugin Instance;
        public IEnumerable<string> DisplayerNames => m_displayer_creators.Select(d=>d.Key);

        private bool m_stop_fixed_update = false;
        private Dictionary<string, Func<int?, DisplayerBase>> m_displayer_creators = new Dictionary<string,Func<int?, DisplayerBase>>();
        private object m_all_displayer_mtx = new object();
        private LinkedList<KeyValuePair<string,DisplayerBase>> m_all_displayers = new LinkedList<KeyValuePair<string,DisplayerBase>>();
        private TimeSpan m_fixed_interval;

        private Task m_fixed_update_thread;
        #endregion

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            I18n.Instance.ApplyLanguage(new DefaultLanguage());
            base.EventBus.BindEvent<PluginEvents.InitCommandEvent>(InitCommand);

            Instance = this;
        }

        /// <summary>
        /// Plugin Init
        /// </summary>
        public override void OnEnable()
        {
            m_config_manager = new PluginConfigurationManager(this);
            m_config_manager.AddItem(new SettingIni());

            m_memory_reader = getHoster().EnumPluings().FirstOrDefault(p => p.Name == "OsuRTDataProvider") as OsuRTDataProviderPlugin;
            var gui = getHoster().EnumPluings().FirstOrDefault(p => p.Name == "ConfigGUI");

            if (gui != null)
            {
                GuiRegisterHelper.RegisterFormatEditorWindow(gui);
            }

            if (m_memory_reader == null)
            {
                Sync.Tools.IO.CurrentIO.WriteColor("No found OsuRTDataProvider!", ConsoleColor.Red);
                return;
            }

            int size = TourneyMode ? m_memory_reader.TourneyListenerManagersCount : 1;

            for (int i = 0; i < size; i++)
            {
                var manager = m_memory_reader.ListenerManager;
                int? id = null;
                if (TourneyMode)
                {
                    id = i;
                    manager = m_memory_reader.TourneyListenerManagers[i];
                }
                m_osu_pp_controls[i] = new DisplayerController(manager,id);
            }

            m_fixed_interval = TimeSpan.FromSeconds(1.0 / Setting.FPS);

            m_fixed_update_thread = Task.Run(() =>
            {
                while (!m_stop_fixed_update)
                {
                    lock (m_all_displayer_mtx)
                    {
                        foreach (var d in m_all_displayers)
                            d.Value.FixedDisplay(m_fixed_interval.TotalSeconds);
                    }
                    Thread.Sleep(m_fixed_interval);
                }
            });

            RegisterDisplayer("wpf", (id) => new WpfDisplayer(id));
            RegisterDisplayer("mmf", (id) => new MmfDisplayer(id));
            RegisterDisplayer("mmf-split", (id) => new MmfDisplayer(id,true));
            RegisterDisplayer("text", (id) => new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString())));
            RegisterDisplayer("text-split", (id) => new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString()),true));

            Sync.Tools.IO.CurrentIO.WriteColor(PLUGIN_NAME + " By " + PLUGIN_AUTHOR, ConsoleColor.DarkCyan);
        }

        #region Displayer operation
        /// <summary>
        /// Register Displayer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public bool RegisterDisplayer(string name,Func<int?,DisplayerBase> creator)
        {
            if(m_displayer_creators.ContainsKey(name))
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{name} Displayer exist!", ConsoleColor.Red);
                return false;
            }

            m_displayer_creators[name]=creator;

            if (Setting.OutputMethods.Contains(name))
                AddDisplayer(name, creator);

            return true;
        }

        private void AddDisplayer(string name,Func<int?, DisplayerBase> creator)
        {
            lock (m_all_displayer_mtx)
            {
                foreach (var p in m_all_displayers)
                    if (p.Key == name) return;

                int size = TourneyMode ? m_memory_reader.TourneyListenerManagersCount : 1;

                for (int i = 0; i < size; i++)
                {
                    int? id = null;
                    if (TourneyMode) id = i;

                    var displayer = creator(id);
                    m_osu_pp_controls[i].AddDisplayer(name, displayer);
                    m_all_displayers.AddLast(new KeyValuePair<string, DisplayerBase>(name, displayer));
                }
            }
        }

        private void RemoveDisplayer(string name)
        {
            lock (m_all_displayer_mtx)
            {
                for (var node = m_all_displayers.First; node != null;)
                {
                    if (node.Value.Key == name)
                    {
                        int size = TourneyMode ? m_memory_reader.TourneyListenerManagersCount : 1;
                        for (int i = 0; i < size; i++)
                        {
                            m_osu_pp_controls[i].RemoveDisplayer(name);

                        }
                        node.Value.Value.OnDestroy();
                        var nnode = node.Next;
                        m_all_displayers.Remove(node);
                        node = nnode;
                        continue;
                    }
                    node = node.Next;
                }
            }
        }

        private void RemoveAllDisplayer()
        {
            foreach (var p in m_all_displayers)
            {
                p.Value.Clear();
                p.Value.OnDestroy();
            }
            m_all_displayers.Clear();
        }
        #endregion

        private void InitCommand(PluginEvents.InitCommandEvent @e)
        {
            @e.Commands.Dispatch.bind("rtpp", (args) =>
            {
                if(args.Count>=2)
                {
                    switch(args[0])
                    {
                        case "add":
                            if (!m_displayer_creators.ContainsKey(args[1])) return false;
                            var creator = m_displayer_creators[args[1]];
                            AddDisplayer(args[1], creator);
                            break;

                        case "remove":
                            if (!m_displayer_creators.ContainsKey(args[1])) return false;
                            RemoveDisplayer(args[1]);

                            break;
                    }
                    return true;
                }
                return false;
            }, "Real Time PP Displayer control panel");
        }

        public override void OnDisable()
        {
            m_stop_fixed_update = true;
            m_fixed_update_thread.Wait(5000);
            RemoveAllDisplayer();
            m_displayer_creators.Clear();
            for (int i = 0; i < m_osu_pp_controls.Length; i++)
                m_osu_pp_controls[i] = null;
        }

        public override void OnExit()
        {
            OnDisable();
        }
    }
}