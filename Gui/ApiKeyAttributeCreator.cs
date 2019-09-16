using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using ConfigGUI.MultiSelect;
using Sync;
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
    class ApiKeyAttributeCreator : StringConfigurationItemCreator
    {
        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            Panel panel = base.CreateControl(attr, prop, configuration_instance);
            panel.Loaded += (s, e) =>
            {
                Panel parent = panel.Parent as Panel;
                int index = parent.Children.IndexOf(panel);
                Panel byCuteSyncPanel = parent.Children[index - 1] as Panel;
                Panel formatterPanel = parent.Children[index - 2] as Panel;

                var formatterCombo = formatterPanel.Children[1] as ComboBox;
                var byCuteSyncCheckBox = byCuteSyncPanel.Children[0] as CheckBox;
                var apiKeyBox = panel.Children[1] as TextBox;

                byCuteSyncPanel.Visibility = Setting.Formatter == "rtppfmt-bp" ? Visibility.Visible : Visibility.Collapsed;
                panel.Visibility = Setting.ByCuteSyncProxy ? Visibility.Collapsed : Visibility.Visible;

                formatterCombo.SelectionChanged += (ss, ee) =>
                {
                    string fmtter = formatterCombo.SelectedItem as string;
                    if (fmtter == "rtppfmt-bp")
                    {
                        byCuteSyncPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        byCuteSyncPanel.Visibility = Visibility.Collapsed;
                    }
                };

                byCuteSyncCheckBox.Checked += (ss, ee) =>
                {
                    if (SyncHost.Instance.EnumPluings().FirstOrDefault(p => p.Name == "PublicOsuBotTransferPlugin") == null)
                    {
                        ee.Handled = true;
                        byCuteSyncCheckBox.IsChecked = false;
                        MessageBox.Show(DefaultLanguage.MBX_POBT_VERSION_NO_INSTALLED, "RealTimePPDisplayer");
                        return;
                    }
                };

                byCuteSyncCheckBox.Click += (ss, ee) =>
                {
                    if (byCuteSyncCheckBox.IsChecked ?? false)
                    {
                        panel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        panel.Visibility = Visibility.Visible;
                    }
                };
            };
            return panel;
        }
    }
}
