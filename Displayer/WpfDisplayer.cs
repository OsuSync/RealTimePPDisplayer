using RealTimePPDisplayer.Displayer.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class WpfDisplayer : IDisplayer
    {
        private int? m_id = null;
        private PPWindow m_win;
        private Thread m_win_thread;

        private bool m_output = false;

        private double m_target_pp = 0.0;
        private double m_current_pp = 0.0;

        private double m_max_pp = 0.0;
        private double m_if_fc_pp = 0.0;

        private double m_speed = 0.0;

        public WpfDisplayer(int? id)
        {
            m_id = id;
        }

        public void Clear()
        {
            m_output = false;
            m_target_pp = 0;
            m_current_pp = 0;
            m_speed = 0;

            m_win.Dispatcher.Invoke(() =>
            {
                m_win.pp_label.Content = "";
                m_win.hit_label.Content = "";
            });
        }

        public void OnUpdatePP(double cur_pp, double if_fc_pp, double max_pp)
        {
            m_output = true;

            if (double.IsNaN(cur_pp)) cur_pp = 0;
            if (double.IsNaN(if_fc_pp)) if_fc_pp = 0;
            if (double.IsNaN(max_pp)) max_pp = 0;
            m_target_pp = cur_pp;
            m_if_fc_pp = if_fc_pp;
            m_max_pp = max_pp;
        }

        public void OnUpdateHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo)
        {
            var formatter = StringFormatter.GetHitCountFormatter();
            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "n300":
                        formatter.Fill(arg, n300); break;
                    case "n100":
                        formatter.Fill(arg, n100); break;
                    case "n50":
                        formatter.Fill(arg, n50); break;
                    case "nmiss":
                        formatter.Fill(arg, nmiss); break;
                    case "combo":
                        formatter.Fill(arg, combo); break;
                    case "max_combo":
                        formatter.Fill(arg, max_combo); break;
                }
            }

            string str = formatter.ToString();

            m_win?.Dispatcher.Invoke(() => {
                m_win.hit_label.Content = str;
            });
        }

        public void Display()
        {
        }

        public void FixedDisplay(double time)
        {
            if (!m_output)return;
            if (double.IsNaN(m_current_pp)) m_current_pp = 0;
            if (double.IsNaN(m_speed)) m_speed = 0;

            m_current_pp = SmoothMath.SmoothDamp(m_current_pp, m_target_pp, ref m_speed, Setting.SmoothTime*0.001, time);

            var formatter = StringFormatter.GetPPFormatter();
            foreach (var arg in formatter)
            {
                switch (arg)
                {
                    case "rtpp":
                        formatter.Fill(arg, m_current_pp); break;
                    case "if_fc_pp":
                        formatter.Fill(arg, m_if_fc_pp); break;
                    case "max_pp":
                        formatter.Fill(arg, m_max_pp); break;
                }
            }

            string str = formatter.ToString();

            m_win?.Dispatcher.Invoke(() =>
            {
                m_win.pp_label.Content = str;
            });
        }

        private void ShowPPWindow(int? id)
        {
            m_win = new PPWindow(Setting.SmoothTime, Setting.FPS);

            if (id != null)
                m_win.Title += $"{id}";

            m_win.client_id.Content = id?.ToString() ?? "";

            m_win.ShowDialog();
        }

        public void OnEnable()
        {
            m_win_thread = new Thread(() => ShowPPWindow(m_id));
            m_win_thread.SetApartmentState(ApartmentState.STA);
            m_win_thread.Start();
        }

        public void OnDisable()
        {
        }
    }
}
