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

        private FormatterBase ppFormatter;
        private FormatterBase hitCountFormatter;

        public WpfDisplayer(int? id, FormatterBase ppFmt, FormatterBase hitFmt):base(id)
        {
            ppFormatter = ppFmt;
            hitCountFormatter = hitFmt;
            Initialize();
        }

        public WpfDisplayer(int? id):base(id)
        {
            ppFormatter = FormatterBase.GetPPFormatter();
            hitCountFormatter = FormatterBase.GetHitCountFormatter();
            Initialize();
        }

        public void Initialize()
        {
            if(ppFormatter!=null)
                ppFormatter.Displayer = this;
            if (hitCountFormatter != null)
                hitCountFormatter.Displayer = this;

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
            Application.Current.Dispatcher.Invoke(() => ShowPPWindow(Id));
        }

        public void HideRow(int row)
        {
            _win?.HideRow(row);
        }

        public override void Clear()
        {
            base.Clear();
            _output = false;

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
                    _win.HitCountContext = hitCountFormatter.GetFormattedString();
                }
            }
                    
            _output = true;
            _win.Refresh();
        }

        public override void FixedDisplay(double time)
        {
            if (!_output)return;

            if (ppFormatter != null)
            {
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
