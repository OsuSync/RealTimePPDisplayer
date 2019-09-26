using Newtonsoft.Json;
using RealTimePPDisplayer.Attribute;
using RealTimePPDisplayer.MultiOutput;
using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace RealTimePPDisplayer
{
    #region Converter

    internal static class ColorConverter
    {
        public static Color StringToColor(string color_str)
        {
            var color = new Color();
            if (color_str[0] == '#')
            {
                color.R = Convert.ToByte(color_str.Substring(1, 2), 16);
                color.G = Convert.ToByte(color_str.Substring(3, 2), 16);
                color.B = Convert.ToByte(color_str.Substring(5, 2), 16);
                color.A = Convert.ToByte(color_str.Substring(7, 2), 16);
            }
            else
            {
                color.A = Convert.ToByte(color_str.Substring(0, 2), 16);
                color.R = Convert.ToByte(color_str.Substring(2, 2), 16);
                color.G = Convert.ToByte(color_str.Substring(4, 2), 16);
                color.B = Convert.ToByte(color_str.Substring(6, 2), 16);
            }
            return color;
        }

        public static string ColorToString(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";
        }
    }

    #endregion Converter

    internal class SettingIni : IConfigurable
    {
        [Path(IsDirectory = false,RequireRestart = true)]
        public ConfigurationElement TextOutputPath
        {
            get => Setting.TextOutputPath;
            set => Setting.TextOutputPath = value;
        }

        [Font]
        public ConfigurationElement FontName
        {
            get=>Setting.FontName;
            set
            {
                Setting.FontName = value;
                Setting.SettingChanged();
            }
        }

        [Integer(MinValue = 10,MaxValue = 150)]
        public ConfigurationElement PPFontSize
        {
            set
            {
                Setting.PPFontSize = int.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.PPFontSize.ToString();
        }


        [Integer(MinValue = 10, MaxValue = 150)]
        public ConfigurationElement HitCountFontSize
        {
            set
            {
                Setting.HitCountFontSize = int.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.HitCountFontSize.ToString();
        }

        [CustomColor]
        public ConfigurationElement PPFontColor
        {
            set
            {
                Setting.PPFontColor = ColorConverter.StringToColor(value);
                Setting.SettingChanged();
            }
            get => ColorConverter.ColorToString(Setting.PPFontColor);
        }

        [CustomColor]
        public ConfigurationElement HitCountFontColor
        {
            set
            {
                Setting.HitCountFontColor = ColorConverter.StringToColor(value);
                Setting.SettingChanged();
            }
            get => ColorConverter.ColorToString(Setting.HitCountFontColor);
        }

        [CustomColor]
        public ConfigurationElement BackgroundColor
        {
            set
            {
                Setting.BackgroundColor = ColorConverter.StringToColor(value);
                Setting.SettingChanged();
            }
            get => ColorConverter.ColorToString(Setting.BackgroundColor);
        }

        [Integer(MinValue =100,MaxValue =1080)]
        public ConfigurationElement WindowHeight
        {
            set
            {
                Setting.WindowHeight = int.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.WindowHeight.ToString();
        }

        [Integer(MinValue = 100, MaxValue = 1920)]
        public ConfigurationElement WindowWidth
        {
            set
            {
                Setting.WindowWidth = int.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.WindowWidth.ToString();
        }

        [Integer(MinValue = 30, MaxValue = 10000)]
        public ConfigurationElement SmoothTime
        {
            set
            {
                Setting.SmoothTime = int.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.SmoothTime.ToString();
        }

        [Integer(MinValue = 1, MaxValue = 60)]
        public ConfigurationElement FPS
        {
            set
            {
                Setting.FPS = int.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.FPS.ToString();
        }

        [DisplayList(NoCheck = true,SplitSeparator = ',', AllowMultiSelect = true,RequireRestart = true)]
        public ConfigurationElement OutputMethods
        {
            get => string.Join(",", Setting.OutputMethods);
            set
            {
                Setting.OutputMethods = value.ToString().Split(',').Select(s=>s.Trim());
            }
        }

        [PerformanceFormat]
        public ConfigurationElement PPFormat
        {
            get => Setting.PPFormat.Replace(Environment.NewLine, "\n").Replace("\n", "\\n");
            set
            {
                Setting.PPFormat = value.ToString().Replace("\\n", Environment.NewLine);
                Setting.PPFormatChanged();
            }
        }

        [HitCountFormat]
        public ConfigurationElement HitCountFormat
        {
            get => Setting.HitCountFormat.Replace(Environment.NewLine, "\n").Replace("\n", "\\n");
            set
            {
                Setting.HitCountFormat = value.ToString().Replace("\\n", Environment.NewLine);
                Setting.HitCountFormatChanged();
            }
        }

        [FormatterList(RequireRestart = true)]
        public ConfigurationElement Formatter
        {
            get => Setting.Formatter;
            set => Setting.Formatter = value.ToString();
        }

        [Bool]
        public ConfigurationElement ByCuteSyncProxy
        {
            get => Setting.ByCuteSyncProxy.ToString();
            set => Setting.ByCuteSyncProxy = bool.Parse(value);
        }

        [ApiKey]
        public ConfigurationElement ApiKey
        {
            set => Setting.ApiKey = value;
            get => Setting.ApiKey;
        }

        [Integer(MinValue = 0,MaxValue = 15)]
        public ConfigurationElement RoundDigits
        {
            set=>Setting.RoundDigits = int.Parse(value);
            get => Setting.RoundDigits.ToString();
        }

        [Bool(RequireRestart = true)]
        public ConfigurationElement DebugMode
        {
            set
            {
                Setting.DebugMode = bool.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.DebugMode.ToString();
        }

        [Bool]
        public ConfigurationElement DisplayHitObject
        {
            get => Setting.DisplayHitObject.ToString();
            set
            {
                Setting.DisplayHitObject = bool.Parse(value);
                Setting.SettingChanged();
            }
        }

        [Bool]
        public ConfigurationElement Topmost
        {
            set
            {
                Setting.Topmost = bool.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.Topmost.ToString();
        }

        [Bool]
        public ConfigurationElement WindowTextShadow
        {
            set
            {
                Setting.WindowTextShadow = bool.Parse(value);
                Setting.SettingChanged();
            }
            get => Setting.WindowTextShadow.ToString();
        }

        [Bool]
        public ConfigurationElement IgnoreTouchScreenDecrease
        {
            set => Setting.IgnoreTouchScreenDecrease = bool.Parse(value);
            get => Setting.IgnoreTouchScreenDecrease.ToString();
        }

        [Bool]
        public ConfigurationElement RankingSendPerformanceToChat
        {
            set => Setting.RankingSendPerformanceToChat = bool.Parse(value);
            get => Setting.RankingSendPerformanceToChat.ToString();
        }

        [Bool]
        public ConfigurationElement UseUnicodePerformanceInformation
        {
            set => Setting.UseUnicodePerformanceInformation = bool.Parse(value);
            get => Setting.UseUnicodePerformanceInformation.ToString();
        }


        private string _multiOutputConfigureFile = "..\\rtpp-multi-output-config.json";
        [String(Hide = true)]
        public ConfigurationElement MultiOutputConfigureFile
        {
            get
            {
                File.WriteAllText(_multiOutputConfigureFile,JsonConvert.SerializeObject(Setting.MultiOutputItems, Formatting.Indented));
                return _multiOutputConfigureFile;
            }

            set
            {
                try
                {
                    _multiOutputConfigureFile = value;
                    Setting.MultiOutputItems = JsonConvert.DeserializeObject<List<MultiOutputItem>>(File.ReadAllText(_multiOutputConfigureFile));
                }
                catch (Exception)
                {
                    Setting.MultiOutputItems = JsonConvert.DeserializeObject<List<MultiOutputItem>>(value);
                }
            }
        }

        public void onConfigurationLoad()
        {
        }

        public void onConfigurationReload()
        {
            Setting.SettingChanged();
        }

        public void onConfigurationSave()
        {
        }
    }

    public static class Setting
    {
        public static IEnumerable<string> OutputMethods = new[] { "wpf" };
        public static List<MultiOutputItem> MultiOutputItems = new List<MultiOutputItem>();
        public static string TextOutputPath = @"rtpp{0}.txt";
        public static bool DisplayHitObject = true;
        public static string FontName = "Segoe UI";
        public static int PPFontSize = 48;
        public static Color PPFontColor = Colors.White;
        public static int HitCountFontSize = 24;
        public static Color HitCountFontColor = Colors.White;
        public static Color BackgroundColor = ColorConverter.StringToColor("#00FF00FF");
        public static int WindowWidth = 280;
        public static int WindowHeight = 172;
        public static int SmoothTime = 200;
        public static int FPS = 30;
        public static bool Topmost = true;
        public static bool WindowTextShadow = true;
        public static bool DebugMode = false;
        public static int RoundDigits = 1;

        private static string _ppFormat = "${smooth(rtpp)}pp";
        public static string PPFormat
        {
            get => _ppFormat;
            set {
                _ppFormat = value;
                PPFormatChanged();
            }
        }

        private static string _hitCountFormat = "${n100@0}x100 ${n50@0}x50 ${nmiss@0}xMiss";
        public static string HitCountFormat
        {
            get => _hitCountFormat;
            set{
                _hitCountFormat = value;
                HitCountFormatChanged();
            }
        }
        public static string Formatter = "rtpp-fmt";
        public static bool IgnoreTouchScreenDecrease = false;
        public static bool RankingSendPerformanceToChat = false;
        public static bool UseUnicodePerformanceInformation = false;
        public static string ApiKey = string.Empty;
        public static bool ByCuteSyncProxy = true;

        public static event Action OnSettingChanged;
        public static event Action OnPPFormatChanged;
        public static event Action OnHitCountFormatChanged;

        public static void SettingChanged()
        {
            OnSettingChanged?.Invoke();
        }

        public static void PPFormatChanged()
        {
            OnPPFormatChanged?.Invoke();
        }

        public static void HitCountFormatChanged()
        {
            OnHitCountFormatChanged?.Invoke();
        }
    }
}