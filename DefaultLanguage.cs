using Sync.Tools;
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
    }
}
