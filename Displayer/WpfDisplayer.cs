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

        private double m_speed = 0.0;
        private double m_speed_fc = 0.0;

        public WpfDisplayer(int? id)
        {
            m_win_thread = new Thread(() => ShowPPWindow(id));
            m_win_thread.SetApartmentState(ApartmentState.STA);
            m_win_thread.Start();
        }

        public override void Clear()
        {
            m_output = false;
            m_target_pp.RealTimePP = 0;
            m_target_pp.FullComboPP = 0;
            m_target_pp.MaxPP = 0;
            m_current_pp.RealTimePP = 0;
            m_current_pp.FullComboPP = 0;
            m_current_pp.MaxPP = 0;

            m_speed = 0;
            m_speed_fc = 0.0;

            if (m_win != null)
            {
                m_win.HitCountContext = "";
                m_win.PPContext = "";
            }
        }

        public override void OnUpdatePP(PPTuple tuple)
        {
            m_output = true;

            m_target_pp.RealTimePP = double.IsNaN(tuple.RealTimePP)?0:tuple.RealTimePP;
            m_target_pp.FullComboPP = double.IsNaN(tuple.FullComboPP) ? 0 : tuple.FullComboPP;

            m_current_pp.MaxPP = double.IsNaN(tuple.MaxPP) ? 0 : tuple.MaxPP;
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
            if (double.IsNaN(m_speed)) m_speed = 0;
            if (double.IsNaN(m_speed_fc)) m_speed_fc = 0;

            m_current_pp.RealTimePP = SmoothMath.SmoothDamp(m_current_pp.RealTimePP, m_target_pp.RealTimePP, ref m_speed, Setting.SmoothTime*0.001, time);
            m_current_pp.FullComboPP = SmoothMath.SmoothDamp(m_current_pp.FullComboPP, m_target_pp.FullComboPP, ref m_speed_fc, Setting.SmoothTime * 0.001, time);

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
