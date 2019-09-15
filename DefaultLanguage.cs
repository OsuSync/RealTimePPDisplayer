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
        public static LanguageElement UI_UPDATE_CONFIGGUI_MESSAGEBOX = "Please update the ConfigGUI to 0.2.1 or later.";
        public static LanguageElement HINT_BEATMAP_NO_FOUND = "[RealTimePPDisplayer]No found beatmap! Make sure the Songs path is correct. (Go to OsuRTDataProvider to set the Songs path).";
        public static LanguageElement HINT_CANNOT_WATCH_OTHER_PLAYER = "[RealTimePPDisplayer]Can't watch other player. (If you want to watch other player, please uncheck \"By CuteSync Proxy\" and enter api key).";
        public static LanguageElement HINT_POBT_VERSION_LOWER = "[RealTimePPDisplayer]Your PublicOsuBotTransfer version is lower than 1.3.0 and you cannot use \"By CuteSync Proxy\".";
        public static LanguageElement MBX_POBT_VERSION_NO_INSTALLED = "You have not installed the PublicOsuBotTransfer plugin.";

        public static LanguageElement UI_OPENEDITOR_BUTTON_CONTENT = "Open Editor";
        public static LanguageElement UI_MULTIOUTPUTEDITOR_BUTTON_CONTENT = "Multi Output Editor";

        public static LanguageElement UI_NAME_LABEL = "Name:";
        public static LanguageElement UI_FORMAT_LABEL = "Format:";
        public static LanguageElement UI_DELETETHIS_BUTTON_CONTENT = "Delete THIS";
        public static LanguageElement UI_SMOOTH_CHECKBOX_CONTENT = "Smooth";
        public static LanguageElement UI_EDITFORMAT_BUTTON_CONTENT = "Edit";

        public static LanguageElement UI_SMMOTH_CHECKBOX_TOOLTIP = "Enable smooth";
        public static LanguageElement UI_TYPE_COMBOBOX_TOOLTIP = "Select Output Method";
        public static LanguageElement UI_FORMATTER_COMBOBOX_TOOLTIP = "Select Formatter";
        public static LanguageElement UI_MODES_MULTISELECTCOMBOBOX_TOOLTIP = "In which mode update Displayer";
        
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
        public static GuiLanguageElement ByCuteSyncProxy = "By CuteSync Proxy";
        public static GuiLanguageElement ApiKey = "Osu! Api key";
        public static GuiLanguageElement Formatter = "Formatter";
    }
}
