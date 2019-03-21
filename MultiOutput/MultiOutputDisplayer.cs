using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Expression;
using RealTimePPDisplayer.Gui;

namespace RealTimePPDisplayer.MultiOutput
{
    class MultiOutputDisplayer:DisplayerBase
    {
        private int? _id;

        struct DisplayerContext
        {
            public MultiOutputItem item;
            public DisplayerBase displayer;
            public StringFormatter fmt;
        }

        private ConcurrentDictionary<string,DisplayerContext> _displayers = new ConcurrentDictionary<string, DisplayerContext>();

        public MultiOutputDisplayer(int? id)
        {
            _id = id;

            MultiOutputEditor.OnDisplayerRemove += (name) => RemoveDisplayer(name);
            MultiOutputEditor.OnDisplayerNew += (item) => AddDisplayer(item);
            MultiOutputEditor.OnDisplayerTypeChange += (name, type) => ChangeType(name, type);
            MultiOutputEditor.OnNameChange += (last_name, name) =>
            {
                var ctx = _displayers[last_name];
                ctx.item.name = name;
                if (ctx.displayer is MmfDisplayer mmf)
                {
                    mmf.MmfName = name;
                }
            };
            MultiOutputEditor.OnFormatChange += (name, format) =>
            {
                var ctx = _displayers[name];
                ctx.fmt.Format = format;
            };
            MultiOutputEditor.OnSmoothChange += (name, smooth) =>
            {
                var ctx = _displayers[name];
                ctx.item.smooth = smooth;
                //Reload Displayer
                ChangeType(name, ctx.item.type);
            };


            InitializeDisplayers();
        }

        private void InitializeDisplayers()
        {
            foreach(var item in Setting.MultiOutputItems)
            {
                if(item!=null)
                    AddDisplayer(item);
            }
        }

        private MultiOutputItem AddDisplayer(string name,string format,MultiOutputType type,bool smooth)
        {
            name = string.IsNullOrEmpty(name) ? $"multi-{Setting.MultiOutputItems.Count}" : name;
            var item = new MultiOutputItem()
            {
                name = name,
                format = "${rtpp}",
                type = type,
                smooth = smooth
            };
            AddDisplayer(item);
            return item;
        }

        private void AddDisplayer(MultiOutputItem item)
        {
            var ctx = new DisplayerContext()
            {
                item = item,
                displayer = null,
                fmt = new StringFormatter(item.format),
            };

            DisplayerBase displayer=null;
            switch (item.type)
            {
                case MultiOutputType.MMF:
                    displayer = new MmfDisplayer(_id, item.name, item.smooth ? ctx.fmt : null, !item.smooth ? ctx.fmt : null);break;
                case MultiOutputType.WPF:
                    displayer = new WpfDisplayer(_id, item.smooth ? ctx.fmt : null, !item.smooth ? ctx.fmt : null);
                    (displayer as WpfDisplayer).HideRow(item.smooth ? 2 : 1);
                    break;
            }

            ctx.displayer = displayer;

            _displayers.TryAdd(item.name, ctx);
        }

        private void RemoveDisplayer(string name)
        {
            var ctx = _displayers[name];
            ctx.displayer.OnDestroy();
            _displayers.TryRemove(name,out _);
        }

        private void ChangeType(string name, MultiOutputType type)
        {
            var ctx = _displayers[name];
            RemoveDisplayer(name);
            AddDisplayer(name, ctx.item.format, type,ctx.item.smooth);
        }

        public override void Display()
        {
            foreach(var kv in _displayers)
            {
                kv.Value.displayer.HitCount = HitCount;
                kv.Value.displayer.Pp = Pp;
                kv.Value.displayer.BeatmapTuple = BeatmapTuple;
                kv.Value.displayer.Playtime = Playtime;
                kv.Value.displayer.Display();
            }
        }

        public override void FixedDisplay(double time)
        {
            foreach (var kv in _displayers)
            {
                kv.Value.displayer.HitCount = HitCount;
                kv.Value.displayer.Pp = Pp;
                kv.Value.displayer.BeatmapTuple = BeatmapTuple;
                kv.Value.displayer.Playtime = Playtime;
                kv.Value.displayer.FixedDisplay(time);
            }
        }

        public override void Clear()
        {
            foreach (var kv in _displayers)
            {
                kv.Value.displayer.Clear();
            }
        }

        public override void OnDestroy()
        {
            foreach (var kv in _displayers)
            {
                kv.Value.displayer.OnDestroy();
            }
        }
    }
}
