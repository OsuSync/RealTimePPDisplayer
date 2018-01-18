using OsuRTDataProvider;
using OsuRTDataProvider.Handler;
using RealTimePPDisplayer.Displayer;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private PPControl[] m_osu_pp_controls = new PPControl[16];
        
        public int TourneyWindowSize => m_memory_reader.TourneyListenerManagersCount;
        public bool TourneyMode => m_memory_reader.TourneyListenerManagers != null;

        #region FixedDisplay
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
        }

        /// <summary>
        /// Plugin Init
        /// </summary>
        public override void OnEnable()
        {
            Setting.PluginInstance = this;

            m_memory_reader = getHoster().EnumPluings().Where(p => p.Name == "OsuRTDataProvider").FirstOrDefault() as OsuRTDataProviderPlugin;

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
                m_osu_pp_controls[i] = new PPControl(manager,id);
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
            RegisterDisplayer("text", (id) => new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString())));

            InitDisplayer();

            ExitHandler.OnConsloeExit += OnExit;
        }

        private void InitDisplayer()
        {
            foreach (var creator_pair in m_displayer_creators)
            {
                var name = creator_pair.Key;
                var creator = creator_pair.Value;

                if (!Setting.OutputMethods.Contains(name)) continue;

                AddDisplayer(name, creator);
            }
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
        #endregion

        private void ModifySetting(string name,string val)
        {
            switch(name)
            {
                case "SmoothTime":
                    if(int.TryParse(val,out int ival))
                        Setting.SmoothTime = ival;
                    break;
            }
        }

        private void InitCommand(PluginEvents.InitCommandEvent @e)
        {
            @e.Commands.Dispatch.bind("rtpp", (args) =>
            {
                if(args.Count>=2)
                {
                    switch(args[0])
                    {
                        case "setting":
                            ModifySetting(args[1], args[2]);
                            break;

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

        public override void OnExit()
        {
            foreach(var p in m_all_displayers)
            {
                p.Value.OnDestroy();
            }
            m_all_displayers.Clear();
        }
    }
}