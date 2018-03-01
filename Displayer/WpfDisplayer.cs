using RealTimePPDisplayer.Displayer.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RealTimePPDisplayer.Displayer
{
    class WpfDisplayer : DisplayerBase
    {
        private PPWindow m_win;
        private static Thread m_win_thread;
        private bool m_output = false;

        PPTuple m_current_pp;
        PPTuple m_target_pp;
        PPTuple m_speed;

        static WpfDisplayer()
        {
            m_win_thread = new Thread(() =>
            {
                if (Application.Current == null)
                    new Application().Run();
            });
            m_win_thread.Name = "STA WPF Application Thread";
            m_win_thread.SetApartmentState(ApartmentState.STA);
            m_win_thread.Start();
        }


        public WpfDisplayer(int? id)
        {
            Application.Current.Dispatcher.Invoke(() => ShowPPWindow(id));
        }

        public override void Clear()
        {
            m_output = false;
            m_speed = PPTuple.Empty;
            m_current_pp = PPTuple.Empty;
            m_target_pp = PPTuple.Empty;

            
            if (m_win != null)
            {
                m_win.HitCountContext = "";
                m_win.PPContext = "";
            }
        }

        public override void OnUpdatePP(PPTuple tuple)
        {
            m_output = true;

            m_target_pp = tuple;
        }

        public override void OnUpdateHitCount(HitCountTuple tuple)
        {
            var formatter = GetFormattedHitCount(tuple);

            string str = formatter.ToString();

            if (m_win != null)
                m_win.HitCountContext = formatter.ToString();
        }

        public override void FixedDisplay(double time)
        {
            if (!m_output)return;

            m_current_pp=SmoothMath.SmoothDampPPTuple(m_current_pp, m_target_pp, ref m_speed, time);

            var formatter = GetFormattedPP(m_current_pp);
            string str = formatter.ToString();

            if (m_win != null)
                m_win.PPContext = formatter.ToString();
        }

        private void ShowPPWindow(int? id)
        {
            m_win = new PPWindow();

            if (id != null)
                m_win.Title += $"{id}";

            m_win.client_id.Content = id?.ToString() ?? "";

            m_win.Show();
        }

        public override void OnDestroy()
        {
            m_win.Dispatcher.Invoke(() => m_win.Close());
        }
    }
}
