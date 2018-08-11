using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    class ConsoleDisplayer:DisplayerBase
    {
        private string _lastStrSpace="";
        private bool _play = false;

        public override void Display()
        {
            int startLine = Console.CursorTop;
            if (!_play)
            {
                _play = true;
            }

            string ppStr = base.FormatPp().ToString().Replace("\r\n","\n");
            string hitCountStr = base.FormatHitCount().ToString().Replace("\r\n","\n");

            string finalStr = $"{ppStr}\n\n{hitCountStr}";
            string str = StringSpace(_lastStrSpace, finalStr);

            Console.Write(str);


            Console.CursorLeft = 0;
            Console.CursorTop -= Console.CursorTop - startLine;

            _lastStrSpace = finalStr;
        }

        private Regex _regex = new Regex(@"\S");

        public override void Clear()
        {
            base.Clear();
            _play = false;

            Console.Write(_regex.Replace(_lastStrSpace, " "));

            _lastStrSpace = "";
        }

        private string StringSpace(string odlStr,string newStr)
        {
            if (string.IsNullOrWhiteSpace(odlStr)||
               string.IsNullOrWhiteSpace(newStr)) return string.Empty;
            var oldList = odlStr.Split('\n');
            var newList = newStr.Split('\n');
            for (int i=0;i<oldList.Length;i++)
            {
                int count = oldList[i].Length - newList[i].Length;
                newList[i] += new string(' ',count>0?count:0);
            }

            return string.Join("\n", newList);
        }
    }
}
