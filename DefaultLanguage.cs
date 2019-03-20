using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;

namespace RealTimePPDisplayer
{
    public class DefaultLanguage : I18nProvider
    {
        public static LanguageElement UI_MENU_TOPMOST = "Topmost";
        public static LanguageElement TEXT_MODE_OUTPUT_PATH_FORMAT = "[RealTimePPDisplayer]PP File: {0}";
        public static LanguageElement MMF_MODE_OUTPUT_PATH_FORMAT = "[RealTimePPDisplayer]Memory Mapping File: {0}";
        public static LanguageElement CHECK_RTPPD_UPDATE = "[RealTimePPDisplayer]Found a new version of RealTimePPDisplayer({0})";
        public static LanguageElement CHECK_OPPAI_UPDATE = "[RealTimePPDisplayer]Found a new version of oppai-ng({0})";
        public static LanguageElement CHECK_GOTO_RELEASE_PAGE_HINT= "[RealTimePPDisplayer]Enter \"rtpp releases\" to open the releases page in your browser.";

        public static GuiLanguageElement FPS = "FPS";
        public static GuiLanguageElement TextOutputPath = "Text output path";
        public static GuiLanguageElement DisplayHitObject = "Display hit object";
        public static GuiLanguageElement FontName = "Font";
        public static GuiLanguageElement PPFontSize = "PP font size";
        public static GuiLanguageElement PPFontColor = "PP font color";
        public static GuiLanguageElement HitCountFontSize = "Hit count font size";
        public static GuiLanguageElement HitCountFontColor = "Hit count font color";
        public static GuiLanguageElement BackgroundColor = "Background color";
        public static GuiLanguageElement WindowHeight = "Window height";
        public static GuiLanguageElement WindowWidth = "Window width";
        public static GuiLanguageElement SmoothTime = "Smooth time";
        public static GuiLanguageElement Topmost = "Topmost";
        public static GuiLanguageElement WindowTextShadow = "Window text shadow";
        public static GuiLanguageElement OutputMethods = "Output methods";
        public static GuiLanguageElement DebugMode = "Debug mode";
        public static GuiLanguageElement PPFormat = "PP format";
        public static GuiLanguageElement HitCountFormat = "Hit count format";
        public static GuiLanguageElement RoundDigits = "Digits";
        public static GuiLanguageElement IgnoreTouchScreenDecrease = "Ignore touch screen decrease";
        public static GuiLanguageElement RankingSendPerformanceToChat = "Send pp to Chat on Ranking";
        public static GuiLanguageElement UseUnicodePerformanceInformation = "Perfer metadata in original language";
    }
}
