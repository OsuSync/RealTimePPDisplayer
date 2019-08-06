using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using RealTimePPDisplayer.Displayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RealTimePPDisplayer.Attribute;
using static RealTimePPDisplayer.Gui.OpenFormatEditorCreator;
using RealTimePPDisplayer.Expression;
using System.Collections.Concurrent;
using static OsuRTDataProvider.Mods.ModsInfo;
using RealTimePPDisplayer.Formatter;

namespace RealTimePPDisplayer.Gui
{
    partial class FormatEditor : Window
    {
        class ConfigItemProxy : INotifyPropertyChanged
        {
            private readonly PropertyInfo m_prop;
            private readonly object m_instance;

            public event PropertyChangedEventHandler PropertyChanged;

            public string Format
            {
                get => BaseConfigurationItemCreator.GetConfigValue(m_prop, m_instance);
                set
                {
                    BaseConfigurationItemCreator.SetConfigValue(m_prop, m_instance, value);
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Format)));
                }
            }

            public ConfigItemProxy(PropertyInfo prop, object configuration_instance)
            {
                m_prop = prop;
                m_instance = configuration_instance;
            }
        }

        private static readonly PPTuple s_perviewPpTuple = new PPTuple
        {
            RealTimeAccuracyPP = 52.25,
            RealTimeAimPP = 121.1,
            RealTimeSpeedPP = 85.1,
            RealTimePP = 351.0,

            FullComboAccuracyPP = 100.2,
            FullComboAimPP = 200.4,
            FullComboSpeedPP = 123.1,
            FullComboPP = 727.0,

            MaxAccuracyPP = 150,
            MaxAimPP = 250.0,
            MaxSpeedPP = 153.1,
            MaxPP = 810.6
        };

        private static readonly HitCountTuple s_perviewHitcountTuple = new HitCountTuple
        {
            Count300 = 501,
            Count100 = 12,
            Count50 = 1,
            CountMiss = 3,
            CountKatu = 100,
            CountGeki = 205,

            Combo=1254,
            PlayerMaxCombo = 1254,
            FullCombo = 1854,
            CurrentMaxCombo = 1256
        };

        private static readonly BeatmapTuple s_perviewBeatmapTuple = new BeatmapTuple
        {
            ObjectsCount=1358,
            Duration = 135000
    };

        private static readonly List<string> s_variables = new List<string>()
        {
            "pi",
            "e",
            "inf",
            "rtpp",
            "rtpp_aim",
            "rtpp_acc",
            "rtpp_speed",
            "fcpp",
            "fcpp_aim",
            "fcpp_acc",
            "fcpp_speed",
            "maxpp",
            "maxpp_aim",
            "maxpp_acc",
            "maxpp_speed",
            "n300",
            "n300g",
            "n200",
            "n150",
            "n100",
            "n50",
            "nmiss",
            "ngeki",
            "nkatu",
            "fullcombo",
            "maxcombo",
            "combo",
            "player_maxcombo",
            "current_maxcombo",
            "objects_count",
            "rtstars",
            "stars",
            "playtime",
            "duration"
        };

        private static readonly List<string> s_functionss = new List<string>()
        {
            "set(varName,expr)",
            "if(cond,true_expr,flase_expr)",
            "sin(x)",
            "cos(x)",
            "tan(x)",
            "asin(x)",
            "acos(x)",
            "atan(x)",
            "pow(x,y)",
            "sqrt(x)",
            "max(a,b)",
            "min(a,b)",
            "exp(x)",
            "log(x)",
            "log10(x)",
            "floor(x)",
            "ceil(x)",
            "round(x,digits)",
            "sign(x)",
            "truncate(x)",
            "clamp(x,min,max)",
            "lerp(from,to,t)",
            "mod(x,y)",
            "random()",
            "getTime()",
            "isnan(a)",
            "isinf(a)"
        };

        private bool _not_close;

        public FormatEditor(PropertyInfo prop, object configurationInstance,FormatterBase fmt,bool not_close = true)
        {
            var item = new ConfigItemProxy(prop, configurationInstance);
            _not_close = not_close;
            InitializeComponent();

            FormatEditBox.DataContext = item;

            if(!(fmt is RtppFormatter))
            {
                grid.RowDefinitions[4].Height = new GridLength(0);
                grid.RowDefinitions[5].Height = new GridLength(0);
                Height = 520;
            }

            fmt.Format = item.Format;
            fmt.HitCount = s_perviewHitcountTuple;
            fmt.Pp = s_perviewPpTuple;
            fmt.BeatmapTuple = s_perviewBeatmapTuple;
            fmt.Playtime = 51000;
            fmt.Mode = OsuRTDataProvider.Listen.OsuPlayMode.Osu;
            fmt.Mods = new OsuRTDataProvider.Mods.ModsInfo()
            {
                Mod = Mods.DoubleTime | Mods.Hidden | Mods.HardRock | Mods.Perfect
            };

            FormatEditBox.TextChanged += (s, e) =>
            {
                fmt.Format = FormatEditBox.Text;
                string formated = fmt.GetFormattedString();
                FormatPreviewBox.Text = formated;
            };

            foreach (var para in s_variables)
            {
                var btn = new Button()
                {
                    Content = para.Replace("_", "__"),
                    Margin = new Thickness(1)
                };

                btn.Click += (s, e) =>
                {
                    int pos = FormatEditBox.CaretIndex;
                    string val = $"${{{para}}}";
                    item.Format = item.Format.Insert(pos,val);
                    FormatEditBox.CaretIndex = pos + val.Length;
                };

                VariableButtonsList.Children.Add(btn);
            }

            foreach (var para in s_functionss)
            {
                var btn = new Button()
                {
                    Content = para.Replace("_", "__"),
                    Margin = new Thickness(1)
                };

                btn.Click += (s, e) =>
                {
                    int pos = FormatEditBox.CaretIndex;
                    string val = $"${{{para}}}";
                    item.Format = item.Format.Insert(pos, val);
                    FormatEditBox.CaretIndex = pos + val.Length;
                };

                FunctionButtonsList.Children.Add(btn);
            }
        }

        private void FormatEditor_OnClosing(object sender, CancelEventArgs e)
        {
            if (_not_close)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
