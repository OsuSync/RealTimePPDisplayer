using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class MmfDisplayer : IDisplayer
    {
        private string m_mmf_name;
        private MemoryMappedFile m_mmf;
        private StringBuilder m_str_builder = new StringBuilder(1024);
        private byte[] m_str_buffer = new byte[1024];

        private static Task s_smooth_task;
        private static bool s_stop = false;
        private static List<Action> s_action_list = new List<Action>();
        private static int s_interval;

        private bool m_output = false;
        private int m_n50 = 0;
        private int m_n100 = 0;
        private int m_nmiss = 0;

        private double m_target_pp=0.0;
        private double m_current_pp=0.0;
        private double m_speed=0.0;

        static MmfDisplayer()
        {
            s_interval = (int)(1000.0 / Setting.FPS);
            s_smooth_task = Task.Run(()=>
            {
                while (!s_stop)
                {
                    foreach (var a in s_action_list)
                        a.Invoke();
                    Thread.Sleep(s_interval);
                }
            });
        }

        public MmfDisplayer(int? id)
        {
            m_mmf_name = id == null ? "rtpp" : $"rtpp{id}";
            m_mmf = MemoryMappedFile.CreateOrOpen(m_mmf_name, 1024);

            s_action_list.Add(Update);
        }

        ~MmfDisplayer()
        {
            m_mmf.Dispose();
            s_action_list.Remove(Update);
        }

        public void Clear()
        {
            m_output = false;
            m_target_pp = 0;
            m_current_pp = 0;
            m_speed = 0;
            m_n100 = 0;
            m_n50 = 0;
            m_nmiss = 0;

            m_str_buffer[0] = 0;
            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                stream.Write(m_str_buffer, 0, 1);
            }
        }

        private bool _init = false;

        public void Display(double pp,int n100, int n50, int nmiss)
        {
            if(!_init)
            {
                Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, m_mmf_name), ConsoleColor.DarkGreen);
                _init = true;
            }

            m_output = true;

            if (double.IsNaN(pp)) pp = 0;

            m_target_pp = pp;
            m_n100 = n100;
            m_n50 = n50;
            m_nmiss = nmiss;
        }

        private void Update()
        {
            if (!m_output) return;
            if (double.IsNaN(m_current_pp)) m_current_pp = 0;
            if (double.IsNaN(m_speed)) m_speed = 0;

            m_current_pp = SmoothMath.SmoothDamp(m_current_pp, m_target_pp, ref m_speed, Setting.SmoothTime, s_interval);

            m_str_builder.Clear();
            m_str_builder.AppendFormat("{0:F2}pp", m_current_pp);

            if (Setting.DisplayHitObject)
                m_str_builder.AppendFormat("\n{0}x100 {1}x50 {2}xMiss", m_n100, m_n50, m_nmiss);

            for (int i = 0; i < m_str_builder.Length; i++)
                m_str_buffer[i] = (byte)m_str_builder[i];
            m_str_buffer[m_str_builder.Length] = 0;

            using (MemoryMappedViewStream stream = m_mmf.CreateViewStream())
            {
                stream.Write(m_str_buffer, 0, m_str_builder.Length + 1);
            }
        }
    }
}
