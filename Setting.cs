using Sync.Tools;
using System;
using System.Windows.Media;

namespace RealTimePPDisplayer
{
    internal class SettingIni : IConfigurable
    {
        public ConfigurationElement UseText { get; set; }
        public ConfigurationElement TextOutputPath { get; set; }
        public ConfigurationElement DisplayHitObject { set; get; }
        public ConfigurationElement PPFontSize { set; get; }
        public ConfigurationElement PPFontColor { set; get; }
        public ConfigurationElement HitObjectFontSize { set; get; }
        public ConfigurationElement HitObjectFontColor { set; get; }
        public ConfigurationElement BackgroundColor { set; get; }
        public ConfigurationElement WindowHeight { set; get; }
        public ConfigurationElement WindoWidth { set; get; }

        public void onConfigurationLoad()
        {
        }

        public void onConfigurationSave()
        {
        }
    }

    internal static class Setting
    {
        public static bool UseText = false;
        public static string TextOutputPath = @"..\rtpp.txt";
        public static bool DisplayHitObject = true;
        public static int PPFontSize=48;
        public static Color PPFontColor=Colors.White;
        public static int HitObjectFontSize=24;
        public static Color HitObjectFontColor=Colors.White;
        public static Color BackgroundColor=StringToColor("FF00FF00");
        public static int WindowWidth = 280;
        public static int WindowHeight=150;

        private static SettingIni setting_output = new SettingIni();
        private static PluginConfiuration plugin_config = null;

        public static RealTimePPDisplayerPlugin PluginInstance
        {
            set
            {
                plugin_config = new PluginConfiuration(value, setting_output);
            }
        }

        private static Color StringToColor(string color_str)
        {
            var color = new Color();
            color.A = Convert.ToByte(color_str.Substring(0, 2),16);
            color.R = Convert.ToByte(color_str.Substring(2, 2), 16);
            color.G = Convert.ToByte(color_str.Substring(4, 2), 16);
            color.B = Convert.ToByte(color_str.Substring(6, 2), 16);
            return color;
        }

        private static string ColorToString(Color c)
        {
            return $"{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        public static void SaveSetting()
        {
            setting_output.UseText = UseText.ToString();
            setting_output.TextOutputPath = TextOutputPath;
            setting_output.DisplayHitObject = DisplayHitObject.ToString();
            setting_output.PPFontColor = ColorToString(PPFontColor);
            setting_output.PPFontSize = PPFontSize.ToString();
            setting_output.HitObjectFontSize = HitObjectFontSize.ToString();
            setting_output.HitObjectFontColor = ColorToString(HitObjectFontColor);
            setting_output.BackgroundColor = ColorToString(BackgroundColor);
            setting_output.WindowHeight = WindowHeight.ToString();
            setting_output.WindoWidth = WindowWidth.ToString();

            plugin_config.ForceSave();
        }

        public static void LoadSetting()
        {
            plugin_config.ForceLoad();
            if (setting_output.UseText == "")
            {
                SaveSetting();
            }
            else
            {
                UseText = bool.Parse(setting_output.UseText);
                TextOutputPath = setting_output.TextOutputPath;
                DisplayHitObject = bool.Parse(setting_output.DisplayHitObject);
                PPFontColor = StringToColor(setting_output.PPFontColor);
                PPFontSize=int.Parse(setting_output.PPFontSize);
                HitObjectFontSize = int.Parse(setting_output.HitObjectFontSize);
                HitObjectFontColor= StringToColor(setting_output.HitObjectFontColor);
                BackgroundColor= StringToColor(setting_output.BackgroundColor);
                WindowHeight=int.Parse(setting_output.WindowHeight);
                WindowWidth = int.Parse(setting_output.WindoWidth);
            }
        }
    }
}