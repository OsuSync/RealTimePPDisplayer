using RealTimePPDisplayer.Displayer.View;
using RealTimePPDisplayer.Expression;
using RealTimePPDisplayer.Formatter;
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

        private FormatterBase ppFormatter;
        private FormatterBase hitCountFormatter;

        public WpfDisplayer(int? id, FormatterBase ppFmt, FormatterBase hitFmt)
        {
            ppFormatter = ppFmt;
            hitCountFormatter = hitFmt;
            Initialize(id);
        }

        public WpfDisplayer(int? id)
        {
            ppFormatter = StringFormatter.GetPPFormatter();
            hitCountFormatter = StringFormatter.GetHitCountFormatter();
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

        public void HideRow(int row)
        {
            _win?.HideRow(row);
        }

        public override void Clear()
        {
            base.Clear();
            _output = false;
            _speed = PPTuple.Empty;
            _currentPp = PPTuple.Empty;

            if (ppFormatter is IFormatterClearable ppfmt)
                ppfmt.Clear();

            if (hitCountFormatter is IFormatterClearable hitfmt)
                hitfmt.Clear();

            if (_win != null)
            {
                _win.HitCountContext = "";
                _win.PpContext = "";
            }
        }

        public override void Display()
        {
            if (_win != null)
            {
                if (hitCountFormatter != null)
                {
                    SetFormatterArgs(hitCountFormatter);
                    _win.HitCountContext = hitCountFormatter.GetFormattedString();
                }
            }
                    
            _output = true;
            _win.Refresh();
        }

        public override void FixedDisplay(double time)
        {
            if (!_output)return;

            _currentPp=SmoothMath.SmoothDampPPTuple(_currentPp, Pp, ref _speed, time);

            if (ppFormatter != null)
            {
                SetFormatterArgs(ppFormatter);
                ppFormatter.Pp = _currentPp;

                if (_win != null)
                    _win.PpContext = ppFormatter.GetFormattedString();
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
