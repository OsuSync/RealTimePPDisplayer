using ConfigGUI;
using RealTimePPDisplayer.Attribute;
using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Gui
{
    static class GuiRegisterHelper
    {
        public static void RegisterFormatEditorWindow(Plugin plugin)
        {
            var gui = plugin as ConfigGuiPlugin;
            gui.ItemFactory.RegisterItemCreator<PerformanceFormatAttribute>(new OpenFormatEditorCreator());
            gui.ItemFactory.RegisterItemCreator<HitCountFormatAttribute>(new OpenFormatEditorCreator());
            gui.ItemFactory.RegisterItemCreator<DisplayListAttribute>(new DisplayListAttributeCreator());
            gui.ItemFactory.RegisterItemCreator<ApiKeyAttribute>(new ApiKeyAttributeCreator());
        }
    }
}
