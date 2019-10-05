using RealTimePPDisplayer.Expression;
using RealTimePPDisplayer.Formatter;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace RealTimePPDisplayer.Displayer
{
    class MmfDisplayer : DisplayerBase
    {
        private string _mmfName;
        public string MmfName
        {
            get => _mmfName;
            set => SetMmfName(value);
        }

        private char[] _ppBuffer = new char[1024];
        private char[] _hitBuffer = new char[1024];
        private int _hitStrLen;
        private MemoryMappedFile[] _mmfs=new MemoryMappedFile[2];

        private bool _output;

        private bool _split;

        private FormatterBase ppFormatter;
        private FormatterBase hitCountFormatter;
        private ConcurrentDictionary<FormatArgs, IAstNode> astDict = new ConcurrentDictionary<FormatArgs, IAstNode>();

        public MmfDisplayer(
            int? id,
            string name,
            FormatterBase ppfmt,
            FormatterBase hitfmt ,
            bool split = false):base(id)
        {
            ppFormatter = ppfmt;
            hitCountFormatter = hitfmt;
            Initialize(id, name, split);
        }

        public MmfDisplayer(int? id,string name, bool split = false):base(id)
        {
            ppFormatter = RtppFormatter.GetPPFormatter();
            hitCountFormatter = RtppFormatter.GetHitCountFormatter();

            Initialize(id, name, split);
        }

        private void Initialize(int? id,string name,bool split)
        {
            _init = false;
            _output = false;
            _split = split;
            if (ppFormatter != null)
                ppFormatter.Displayer = this;
            if (hitCountFormatter != null)
                hitCountFormatter.Displayer = this;
            SetMmfName(name);
        }

        private void SetMmfName(string name)
        {
            _mmfName = Id == null ? $"{name}" : $"{name}{Id}";
            if (_split)
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

            if (ppFormatter is IFormatterClearable ppfmt)
                ppfmt.Clear();

            if (hitCountFormatter is IFormatterClearable hitfmt)
                hitfmt.Clear();

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
                if(_split)
                {
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, $"{_mmfName}-pp"), ConsoleColor.DarkGreen);
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, $"{_mmfName}-hit"), ConsoleColor.DarkGreen);
                }
                else
                    Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.MMF_MODE_OUTPUT_PATH_FORMAT, _mmfName), ConsoleColor.DarkGreen);
                _init = true;
            }

            _output = true;
            if (hitCountFormatter != null)
            {
                var s = hitCountFormatter.GetFormattedString();
                _hitStrLen = s.Length;
                s.CopyTo(0, _hitBuffer, 0, _hitStrLen);
            }
        }

        public override void FixedDisplay(double time)
        {
            if (!_output) return;

            StreamWriter[] streamWriters = new StreamWriter[2];
            if (_split)
            {
                streamWriters[0] = new StreamWriter(_mmfs[0].CreateViewStream());
                streamWriters[1] = new StreamWriter(_mmfs[1].CreateViewStream());
            }
            else
            {
                streamWriters[0] = new StreamWriter(_mmfs[0].CreateViewStream());
                streamWriters[1] = streamWriters[0];
            }

            if (ppFormatter!=null && ppFormatter.GetFormattedString() is string s)
            {
                int len = s.Length;
                s.CopyTo(0, _ppBuffer, 0, len);

                streamWriters[0].Write(_ppBuffer, 0, len);
                streamWriters[0].Write(!_split ? '\n' : '\0');
            }

            streamWriters[1].Write(_hitBuffer, 0, _hitStrLen);
            streamWriters[1].Write('\0');

            for (int i = 0; i < _mmfs.Length; i++)
                if (_mmfs[i] != null)
                    streamWriters[i].Dispose();
        }

        public override void OnDestroy()
        {
            Clear();
            foreach (var mmf in _mmfs)
            {
                mmf?.Dispose();
            }
        }
    }
}
