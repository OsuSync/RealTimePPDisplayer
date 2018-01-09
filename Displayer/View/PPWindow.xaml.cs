using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace RealTimePPDisplayer.Displayer.View
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
                m_target_pp = value;
                display_pp = true;
            }
        }

        private double m_current_pp = 0.0;
        private double m_target_pp = 0.0;
        private double m_speed = 0.0;
        private double m_smooth_time;
        private double m_intertime = 0.033;
        #region construct
        public PPWindow(int st,int fps)
        {
            InitializeComponent();
            m_smooth_time = st/1000.0;
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

            //Text Shadow
            if (Setting.WindowTextShadow)
            {
                pp_label.Effect = new DropShadowEffect() { BlurRadius = 5 };
                hit_label.Effect = new DropShadowEffect() { BlurRadius = 4 };
                client_id.Effect = new DropShadowEffect() { BlurRadius = 3 };
            }

            //Transparency
            if (Setting.BackgroundColor.A != 255)
                AllowsTransparency = true;

            topmost_item.IsChecked = Setting.Topmost;
            topmost_item.Header = (string)DefaultLanguage.UI_MENU_TOPMOST;
            Topmost = Setting.Topmost;
        }
        #endregion
        #region Show PP
        private void UpdatePP(object sender,EventArgs e)
        {
            if (!display_pp) return;
            if (double.IsNaN(m_current_pp)) m_current_pp = 0;
            if (double.IsNaN(m_speed)) m_speed = 0;

            m_current_pp = SmoothMath.SmoothDamp(m_current_pp, m_target_pp, ref m_speed, m_smooth_time, m_intertime);
            pp_label.Content = $"{m_current_pp:F2}pp";
        }

        public void ClearPP()
        {
            display_pp = false;
            m_current_pp = 0.0;
            m_target_pp = 0.0;
            m_speed = 0.0;
            pp_label.Content = "";
        }
        #endregion

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