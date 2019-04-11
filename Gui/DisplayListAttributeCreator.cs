using ConfigGUI;
using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using ConfigGUI.MultiSelect;
using RealTimePPDisplayer.MultiOutput;
using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RealTimePPDisplayer.Gui
{
    class DisplayListAttributeCreator: ListConfigurationItemCreator
    {
        private MultiOutputEditor _editor;

        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            Panel panel = base.CreateControl(attr, prop, configuration_instance);
            var multi_combo = panel.Children[1] as MultiSelectComboBox;
            multi_combo.Width = 200;

            Button multi_mmf_btn = new Button()
            {
                Content = DefaultLanguage.UI_MULTIOUTPUTEDITOR_BUTTON_CONTENT,
                IsEnabled = !ConfigWindow.RequireRestart,
                Margin = new Thickness(1),
                Visibility = Setting.OutputMethods.Any(om => om == MultiOutputDisplayer.METHOD_NAME) ? Visibility.Visible : Visibility.Hidden
            };

            multi_mmf_btn.Click += (s, e) =>
            {
                if(typeof(ListConfigurationItemCreator).Assembly.GetName().Version < Version.Parse("0.2.1"))
                {
                    MessageBox.Show(DefaultLanguage.UI_UPDATE_CONFIGGUI_MESSAGEBOX, "Multi Output",MessageBoxButton.OK);
                    return;
                }
                _editor = _editor ?? new MultiOutputEditor();
                if (_editor.Visibility == Visibility.Visible)
                    _editor.Activate();
                else
                    _editor.Show();
            };

            multi_combo.Click += (s, e) =>
            {
                multi_mmf_btn.Visibility = Setting.OutputMethods.Any(om => om == MultiOutputDisplayer.METHOD_NAME) ? Visibility.Visible : Visibility.Hidden;
                multi_mmf_btn.IsEnabled = !ConfigWindow.RequireRestart;
            };
            panel.Children.Add(multi_mmf_btn);
            return panel;
        }
    }
}
