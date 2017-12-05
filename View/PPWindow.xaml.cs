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
                    m_current_pp = value;
                m_target_pp = value;
                display_pp = true;
            }
        }

        private double m_current_pp = 0.0;
        private double m_target_pp = 0.0;
        private double m_speed = 0.0;
        private double m_smooth_time;
        private double m_intertime = 0.033;

        public PPWindow(int st,int fps)
        {
            InitializeComponent();
            m_smooth_time = st/1000.0f;
            m_intertime = 1.0/fps;

            m_timer.Tick += (s, e) =>
            {
                if (!display_pp) return;
                m_current_pp=SmoothDamp(m_current_pp, m_target_pp, ref m_speed, m_smooth_time, m_intertime);
                pp_label.Content = $"{m_current_pp:F}pp";
            };
            m_timer.Interval = TimeSpan.FromSeconds(m_intertime);
            m_timer.Start();
        }

        public void ClearPP()
        {
            display_pp = false;
            m_current_pp = 0.0f;
            m_target_pp = 0.0f;
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