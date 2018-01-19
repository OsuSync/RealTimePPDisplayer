using OsuRTDataProvider.Handler;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace RealTimePPDisplayer
{
    #region Converter

    internal static class ColorConverter
    {
        public static Color StringToColor(string color_str)
        {
            var color = new Color();
            color.A = Convert.ToByte(color_str.Substring(0, 2), 16);
            color.R = Convert.ToByte(color_str.Substring(2, 2), 16);
            color.G = Convert.ToByte(color_str.Substring(4, 2), 16);
            color.B = Convert.ToByte(color_str.Substring(6, 2), 16);
            return color;
        }

        public static string ColorToString(Color c)
        {
            return $"{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    #endregion Converter

    internal class SettingIni : IConfigurable
    {
        public ConfigurationElement UseText { get; set; }
        public ConfigurationElement TextOutputPath { get; set; }
        public ConfigurationElement DisplayHitObject { set; get; }
        public ConfigurationElement PPFontSize { set; get; }
        public ConfigurationElement PPFontColor { set; get; }
        public ConfigurationElement HitCountFontSize { set; get; }
        public ConfigurationElement HitCountFontColor { set; get; }
        public ConfigurationElement BackgroundColor { set; get; }
        public ConfigurationElement WindowHeight { set; get; }
        public ConfigurationElement WindowWidth { set; get; }
        public ConfigurationElement SmoothTime { get; set; }
        public ConfigurationElement FPS { get; set; }
        public ConfigurationElement Topmost { get; set; }
        public ConfigurationElement WindowTextShadow { get; set; }
        public ConfigurationElement OutputMethods { get; set; }
        public ConfigurationElement DebugMode { get; set; }
        public ConfigurationElement PPFormat { get; set; }
        public ConfigurationElement HitCountFormat { get; set; }
        public ConfigurationElement RoundDigits { get; set; }

        public void onConfigurationLoad()
        {
            try
            {
                Setting.UseText = bool.Parse(UseText);
                Setting.TextOutputPath = TextOutputPath;
                Setting.DisplayHitObject = bool.Parse(DisplayHitObject);
                Setting.PPFontColor = ColorConverter.StringToColor(PPFontColor);
                Setting.PPFontSize = int.Parse(PPFontSize);
                Setting.HitCountFontSize = int.Parse(HitCountFontSize);
                Setting.HitCountFontColor = ColorConverter.StringToColor(HitCountFontColor);
                Setting.BackgroundColor = ColorConverter.StringToColor(BackgroundColor);
                Setting.WindowHeight = int.Parse(WindowHeight);
                Setting.WindowWidth = int.Parse(WindowWidth);
                Setting.SmoothTime = int.Parse(SmoothTime);
                Setting.FPS = int.Parse(FPS);
                Setting.Topmost = bool.Parse(Topmost);
                Setting.DebugMode = bool.Parse(DebugMode);
                Setting.WindowTextShadow = bool.Parse(WindowTextShadow);
                Setting.OutputMethods = ((string)OutputMethods).Split(',').Select(s => s.Trim().ToLower());
                Setting.HitCountFormat = HitCountFormat;
                Setting.PPFormat = PPFormat;
                Setting.RoundDigits = int.Parse(RoundDigits);
            }
            catch (Exception e)
            {
                onConfigurationSave();
            }
        }

        public void onConfigurationSave()
        {
            UseText = Setting.UseText.ToString();
            TextOutputPath = Setting.TextOutputPath;
            DisplayHitObject = Setting.DisplayHitObject.ToString();
            PPFontColor = ColorConverter.ColorToString(Setting.PPFontColor);
            PPFontSize = Setting.PPFontSize.ToString();
            HitCountFontSize = Setting.HitCountFontSize.ToString();
            HitCountFontColor = ColorConverter.ColorToString(Setting.HitCountFontColor);
            BackgroundColor = ColorConverter.ColorToString(Setting.BackgroundColor);
            WindowHeight = Setting.WindowHeight.ToString();
            WindowWidth = Setting.WindowWidth.ToString();
            SmoothTime = Setting.SmoothTime.ToString();
            FPS = Setting.FPS.ToString();
            Topmost = Setting.Topmost.ToString();
            WindowTextShadow = Setting.WindowTextShadow.ToString();
            OutputMethods = string.Join(",", Setting.OutputMethods);
            DebugMode = Setting.DebugMode.ToString();
            PPFormat = Setting.PPFormat;
            HitCountFormat = Setting.HitCountFormat;
            RoundDigits = Setting.RoundDigits.ToString();
        }
    }

    internal static class Setting
    {
        public static IEnumerable<string> OutputMethods = new[]{ "wpf" };
        public static bool UseText = false;
        public static string TextOutputPath = @"rtpp{0}.txt";
        public static bool DisplayHitObject = true;
        public static int PPFontSize = 48;
        public static Color PPFontColor = Colors.White;
        public static int HitCountFontSize = 24;
        public static Color HitCountFontColor = Colors.White;
        public static Color BackgroundColor = ColorConverter.StringToColor("FF00FF00");
        public static int WindowWidth = 280;
        public static int WindowHeight = 172;
        public static int SmoothTime = 200;
        public static int FPS = 60;
        public static bool Topmost = true;
        public static bool WindowTextShadow = true;
        public static bool DebugMode = false;
        public static int RoundDigits = 2;
        //current_pp if_fc_pp max_pp
        public static string PPFormat = "${rtpp}pp";
        //combo max_combo n300 n100 n50 nmiss
        public static string HitCountFormat = "${n100}x100 ${n50}x50 ${nmiss}xMiss";
        

        private static SettingIni setting_output = new SettingIni();
        private static PluginConfiuration plugin_config = null;

        public static RealTimePPDisplayerPlugin PluginInstance
        {
            set
            {
                plugin_config = new PluginConfiuration(value, setting_output);
                plugin_config.ForceLoad();
            }
        }

        static Setting()
        {
            ExitHandler.OnConsloeExit += () => plugin_config?.ForceSave();
        }
    }
}