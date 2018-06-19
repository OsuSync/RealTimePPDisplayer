using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using RealTimePPDisplayer.Displayer;
using Sync.Tools.ConfigurationAttribute;
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

namespace RealTimePPDisplayer.Gui
{
    public class OpenFormatEditorCreator: BaseConfigurationItemCreator
    {
        public class ConfigItemProxy: INotifyPropertyChanged
        {
            private PropertyInfo m_prop;
            private object m_instance;

            public event PropertyChangedEventHandler PropertyChanged;

            public string Format
            {
                get => GetConfigValue(m_prop, m_instance).Replace("\\n",Environment.NewLine);
                set
                {
                    SetConfigValue(m_prop, m_instance, value.Replace(Environment.NewLine, "\\n").Replace("\n","\\n"));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Format)));
                }
            }

            public ConfigItemProxy(PropertyInfo prop, object configuration_instance)
            {
                m_prop = prop;
                m_instance = configuration_instance;
            }
        }

        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            var panel = base.CreateControl(attr, prop, configuration_instance);
            var item = new ConfigItemProxy(prop, configuration_instance);

            Button btn = new Button()
            {
                Content = "Open Editor",
                Margin = new Thickness(1)
            };

            btn.Click += (s, e) =>
            {
                var ishticount = attr is HitCountFormatAttribute;
                new FormatEditor(item, ishticount).ShowDialog();
            };

            panel.Children.Add(btn);

            return panel;
        }
    }
}
