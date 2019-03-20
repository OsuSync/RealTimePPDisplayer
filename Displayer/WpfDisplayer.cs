using RealTimePPDisplayer.Displayer.View;
using RealTimePPDisplayer.Expression;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace RealTimePPDisplayer.Displayer
{
    class WpfDisplayer : DisplayerBase
    {
        private PPWindow _win;
        private bool _output = false;

        private PPTuple _currentPp;
        private PPTuple _speed;

        private StringFormatter ppFormatter = new PPStringFormatter();
        private StringFormatter hitCountFormatter = new HitCountStringFormatter();
        private ConcurrentDictionary<FormatArgs, IAstNode> astDict = new ConcurrentDictionary<FormatArgs, IAstNode>();

        public WpfDisplayer(int? id, StringFormatter ppFmt, StringFormatter hitFmt)
        {
            ppFormatter = ppFmt;
            hitCountFormatter = hitFmt;
            Initialize(id);
        }

        public WpfDisplayer(int? id)
        {
            Initialize(id);
        }

        public void Initialize(int? id)
        {
            if (Application.Current == null)
            {
                var winThread = new Thread(() => new Application().Run())
                {
                    Name = "STA WPF Application Thread"
                };
                winThread.SetApartmentState(ApartmentState.STA);
                winThread.Start();
                Thread.Sleep(100);
            }

            Debug.Assert(Application.Current != null, "Application.Current != null");
            Application.Current.Dispatcher.Invoke(() => ShowPPWindow(id));
        }

        public override void Clear()
        {
            base.Clear();
            _output = false;
            _speed = PPTuple.Empty;
            _currentPp = PPTuple.Empty;
            
            if (_win != null)
            {
                _win.HitCountContext = "";
                _win.PpContext = "";
            }
        }

        public override void Display()
        {
            if (_win != null)
                if(hitCountFormatter!=null)
                    _win.HitCountContext = Format(hitCountFormatter,astDict).ToString();
            _output = true;
            _win.Refresh();
        }

        public override void FixedDisplay(double time)
        {
            if (!_output)return;

            _currentPp=SmoothMath.SmoothDampPPTuple(_currentPp, Pp, ref _speed, time);

            if (ppFormatter != null)
            {
                var formatter = Format(ppFormatter, astDict, _currentPp, HitCount, BeatmapTuple);

                if (_win != null)
                    _win.PpContext = formatter.ToString();
                _win.Refresh();
            }
            else
            {
                _win.PpContext = "";
            }
        }

        private void ShowPPWindow(int? id)
        {
            _win = new PPWindow();

            if (id != null)
                _win.Title += $"{id}";

            _win.client_id.Content = id?.ToString() ?? "";

            _win.Show();
        }

        public override void OnDestroy()
        {
            _win.Dispatcher.Invoke(() => _win.Close());
        }
    }
}
