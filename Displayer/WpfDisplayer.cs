using RealTimePPDisplayer.Displayer.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RealTimePPDisplayer.Displayer
{
    class WpfDisplayer : DisplayerBase
    {
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
            m_win_thread = new Thread(() => ShowPPWindow(id));
            m_win_thread.SetApartmentState(ApartmentState.STA);
            m_win_thread.Start();
        }

        public override void Clear()
        {
            m_output = false;
            m_target_pp = 0;
            m_current_pp = 0;
            m_speed = 0;

            if (m_win != null)
            {
                m_win.HitCountContext = "";
                m_win.PPContext = "";
            }
        }

        public override void OnUpdatePP(double cur_pp, double if_fc_pp, double max_pp)
        {
            m_output = true;

            if (double.IsNaN(cur_pp)) cur_pp = 0;
            if (double.IsNaN(if_fc_pp)) if_fc_pp = 0;
            if (double.IsNaN(max_pp)) max_pp = 0;
            m_target_pp = cur_pp;
            m_if_fc_pp = if_fc_pp;
            m_max_pp = max_pp;
        }

        public override void OnUpdateHitCount(int n300, int n100, int n50, int nmiss, int combo, int max_combo)
        {
            var formatter = GetFormattedHitCount(n300, n100, n50, nmiss, combo, max_combo);

            string str = formatter.ToString();

            if (m_win != null)
                m_win.HitCountContext = formatter.ToString();
        }

        public override void FixedDisplay(double time)
        {
            if (!m_output)return;
            if (double.IsNaN(m_current_pp)) m_current_pp = 0;
            if (double.IsNaN(m_speed)) m_speed = 0;

            m_current_pp = SmoothMath.SmoothDamp(m_current_pp, m_target_pp, ref m_speed, Setting.SmoothTime*0.001, time);

            var formatter = GetFormattedPP(m_current_pp, m_if_fc_pp, m_max_pp);

            string str = formatter.ToString();

            if (m_win != null)
                m_win.PPContext = formatter.ToString();
        }

        private void ShowPPWindow(int? id)
        {
            m_win = new PPWindow(Setting.SmoothTime, Setting.FPS);

            if (id != null)
                m_win.Title += $"{id}";

            m_win.client_id.Content = id?.ToString() ?? "";

            m_win.ShowDialog();
        }

        public override void OnDestroy()
        {
            m_win.Dispatcher.Invoke(() => m_win.Close());
        }
    }
}
