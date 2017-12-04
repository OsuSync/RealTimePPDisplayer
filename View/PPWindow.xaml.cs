using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RealTimePPDisplayer.View
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class PPWindow :Window
    {
        DispatcherTimer m_timer=new DispatcherTimer();

        bool display_pp=false;

        public double PP {
            set{
                if (double.IsNaN(value)) value = 0.0;
                if (!display_pp)
                    current_pp = value;
                target_pp = value;
                display_pp = true;
            }
        }

        double current_pp = 0.0f;
        double target_pp = 0.0f;


        public PPWindow()
        {
            InitializeComponent();
            m_timer.Tick += (s, e) => 
            {
                if (!display_pp) return;
                current_pp = Lerp(current_pp, target_pp, 0.1f);
                pp_label.Content = $"{current_pp:F}pp";
            };
            m_timer.Interval = TimeSpan.FromMilliseconds(16);
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

        private double Lerp(double a, double b, double t)
        {
            return a * (1-t) + t * b;
        }
        
    }
}
