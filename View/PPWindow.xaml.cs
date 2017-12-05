using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RealTimePPDisplayer.View
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class PPWindow : Window
    {
        private DispatcherTimer m_timer = new DispatcherTimer();

        private bool display_pp = false;

        public double PP
        {
            set
            {
                if (double.IsNaN(value)) value = 0.0;
                if (!display_pp)
                    current_pp = value;
                target_pp = value;
                display_pp = true;
            }
        }

        private double current_pp = 0.0;
        private double target_pp = 0.0;
        private double speed = 0.0;
        private double smooth_time;

        public PPWindow(int st)
        {
            InitializeComponent();
            this.smooth_time = st/1000.0f;

            m_timer.Tick += (s, e) =>
            {
                if (!display_pp) return;
                current_pp=SmoothDamp(current_pp, target_pp, ref speed, smooth_time, 0.033);
                pp_label.Content = $"{current_pp:F}pp";
            };
            m_timer.Interval = TimeSpan.FromMilliseconds(33);
            m_timer.Start();
        }

        public void ClearPP()
        {
            display_pp = false;
            current_pp = 0.0f;
            target_pp = 0.0f;
            pp_label.Content = "";
        }

        private void Window_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        //From: http://devblog.aliasinggames.com/inertialdamp-unity-smoothdamp-alternative/
        public double InertialDamp(double previousValue, Double targetValue, double smoothTime, float h)
        {
            double x = previousValue - targetValue;
            double newValue = x + h * (-1f / smoothTime * x);
            return targetValue + newValue;
        }

        public double SmoothDamp(double previousValue, double targetValue, ref double speed, double smoothTime, double h)
        {
            double T1 = 0.36f * smoothTime;
            double T2 = 0.64f * smoothTime;
            double x = previousValue - targetValue;
            double newSpeed = speed + h * (-1f / (T1 * T2) * x - (T1 + T2) / (T1 * T2) * speed);
            double newValue = x + h * speed;
            speed = newSpeed;
            return targetValue + newValue;
        }
    }
}