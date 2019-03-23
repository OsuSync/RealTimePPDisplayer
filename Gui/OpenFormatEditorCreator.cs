using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using RealTimePPDisplayer.Attribute;
using Sync.Tools.ConfigurationAttribute;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace RealTimePPDisplayer.Gui
{
    class OpenFormatEditorCreator: BaseConfigurationItemCreator
    {
        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            var panel = base.CreateControl(attr, prop, configuration_instance);
            FormatEditor window = null;

            Button btn = new Button()
            {
                Content = "Open Editor",
                Margin = new Thickness(1)
            };

            btn.Click += (s, e) =>
            {
                string format = "";
                if(prop.GetCustomAttribute<PerformanceFormatAttribute>() != null)
                {
                    format = Setting.PPFormat;
                }
                else
                {
                    format = Setting.HitCountFormat;
                }
                window = (window ?? new FormatEditor(prop, configuration_instance,new StringFormatter(format)));
                if (window.Visibility == Visibility.Visible)
                    window.Activate();
                else
                    window.Show();
            };

            panel.Children.Add(btn);

            return panel;
        }
    }
}
