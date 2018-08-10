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
using static RealTimePPDisplayer.Gui.OpenFormatEditorCreator;

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
                get => BaseConfigurationItemCreator.GetConfigValue(m_prop, m_instance).Replace("\\n", Environment.NewLine);
                set
                {
                    BaseConfigurationItemCreator.SetConfigValue(m_prop, m_instance, value.Replace(Environment.NewLine, "\\n").Replace("\n", "\\n"));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Format)));
                }
            }

            public ConfigItemProxy(PropertyInfo prop, object configuration_instance)
            {
                m_prop = prop;
                m_instance = configuration_instance;
            }
        }

        private static readonly PPTuple s_perview_pp_tuple = new PPTuple
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

        private static readonly HitCountTuple s_perview_hitcount_tuple = new HitCountTuple
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
            RealTimeMaxCombo = 1256
        };

        private static readonly List<string> s_pp_commands = new List<string>()
        {
            "rtpp","rtpp_aim","rtpp_acc","rtpp_speed",
            "fcpp","fcpp_aim","fcpp_acc","fcpp_speed",
            "maxpp","maxpp_aim","maxpp_acc","maxpp_speed"
        };

        private static readonly List<string> s_hitcount_commands = new List<string>()
        {
            "n300","n300g","n200","n150","n100","n50","nmiss","ngeki","nkatu",
            "rtmaxcombo","fullcombo","maxcombo","combo"
        };

        public FormatEditor(PropertyInfo prop, object configuration_instance, bool isHitCount)
        {
            var item = new ConfigItemProxy(prop, configuration_instance);

            InitializeComponent();

            FormatEditBox.DataContext = item;

            FormatEditBox.TextChanged += (s, e) =>
            {
                string formated;
                if (isHitCount)
                {
                    formated = DisplayerBase.GetFormattedHitCount(s_perview_hitcount_tuple).ToString();
                }
                else
                {
                    formated = DisplayerBase.GetFormattedPP(s_perview_pp_tuple).ToString();
                }
                FormatPreviewBox.Text = formated;
            };

            var commands = isHitCount ? s_hitcount_commands:s_pp_commands;
            foreach (var para in commands)
            {
                var btn = new Button()
                {
                    Content = para.Replace("_", "__"),
                    Margin = new Thickness(1)
                };

                btn.Click += (s, e) =>
                {
                    item.Format += $"${{{para}}}";
                };

                ButtonsList.Children.Add(btn);
            }
        }
    }
}
