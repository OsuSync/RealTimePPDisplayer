using RealTimePPDisplayer.Formatter;
using System;
using System.IO;

namespace RealTimePPDisplayer.Displayer
{
    sealed class TextDisplayer : DisplayerBase
    {
        private readonly char[] _ppBuffer = new char[1024];
        private readonly char[] _hitBuffer = new char[1024];
        private int _ppStrLen;
        private int _hitStrLen;

        private readonly string[] _filenames=new string[2];

        private readonly bool _splited;

        private FormatterBase ppFormatter;
        private FormatterBase hitCountFormatter;

        public TextDisplayer(int? id,string filename,bool splited=false):base(id)
        {
            _splited = splited;

            ppFormatter = RtppFormatter.GetPPFormatter();
            hitCountFormatter = RtppFormatter.GetHitCountFormatter();
            if (ppFormatter != null)
                ppFormatter.Displayer = this;
            if (hitCountFormatter != null)
                hitCountFormatter.Displayer = this;

            if (!Path.IsPathRooted(filename))
                _filenames[0] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            else
                _filenames[0] = filename;

            if (_splited)
            {
                string ext = Path.GetExtension(_filenames[0]);
                string path = Path.GetDirectoryName(_filenames[0]);
                string file = Path.GetFileNameWithoutExtension(_filenames[0]);
                _filenames[0] = $"{path}{Path.DirectorySeparatorChar}{file}-pp{ext}";
                _filenames[1] = $"{path}{Path.DirectorySeparatorChar}{file}-hit{ext}";
            }
            Clear();//Create File
        }

        public override void Clear()
        {
            base.Clear();

            if (ppFormatter is IFormatterClearable ppfmt)
                ppfmt.Clear();

            if (hitCountFormatter is IFormatterClearable hitfmt)
                hitfmt.Clear();

            using (File.Open(_filenames[0], FileMode.Create, FileAccess.Write, FileShare.Read))
                if(_splited)
                    using (File.Open(_filenames[1], FileMode.Create, FileAccess.Write, FileShare.Read)){}
        }
        private bool _init;

        public override void Display()
        {
            if (!_init)
            {
                foreach(var filename in _filenames)
                    if(filename!=null)
                        Sync.Tools.IO.CurrentIO.WriteColor(string.Format(DefaultLanguage.TEXT_MODE_OUTPUT_PATH_FORMAT, filename), ConsoleColor.DarkGreen);
                _init = true;
            }

            string s = ppFormatter.GetFormattedString();
            _ppStrLen = s.Length;
            s.CopyTo(0,_ppBuffer,0, _ppStrLen);

            s = hitCountFormatter.GetFormattedString();
            _hitStrLen = s.Length;
            s.CopyTo(0, _hitBuffer, 0, _hitStrLen);

            StreamWriter[] streamWriters = new StreamWriter[2];

            if (_splited)
            {
                streamWriters[0] = new StreamWriter(File.Open(_filenames[0], FileMode.Create, FileAccess.Write, FileShare.Read));
                streamWriters[1] = new StreamWriter(File.Open(_filenames[1], FileMode.Create, FileAccess.Write, FileShare.Read));
            }
            else
            {
                streamWriters[0] = new StreamWriter(File.Open(_filenames[0], FileMode.Create, FileAccess.Write, FileShare.Read));
                streamWriters[1] = streamWriters[0];
            }

            streamWriters[0].Write(_ppBuffer, 0, _ppStrLen);
            if (!_splited)
                streamWriters[0].Write(Environment.NewLine);

            streamWriters[1].Write(_hitBuffer, 0, _hitStrLen);

            for (int i=0; i < _filenames.Length; i++)
                if(_filenames[i]!=null)
                    streamWriters[i].Dispose();
        }
        public override void OnDestroy()
        {
            Clear();
        }
    }
}
