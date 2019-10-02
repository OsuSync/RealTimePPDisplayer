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
using System.Globalization;

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
            "acc",
            "score",
            "duration",
            "ur",
            "error_min",
            "error_max"
        };

        private static readonly List<string> s_rtpp_bp_only_variables = new List<string>()
        {
            "rtbp",
            "fcbp",
            "rtpp_with_weight",
            "fcpp_with_weight",
            "rtpp_weight",
            "fcpp_weight"
        };

        private static readonly List<string> s_functions = new List<string>()
        {
            "set(varName,expr)",
            "if(cond,true_expr,false_expr)",
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
            "isinf(a)",
            "smooth(var)"
        };
        private ConfigItemProxy item;
        private bool _not_close;
        private FormatterBase _fmt;

        public FormatEditor(PropertyInfo prop, object configurationInstance,FormatterBase fmt,bool not_close = true)
        {
            item = new ConfigItemProxy(prop, configurationInstance);
            _not_close = not_close;
            InitializeComponent();

            FormatEditBox.DataContext = item;

            if(!(fmt is RtppFormatter))
            {
                grid.RowDefinitions[4].Height = new GridLength(0);
                grid.RowDefinitions[5].Height = new GridLength(0);
                Height = 520;
            }

            _fmt = fmt;
            fmt.Format = item.Format;
            fmt.Displayer = new DisplayerBase();
            fmt.Displayer.HitCount = s_perviewHitcountTuple;
            fmt.Displayer.Pp = s_perviewPpTuple;
            fmt.Displayer.BeatmapTuple = s_perviewBeatmapTuple;
            fmt.Displayer.Playtime = 51000;
            fmt.Displayer.Mode = OsuRTDataProvider.Listen.OsuPlayMode.Osu;
            fmt.Displayer.Playername = Constants.PREVIWEING_PLAYNAME;
            fmt.Displayer.Mods = new OsuRTDataProvider.Mods.ModsInfo()
            {
                Mod = Mods.DoubleTime | Mods.Hidden | Mods.HardRock | Mods.Perfect
            };


            FormatEditBox.TextChanged += (s, e) =>
            {
                //fmt.Format = FormatEditBox.Text;
                item.Format = FormatEditBox.Text;

                if (AutoCompileCheckbox.IsChecked??false)
                {
                    FormatPreview();
                }
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

            if(fmt is RtppFormatWithBp)
            {
                foreach (var para in s_rtpp_bp_only_variables)
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

                    VariableButtonsList.Children.Add(btn);
                }
            }

            foreach (var para in s_functions)
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

        bool is_preview_updaing = false;

        Brush normal_state= new SolidColorBrush(Colors.Black);
        Brush updating_state = new SolidColorBrush(Colors.Yellow);

        private void FormatPreview()
        {
            if (is_preview_updaing)
                return;

            PreviewLabel.Foreground = updating_state;

            Task.Run(() =>
            {
                is_preview_updaing = true;
                string cmp_str = item.Format;
                string formated = string.Empty;

                do
                {
                    formated = _fmt.GetFormattedString();
                } while (item.Format!=cmp_str);

                FormatPreviewBox.Dispatcher.BeginInvoke(new Action(() => {
                    FormatPreviewBox.Text = formated;
                    PreviewLabel.Foreground = normal_state;
                    is_preview_updaing = false;
                }));
            });
        }

        private void ManualPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            FormatPreview();
        }
    }
}
