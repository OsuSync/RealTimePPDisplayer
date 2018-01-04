using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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

            MouseLeftButtonDown += (s,e) => DragMove();
 
            m_timer.Tick += UpdatePP;
            m_timer.Interval = TimeSpan.FromSeconds(m_intertime);
            m_timer.Start();

            Width = Setting.WindowWidth;
            Height = Setting.WindowHeight;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.PrimaryScreenWidth - Width - 50;
            Top = 0;

            //Hit Label
            hit_label.FontSize = Setting.HitObjectFontSize;
            hit_label.Visibility = Setting.DisplayHitObject?Visibility.Visible:Visibility.Hidden;
            hit_label.Foreground = new SolidColorBrush()
            {
                Color = Setting.HitObjectFontColor
            };

            //PP Label
            pp_label.FontSize = Setting.PPFontSize;
            pp_label.Foreground = new SolidColorBrush()
            {
                Color = Setting.PPFontColor
            };

            Background = new SolidColorBrush()
            {
                Color = Setting.BackgroundColor
            };

            topmost_item.IsChecked = Setting.Topmost;
            Topmost = Setting.Topmost;
        }

        #region Show PP
        private void UpdatePP(object sender,EventArgs e)
        {
            if (!display_pp) return;
            m_current_pp = SmoothDamp(m_current_pp, m_target_pp, ref m_speed, m_smooth_time, m_intertime);
            pp_label.Content = $"{m_current_pp:F}pp";
        }

        public void ClearPP()
        {
            display_pp = false;
            m_current_pp = 0.0f;
            m_target_pp = 0.0f;
            pp_label.Content = "";
        }
        #endregion

        //From: http://devblog.aliasinggames.com/inertialdamp-unity-smoothdamp-alternative/
        private double SmoothDamp(double previousValue, double targetValue, ref double speed, double smoothTime, double h)
        {
            double T1 = 0.36f * smoothTime;
            double T2 = 0.64f * smoothTime;
            double x = previousValue - targetValue;
            double newSpeed = speed + h * (-1f / (T1 * T2) * x - (T1 + T2) / (T1 * T2) * speed);
            double newValue = x + h * speed;
            speed = newSpeed;
            return targetValue + newValue;
        }


        private void WindowSizeChanged(object sender,SizeChangedEventArgs e)
        {
            Setting.WindowHeight = (int)e.NewSize.Height;
            Setting.WindowWidth = (int)e.NewSize.Width;
        }

        private void TopmostItem_Click(object sender, RoutedEventArgs e)
        {
            Topmost = topmost_item.IsChecked;
            Setting.Topmost = Topmost;
        }
    }
}