using Sync.Tools;
using Sync.Tools.ConfigGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{
    public class DefaultLanguage : I18nProvider
    {
        public static LanguageElement UI_MENU_TOPMOST = "Topmost";
        public static LanguageElement TEXT_MODE_OUTPUT_PATH_FORMAT = "[RealTimePPDisplayer]PP File: {0}";
        public static LanguageElement MMF_MODE_OUTPUT_PATH_FORMAT = "[RealTimePPDisplayer]Memory Mapping File: {0}";

        [ConfigI18n]
        public static LanguageElement FPS = "FPS";

        [ConfigI18n]
        public static LanguageElement TextOutputPath = "Text output path";

        [ConfigI18n]
        public static LanguageElement DisplayHitObject = "Display hit object";

        [ConfigI18n]
        public static LanguageElement FontName = "Font";

        [ConfigI18n]
        public static LanguageElement PPFontSize = "PP font size";

        [ConfigI18n]
        public static LanguageElement PPFontColor = "PP font color";

        [ConfigI18n]
        public static LanguageElement HitCountFontSize = "Hit count font size";

        [ConfigI18n]
        public static LanguageElement HitCountFontColor = "Hit count font color";

        [ConfigI18n]
        public static LanguageElement BackgroundColor = "Background color";

        [ConfigI18n]
        public static LanguageElement WindowHeight = "Window height";

        [ConfigI18n]
        public static LanguageElement WindowWidth = "Window width";

        [ConfigI18n]
        public static LanguageElement SmoothTime = "Smooth time";

        [ConfigI18n]
        public static LanguageElement Topmost = "Topmost";

        [ConfigI18n]
        public static LanguageElement WindowTextShadow = "Window text shadow";

        [ConfigI18n]
        public static LanguageElement OutputMethods = "Output methods";

        [ConfigI18n]
        public static LanguageElement DebugMode = "Debug mode";

        [ConfigI18n]
        public static LanguageElement PPFormat = "PP format";

        [ConfigI18n]
        public static LanguageElement HitCountFormat = "Hit count format";

        [ConfigI18n]
        public static LanguageElement RoundDigits = "Digits";

        [ConfigI18n]
        public static LanguageElement IgnoreTouchScreenDecrease = "Ignore touch screen decrease";
    }
}
