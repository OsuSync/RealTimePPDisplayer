using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sync.Plugins;
using Sync.Tools;
using OsuRTDataProvider;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Gui;

namespace RealTimePPDisplayer
{
    [SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Version = "^1.4.3", Require = true)]
    [SyncPluginID("8eb9e8e0-7bca-4a96-93f7-6408e76898a9", VERSION)]
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";
        public const string VERSION= "1.6.3";

        private readonly DisplayerController[] _osuPpControls = new DisplayerController[16];

        private PluginConfigurationManager _configManager;

        public int TourneyWindowCount { get; private set; }
        public bool TourneyMode { get; private set; }

        #region FixedDisplay Field
        public static RealTimePPDisplayerPlugin Instance { get; private set; }
        public IEnumerable<string> DisplayerNames => _displayerCreators.Select(d=>d.Key);

        private bool _stopFixedUpdate;
        private readonly Dictionary<string, Func<int?, DisplayerBase>> _displayerCreators = new Dictionary<string,Func<int?, DisplayerBase>>();
        private readonly object _allDisplayerMtx = new object();
        private readonly LinkedList<KeyValuePair<string,DisplayerBase>> _allDisplayers = new LinkedList<KeyValuePair<string,DisplayerBase>>();
        private TimeSpan _fixedInterval;

        private Task _fixedUpdateThread;
        #endregion

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            I18n.Instance.ApplyLanguage(new DefaultLanguage());
            EventBus.BindEvent<PluginEvents.InitCommandEvent>(InitCommand);

            Instance = this;
        }

        /// <summary>
        /// Plugin Init
        /// </summary>
        public override void OnEnable()
        {
            _configManager = new PluginConfigurationManager(this);
            _configManager.AddItem(new SettingIni());

            var ortdp = getHoster().EnumPluings().FirstOrDefault(p => p.Name == "OsuRTDataProvider") as OsuRTDataProviderPlugin;
            var gui = getHoster().EnumPluings().FirstOrDefault(p => p.Name == "ConfigGUI");

            if (gui != null)
            {
                GuiRegisterHelper.RegisterFormatEditorWindow(gui);
            }

            TourneyMode = ortdp.TourneyListenerManagers != null;
            TourneyWindowCount = ortdp.TourneyListenerManagersCount;
            int size = TourneyMode ? TourneyWindowCount : 1;

            for (int i = 0; i < size; i++)
            {
                var manager = ortdp.ListenerManager;
                if (TourneyMode)
                {
                    manager = ortdp.TourneyListenerManagers[i];
                }
                _osuPpControls[i] = new DisplayerController(manager);
            }

            _fixedInterval = TimeSpan.FromSeconds(1.0 / Setting.FPS);

            _fixedUpdateThread = Task.Run(() =>
            {
                while (!_stopFixedUpdate)
                {
                    lock (_allDisplayerMtx)
                    {
                        foreach (var d in _allDisplayers)
                            d.Value.FixedDisplay(_fixedInterval.TotalSeconds);
                    }
                    Thread.Sleep(_fixedInterval);
                }
            });

            RegisterDisplayer("wpf", id => new WpfDisplayer(id));
            RegisterDisplayer("mmf", id => new MmfDisplayer(id));
            RegisterDisplayer("mmf-split", id => new MmfDisplayer(id,true));
            RegisterDisplayer("text", id => new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString())));
            RegisterDisplayer("text-split", id => new TextDisplayer(string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString()),true));
            RegisterDisplayer("console", id => new ConsoleDisplayer());

            IO.CurrentIO.WriteColor(PLUGIN_NAME + " By " + PLUGIN_AUTHOR, ConsoleColor.DarkCyan);
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
            if(_displayerCreators.ContainsKey(name))
            {
                IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{name} Displayer exist!", ConsoleColor.Red);
                return false;
            }

            _displayerCreators[name]=creator;

            if (Setting.OutputMethods.Contains(name))
                AddDisplayer(name, creator);

            return true;
        }

        private void AddDisplayer(string name,Func<int?, DisplayerBase> creator)
        {
            lock (_allDisplayerMtx)
            {
                foreach (var p in _allDisplayers)
                    if (p.Key == name) return;

                int size = TourneyMode ? TourneyWindowCount : 1;

                for (int i = 0; i < size; i++)
                {
                    int? id = null;
                    if (TourneyMode) id = i;

                    var displayer = creator(id);
                    _osuPpControls[i].AddDisplayer(name, displayer);
                    _allDisplayers.AddLast(new KeyValuePair<string, DisplayerBase>(name, displayer));
                }
            }
        }

        private void RemoveDisplayer(string name)
        {
            lock (_allDisplayerMtx)
            {
                for (var node = _allDisplayers.First; node != null;)
                {
                    if (node.Value.Key == name)
                    {
                        try
                        {
                            int size = TourneyMode ? TourneyWindowCount : 1;
                            for (int i = 0; i < size; i++)
                            {
                                _osuPpControls[i].RemoveDisplayer(name);

                            }

                            node.Value.Value.OnDestroy();
                            var nnode = node.Next;
                            _allDisplayers.Remove(node);
                            node = nnode;
                            continue;
                        }
                        catch (TaskCanceledException)
                        { }
                    }
                    node = node.Next;
                }
            }
        }

        private void RemoveAllDisplayer()
        {
            lock (_allDisplayerMtx)
            {
                foreach (var p in _allDisplayers)
                {
                    p.Value.Clear();
                    p.Value.OnDestroy();
                }

                _allDisplayers.Clear();
            }
        }
        #endregion

        private void InitCommand(PluginEvents.InitCommandEvent e)
        {
            e.Commands.Dispatch.bind("rtpp", args =>
            {
                if(args.Count>=2)
                {
                    switch(args[0])
                    {
                        case "add":
                            if (!_displayerCreators.ContainsKey(args[1])) return false;
                            var creator = _displayerCreators[args[1]];
                            AddDisplayer(args[1], creator);
                            break;

                        case "remove":
                            if (!_displayerCreators.ContainsKey(args[1])) return false;
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
            _stopFixedUpdate = true;
            _fixedUpdateThread.Wait(5000);
            RemoveAllDisplayer();
            _displayerCreators.Clear();
            for (int i = 0; i < _osuPpControls.Length; i++)
                _osuPpControls[i] = null;
        }

        public override void OnExit()
        {
            OnDisable();
        }
    }
}