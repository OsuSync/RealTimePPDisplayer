using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using RealTimePPDisplayer.Attribute;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Formatter;
using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace RealTimePPDisplayer.Gui
{
    class SettingFormatProxy : IConfigurable
    {
        private bool _is_pp = false;

        public SettingFormatProxy(bool is_pp)
        {
            _is_pp = is_pp;
        }

        public ConfigurationElement FormatElement
        {
            get => _is_pp ? Setting.PPFormat : Setting.HitCountFormat;
            set {
                if (_is_pp)
                {
                    Setting.PPFormat = value;
                }
                else
                {
                    Setting.HitCountFormat = value;
                }
            }
        }

        #region unused
        public void onConfigurationLoad()
        {

        }

        public void onConfigurationReload()
        {

        }

        public void onConfigurationSave()
        {

        }

        #endregion
    }

    class OpenFormatEditorCreator: BaseConfigurationItemCreator
    {
        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            var panel = base.CreateControl(attr, prop, configuration_instance);
            FormatEditor window = null;

            Button btn = new Button()
            {
                Content = DefaultLanguage.UI_OPENEDITOR_BUTTON_CONTENT,
                Margin = new Thickness(1)
            };

            btn.Click += (s, e) =>
            {
                SettingFormatProxy elementInstance = null;
                FormatterBase fmtter;
                if(prop.GetCustomAttribute<PerformanceFormatAttribute>() != null)
                {
                    elementInstance = new SettingFormatProxy(true);
                    fmtter = FormatterBase.GetPPFormatter();
                }
                else
                {
                    elementInstance = new SettingFormatProxy(false);
                    fmtter = FormatterBase.GetHitCountFormatter();
                }
                window = (window ?? new FormatEditor(typeof(SettingFormatProxy).GetProperty("FormatElement"), elementInstance, fmtter));

                window.Closed += (ss, ee) => window = null;

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
