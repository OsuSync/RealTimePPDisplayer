using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace RealTimePPDisplayer.Displayer
{
    class MmfDisplayer : DisplayerBase
    {
        private readonly string _mmfName;

        private readonly char[] _ppBuffer = new char[1024];
        private readonly char[] _hitBuffer = new char[1024];
        private int _hitStrLen;
        private readonly MemoryMappedFile[] _mmfs=new MemoryMappedFile[2];

        private bool _output;

        private PPTuple _currentPp;
        private PPTuple _speed;

        private readonly bool _splited;

        public MmfDisplayer(int? id,bool splited = false)
        {
            _init = false;
            _output = false;
            _splited = splited;
            _mmfName = id == null ? "rtpp" : $"rtpp{id}";

            if (_splited)
            {
                _mmfs[0] = MemoryMappedFile.CreateOrOpen($"{_mmfName}-pp", 1024);
                _mmfs[1] = MemoryMappedFile.CreateOrOpen($"{_mmfName}-hit", 1024);
            }
            else
            {
                _mmfs[0] = MemoryMappedFile.CreateOrOpen(_mmfName, 1024);
            }
        }

        public override void Clear()
        {
            base.Clear();
            _output = false;
            _speed = PPTuple.Empty;
            _currentPp = PPTuple.Empty;

            foreach (var mmf in _mmfs)
            {
                if (mmf != null)
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                        stream.WriteByte(0);
            }
        }

        private bool _init;

        public override void Display()
        {
            if (!_init)
            {
                if(_splited)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, $"{_mmfName}-pp"), ConsoleColor.DarkGreen);
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, $"{_mmfName}-hit"), ConsoleColor.DarkGreen);
                }
                else
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, _mmfName), ConsoleColor.DarkGreen);
                _init = true;
            }

            _output = true;
            _hitStrLen= FormatHitCount().CopyTo(0,_hitBuffer,0);
        }

        public override void FixedDisplay(double time)
        {
            if (!_output) return;
            if (double.IsNaN(_currentPp.RealTimePP)) _currentPp.RealTimePP = 0;
            if (double.IsNaN(_currentPp.FullComboPP)) _currentPp.FullComboPP = 0;
            if (double.IsNaN(_speed.RealTimePP)) _speed.RealTimePP = 0;
            if (double.IsNaN(_speed.FullComboPP)) _speed.FullComboPP = 0;

            _currentPp = SmoothMath.SmoothDampPPTuple(_currentPp, Pp, ref _speed, time);

            var formatter = FormatPp(_currentPp);

            int len= formatter.CopyTo(0,_ppBuffer,0);

            StreamWriter[] streamWriters = new StreamWriter[2];

            if (_splited)
            {
                streamWriters[0] = new StreamWriter(_mmfs[0].CreateViewStream());
                streamWriters[1] = new StreamWriter(_mmfs[1].CreateViewStream());
            }
            else
            {
                streamWriters[0] = new StreamWriter(_mmfs[0].CreateViewStream());
                streamWriters[1] = streamWriters[0];
            }

            streamWriters[0].Write(_ppBuffer, 0, len);
            streamWriters[0].Write(!_splited ? '\n' : '\0');

            streamWriters[1].Write(_hitBuffer, 0, _hitStrLen);
            streamWriters[1].Write('\0');

            for (int i = 0; i < _mmfs.Length; i++)
                if (_mmfs[i] != null)
                    streamWriters[i].Dispose();
        }

        public override void OnDestroy()
        {
            foreach(var mmf in _mmfs)
            {
                mmf?.Dispose();
            }
        }
    }
}
