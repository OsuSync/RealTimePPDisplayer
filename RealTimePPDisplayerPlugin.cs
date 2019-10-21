using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sync.Plugins;
using Sync.Tools;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Gui;
using RealTimePPDisplayer.MultiOutput;
using RealTimePPDisplayer.Formatter;
using RealTimePPDisplayer.Warpper;

namespace RealTimePPDisplayer
{
    [SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Version = "^1.5.0", Require = true)]
    [SyncPluginID("8eb9e8e0-7bca-4a96-93f7-6408e76898a9", VERSION)]
    public class RealTimePPDisplayerPlugin : Plugin
    {
        public const string PLUGIN_NAME = "RealTimePPDisplayer";
        public const string PLUGIN_AUTHOR = "KedamaOvO";
        public const string VERSION= "1.8.7";

        private List<DisplayerController> _osuDisplayerControls = new List<DisplayerController>(16);

        private PluginConfigurationManager _configManager;

        public int TourneyWindowCount { get; private set; }
        public bool TourneyMode { get; private set; } = false;

        #region FixedDisplay Field
        public static RealTimePPDisplayerPlugin Instance { get; private set; }

        public struct FormatterConfiguration
        {
            /// string format
            public Func<string, FormatterBase> Creator { get; set; }
            public string DefaultFormat { get; set; }
        }

        private bool _stopFixedUpdate;
        private readonly Dictionary<string, Func<int?, DisplayerBase>> _displayerCreators = new Dictionary<string,Func<int?, DisplayerBase>>();
        private readonly Dictionary<string, Func<int?, MultiOutputItem, FormatterBase, DisplayerBase>> _multiDisplayerCreators = new Dictionary<string, Func<int?,MultiOutputItem, FormatterBase, DisplayerBase>>();
        private readonly Dictionary<string, FormatterConfiguration> _formatterCreators = new Dictionary<string, FormatterConfiguration>();

        public IEnumerable<string> DisplayerTypes => _displayerCreators.Keys;
        public IEnumerable<string> MultiDisplayerTypes => _multiDisplayerCreators.Keys;
        public IEnumerable<string> FormatterTypes => _formatterCreators.Keys;

        private TimeSpan _fixedInterval;
        
        private Task _fixedUpdateThread;
        #endregion

        public RealTimePPDisplayerPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            I18n.Instance.ApplyLanguage(new DefaultLanguage());
            EventBus.BindEvent<PluginEvents.InitCommandEvent>(InitCommand);
            EventBus.BindEvent<PluginEvents.ProgramReadyEvent>(InitDisplayer);
            EventBus.BindEvent<PluginEvents.ProgramReadyEvent>((e) =>
            {
                Task.Run(()=>UpdateChecker.CheckUpdate());
            });

            Instance = this;
        }

        /// <summary>
        /// Plugin Preinit
        /// </summary>
        public override void OnEnable()
        {
            _configManager = new PluginConfigurationManager(this);
            _configManager.AddItem(new SettingIni());

            var ortdp = new OsuRTDataProviderWarpper(getHoster().EnumPluings().FirstOrDefault(p => p.Name == "OsuRTDataProvider"),ref _osuDisplayerControls);
            var gui = getHoster().EnumPluings().FirstOrDefault(p => p.Name == "ConfigGUI");

            TourneyMode = ortdp.TourneyMode;
            TourneyWindowCount = ortdp.TourneyWindowCount;
            
            if (gui != null)
            {
                GuiRegisterHelper.RegisterFormatEditorWindow(gui);
            }

            _fixedInterval = TimeSpan.FromSeconds(1.0 / Setting.FPS);

            _fixedUpdateThread = Task.Run(() =>
            {
                while (!_stopFixedUpdate)
                {
                    foreach (var c in _osuDisplayerControls)
                        foreach(var d in c.Displayers)
                            d.Value.FixedDisplay(_fixedInterval.TotalSeconds);
                    Thread.Sleep(_fixedInterval);
                }
            });

            RegisterDisplayer("wpf", id => 
            {
                var d = new WpfDisplayer(id);
                if (!TourneyMode)
                    d.HideRow(0);
                if (!Setting.DisplayHitObject)
                    d.HideRow(2);
                return d;
            });

            RegisterDisplayer("mmf", id => new MmfDisplayer(id,"rtpp"));
            RegisterDisplayer("mmf-split", id => new MmfDisplayer(id,"rtpp",true));
            RegisterDisplayer(MultiOutputDisplayer.METHOD_NAME, id => new MultiOutputDisplayer(id,_multiDisplayerCreators,_formatterCreators));
            RegisterDisplayer("text", id => new TextDisplayer(id,string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString())));
            RegisterDisplayer("text-split", id => new TextDisplayer(id,string.Format(Setting.TextOutputPath, id == null ? "" : id.Value.ToString()),true));

            RegisterFormatter("rtpp-fmt", (fmt) => new RtppFormatter(fmt), "${rtpp@1}pp");
            RegisterFormatter("rtppfmt-bp", (fmt) => new RtppFormatWithBp(fmt), "${rtpp@1}pp (${rtpp_with_weight@1}pp) BP: #${rtbp@0}");

            IO.CurrentIO.WriteColor($"{PLUGIN_NAME} By {PLUGIN_AUTHOR} Ver.{VERSION}", ConsoleColor.DarkCyan);
        }

        private void InitDisplayer(PluginEvents.ProgramReadyEvent e)
        {
            //create displayer instance
            foreach(string displayerName in Setting.OutputMethods)
            {
                if (_displayerCreators.ContainsKey(displayerName))
                {
                    AddDisplayer(displayerName);
                }
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
            if(_displayerCreators.ContainsKey(name))
            {
                IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{name} Displayer exist!", ConsoleColor.Red);
                return false;
            }

            _displayerCreators[name]=creator;
            return true;
        }

        /// <summary>
        /// Register Displayer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public bool RegisterMultiDisplayer(string name, Func<int?,MultiOutputItem,FormatterBase, DisplayerBase> creator)
        {
            if (_multiDisplayerCreators.ContainsKey(name))
            {
                IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{name} Displayer exist!", ConsoleColor.Red);
                return false;
            }

            _multiDisplayerCreators[name] = creator;
            return true;
        }

        public bool RegisterFormatter(string name, Func<string,FormatterBase> creator,string defaultFormat)
        {
            if (_formatterCreators.ContainsKey(name))
            {
                IO.CurrentIO.WriteColor($"[RealTimePPDisplayer]{name} Formatter exist!", ConsoleColor.Red);
                return false;
            }

            _formatterCreators[name] = new FormatterConfiguration(){
                Creator = creator,
                DefaultFormat = defaultFormat
            };
            return true;
        }

        public string GetFormatterDefaultFormat(string name)
        {
            if (!_formatterCreators.ContainsKey(name))
                return "";
            return _formatterCreators[name].DefaultFormat;
        }

        public FormatterBase NewFormatter(string formatterName,string format="")
        {
            if (_formatterCreators.TryGetValue(formatterName, out var config))
            {
                if (format == "")
                {
                    format = GetFormatterDefaultFormat(formatterName);
                }
                var fmtter = config.Creator(format);
                return fmtter;
            }

            return null;
        }

        private bool AddDisplayer(string name)
        {

            foreach (var p in _osuDisplayerControls[0].Displayers)
                if (p.Key == name) return false;

            int size = TourneyMode ? TourneyWindowCount : 1;

            if (_displayerCreators.TryGetValue(name, out var creator))
            {
                for (int i = 0; i < size; i++)
                {
                    lock (_osuDisplayerControls[i])
                    {
                        int? id = null;
                        if (TourneyMode) id = i;

                        var displayer = creator(id);
                        _osuDisplayerControls[i].AddDisplayer(name, displayer);
                    }
                }
                return true;
            }

            return false;
        }

        private void RemoveDisplayer(string name)
        {
            foreach (var c in _osuDisplayerControls)
            {
                foreach (var p in c.Displayers)
                {
                    if (p.Key == name)
                    {
                        try
                        {
                            foreach (var control in _osuDisplayerControls)
                            {
                                lock (control)
                                {
                                    control.RemoveDisplayer(name);
                                }
                            }
                            continue;
                        }
                        catch (TaskCanceledException)
                        { }
                    }
                }
            }
        }

        private void RemoveAllDisplayer()
        {
            lock (_osuDisplayerControls)
            {
                foreach (var c in _osuDisplayerControls)
                    c.RemoveAllDisplayer();
            }
        }
        #endregion

        private void InitCommand(PluginEvents.InitCommandEvent e)
        {
            e.Commands.Dispatch.bind("rtpp", args =>
            {
                if(args[0] == "releases")
                {
                    System.Diagnostics.Process.Start("https://github.com/OsuSync/RealTimePPDisplayer/releases");
                }

                if(args.Count>=2)
                {
                    switch(args[0])
                    {
                        case "add":
                            return AddDisplayer(args[1]);

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
            _osuDisplayerControls.Clear();
        }

        public override void OnExit()
        {
            OnDisable();
        }
    }
}