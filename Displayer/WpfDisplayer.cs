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
        private PPWindow m_win;
        private Thread m_win_thread;

        public WpfDisplayer(int? id)
        {
            m_win_thread= new Thread(() => ShowPPWindow(id));
            m_win_thread.SetApartmentState(ApartmentState.STA);
            m_win_thread.Start();
        }

        private void ShowPPWindow(int? id)
        {
            m_win = new PPWindow(Setting.SmoothTime, Setting.FPS);

            if (id != null)
                m_win.Title += $"{id}";

            m_win.client_id.Content = id?.ToString() ?? "";

            m_win.ShowDialog();
        }

        public void Clear()
        {
            m_win.Dispatcher.Invoke(() =>
            {
                m_win.ClearPP();
                m_win.hit_label.Content = "";
            });
        }

        public void Display(double pp, int n300, int n100, int n50, int nmiss)
        {
            m_win?.Dispatcher.Invoke(() =>
            {
                m_win.PP = pp;
                m_win.hit_label.Content = $"{n100}x100 {n50}x50 {nmiss}xMiss";
            });
        }
    }
}
