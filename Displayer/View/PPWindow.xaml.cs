using System;
using System.ComponentModel;
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
    public partial class PPWindow : Window, INotifyPropertyChanged
    { 
        #region construct
        public PPWindow(int st,int fps)
        {
            InitializeComponent();
            DataContext = this;

            //Transparency
            if (Setting.BackgroundColor.A != 255)
                AllowsTransparency = true;

            LoadSetting();

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.PrimaryScreenWidth - Width - 50;
            Top = 0;

            MouseLeftButtonDown += (s,e) => DragMove();
            Setting.OnSettingChanged += ReloadSetting;
        }
        #endregion

        #region property 
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        private string m_pp_context = "0.00pp";
        public string PPContext
        {
            get => m_pp_context;
            set
            {
                m_pp_context = value;
                OnPropertyChanged("PPContext");
            }
        }

        private string m_hit_count_context = "0x100 0x50 0xMiss";
        public string HitCountContext
        {
            get => m_hit_count_context;
            set
            {
                m_hit_count_context = value;
                OnPropertyChanged("HitCountContext");
            }
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

        private void LoadSetting()
        {
            Width = Setting.WindowWidth;
            Height = Setting.WindowHeight;

            FontFamily = new FontFamily(Setting.FontName);

            //Hit Label
            hit_label.FontSize = Setting.HitCountFontSize;
            hit_label.Visibility = Setting.DisplayHitObject ? Visibility.Visible : Visibility.Hidden;
            hit_label.Foreground = new SolidColorBrush()
            {
                Color = Setting.HitCountFontColor
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

            topmost_item.IsChecked = Setting.Topmost;
            topmost_item.Header = (string)DefaultLanguage.UI_MENU_TOPMOST;
            Topmost = Setting.Topmost;
        }

        private void ReloadSetting()
        {
            Dispatcher.Invoke(() => LoadSetting());
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Setting.OnSettingChanged -= ReloadSetting;
        }
    }
}