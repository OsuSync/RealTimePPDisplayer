using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Expression;
using RealTimePPDisplayer.Formatter;
using RealTimePPDisplayer.Gui;
using static RealTimePPDisplayer.RealTimePPDisplayerPlugin;

namespace RealTimePPDisplayer.MultiOutput
{
    class MultiOutputDisplayer:DisplayerBase
    {
        public const string METHOD_NAME = "multi-output";

        class DisplayerContext
        {
            public MultiOutputItem item;
            public DisplayerBase displayer;
            public FormatterBase fmtter;
        }

        private ConcurrentDictionary<string,DisplayerContext> _displayers = new ConcurrentDictionary<string, DisplayerContext>();
        private Dictionary<string, Func<int?, MultiOutputItem,FormatterBase, DisplayerBase>> _displayer_creators;
        private Dictionary<string, FormatterConfiguration> _fmt_creators;

        static MultiOutputDisplayer()
        {
            RealTimePPDisplayerPlugin.Instance.RegisterMultiDisplayer("mmf", (id, item, fmt) => new MmfDisplayer(id, item.name, item.smooth ? fmt : null, !item.smooth ? fmt : null));
            /*RealTimePPDisplayerPlugin.Instance.RegisterMultiDisplayer("wpf", (id, item, fmt) =>
            {
                var displayer = new WpfDisplayer(id, item.smooth ? fmt : null, !item.smooth ? fmt : null);
                displayer.HideRow(item.smooth ? 2 : 1);
                return displayer;
            });*/
        }

        public MultiOutputDisplayer(int? id,
            Dictionary<string, Func<int?, MultiOutputItem, FormatterBase, DisplayerBase>> displayer_creators,
            Dictionary<string, FormatterConfiguration> fmt_creator
            ):base(id)
        {
            _fmt_creators = fmt_creator;
            _displayer_creators = displayer_creators;

            MultiOutputEditor.OnDisplayerRemove += (name) => RemoveDisplayer(name);
            MultiOutputEditor.OnDisplayerNew += (item) => AddDisplayer(item);
            MultiOutputEditor.OnDisplayerTypeChange += (name, type) => ChangeType(name, type);
            MultiOutputEditor.OnNameChange += (last_name, name) =>
            {
                _displayers.TryRemove(last_name, out var ctx);
                ctx.item.name = name;
                if (ctx.displayer is MmfDisplayer mmf)
                {
                    mmf.MmfName = name;
                }
                _displayers.TryAdd(name, ctx);
            };
            MultiOutputEditor.OnFormatChange += (name, format) =>
            {
                var ctx = _displayers[name];
                ctx.fmtter.Format = format;
            };
            MultiOutputEditor.OnSmoothChange += (name, smooth) =>
            {
                var ctx = _displayers[name];
                ctx.item.smooth = smooth;
                //Reload Displayer
                ChangeType(name, ctx.item.type);
            };
            MultiOutputEditor.OnFormatterChange += (name, fmtter) =>
            {
                var ctx = _displayers[name];
                var defaultFormat = RealTimePPDisplayerPlugin.Instance.GetFormatterDefaultFormat(fmtter);
                ctx.displayer.OnDestroy();

                ctx.item.formatter = fmtter;
                ctx.item.format = defaultFormat;
                ctx.fmtter = RealTimePPDisplayerPlugin.Instance.NewFormatter(ctx.item.formatter, ctx.item.format);
                ctx.displayer = _displayer_creators[ctx.item.type](Id,ctx.item,ctx.fmtter);
            };
        }

        public override void OnReady()
        {
            InitializeDisplayers();
            foreach (var p in _displayers)
            {
                p.Value.displayer.OnReady();
            }
        }

        private void InitializeDisplayers()
        {
            foreach(var item in Setting.MultiOutputItems)
            {
                if(item!=null)
                    AddDisplayer(item);
            }
        }

        private void AddDisplayer(MultiOutputItem item)
        {
            var fmt = RealTimePPDisplayerPlugin.Instance.NewFormatter(item.formatter,item.format);
            var ctx = new DisplayerContext()
            {
                item = item,
                displayer = null,
                fmtter = fmt
            };

            DisplayerBase displayer=null;
            if(_displayer_creators.TryGetValue(item.type,out var creator))
            {
                displayer = creator(Id, item, ctx.fmtter);
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

        private void ChangeType(string name, string type)
        {
            var ctx = _displayers[name];
            RemoveDisplayer(name);
            ctx.item.type = type;
            AddDisplayer(ctx.item);
        }

        public override void Display()
        {
            foreach(var kv in _displayers)
            {
                if (!kv.Value.item.modes.Contains(Mode.ToString())) continue;
                kv.Value.displayer.HitCount = HitCount;
                kv.Value.displayer.Pp = Pp;
                kv.Value.displayer.BeatmapTuple = BeatmapTuple;
                kv.Value.displayer.Playtime = Playtime;
                kv.Value.displayer.Mode = Mode;
                kv.Value.displayer.Mods = Mods;
                kv.Value.displayer.Status = Status;
                kv.Value.displayer.Playername = Playername;
                kv.Value.displayer.Display();
            }
        }

        public override void FixedDisplay(double time)
        {
            foreach (var kv in _displayers)
            {
                if (!kv.Value.item.modes.Contains(Mode.ToString())) continue;
                kv.Value.displayer.HitCount = HitCount;
                kv.Value.displayer.Pp = Pp;
                kv.Value.displayer.BeatmapTuple = BeatmapTuple;
                kv.Value.displayer.Playtime = Playtime;
                kv.Value.displayer.Mode = Mode;
                kv.Value.displayer.Mods = Mods;
                kv.Value.displayer.Status = Status;
                kv.Value.displayer.Playername = Playername;
                kv.Value.displayer.Accuracy = Accuracy;
                kv.Value.displayer.Score = Score;
                kv.Value.displayer.FixedDisplay(time);
            }
        }

        public override void Clear()
        {
            base.Clear();
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
