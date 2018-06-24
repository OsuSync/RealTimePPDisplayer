using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
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

            Button btn = new Button()
            {
                Content = "Open Editor",
                Margin = new Thickness(1)
            };

            btn.Click += (s, e) =>
            {
                var ishticount = attr is HitCountFormatAttribute;
                new FormatEditor(prop, configuration_instance, ishticount).ShowDialog();
            };

            panel.Children.Add(btn);

            return panel;
        }
    }
}
