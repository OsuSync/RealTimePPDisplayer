using OsuRTDataProvider;
using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Warpper
{
    class OsuRTDataProviderWarpper
    {
        public int TourneyWindowCount { get;}
        public bool TourneyMode { get;}
        public OsuRTDataProviderWarpper(Plugin p,ref List<DisplayerController> _osuDisplayerControls)
        {
            if(p is OsuRTDataProviderPlugin ortdp)
            {
                TourneyWindowCount = ortdp.TourneyListenerManagersCount;
                int size = 1;
                if (TourneyWindowCount != 0)
                {
                    size = TourneyWindowCount;
                    TourneyMode = true;
                }


                //Create DisplayerController per osu instance
                for (int i = 0; i < size; i++)
                {
                    var manager = ortdp.ListenerManager;
                    if (TourneyMode)
                    {
                        manager = ortdp.TourneyListenerManagers[i];
                    }
                    _osuDisplayerControls.Add(new DisplayerController(manager));
                }
            }
        }
    }
}
