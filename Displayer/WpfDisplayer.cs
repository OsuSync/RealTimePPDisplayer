using RealTimePPDisplayer.Displayer.View;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace RealTimePPDisplayer.Displayer
{
    class WpfDisplayer : DisplayerBase
    {
        private PPWindow _win;
        private bool _output;

        PPTuple _currentPp;
        PPTuple _speed;

        public WpfDisplayer(int? id)
        {
            _output = false;
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
                _win.HitCountContext = FormatHitCount().ToString();
            _output = true;
            _win.Refresh();
        }

        public override void FixedDisplay(double time)
        {
            if (!_output)return;

            _currentPp=SmoothMath.SmoothDampPPTuple(_currentPp, Pp, ref _speed, time);

            var formatter = FormatPp(_currentPp);

            if (_win != null)
                _win.PpContext = formatter.ToString();
            _win.Refresh();
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
