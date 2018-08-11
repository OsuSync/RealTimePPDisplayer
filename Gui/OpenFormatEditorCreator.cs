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
                window = (window ?? new FormatEditor(prop, configuration_instance));
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
