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

        PPTuple m_current_pp;
        PPTuple m_target_pp;
        PPTuple m_speed;

        public WpfDisplayer(int? id)
        {
            m_win_thread = new Thread(() => ShowPPWindow(id));
            m_win_thread.SetApartmentState(ApartmentState.STA);
            m_win_thread.Start();
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
            if (double.IsNaN(m_current_pp.RealTimePP)) m_current_pp.RealTimePP = 0;
            if (double.IsNaN(m_current_pp.FullComboPP)) m_current_pp.FullComboPP = 0;
            if (double.IsNaN(m_speed.RealTimePP)) m_speed.RealTimePP = 0;
            if (double.IsNaN(m_speed.FullComboPP)) m_speed.FullComboPP = 0;

            m_current_pp=SmoothMath.SmoothDampPPTuple(m_current_pp, m_target_pp, ref m_speed, time);

            var formatter = GetFormattedPP(m_current_pp);
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
