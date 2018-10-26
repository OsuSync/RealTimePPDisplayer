using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Expression
{
    class ExprssionTokenException : Exception
    {
        public ExprssionTokenException(string msg):base(msg)
        {
            
        }
    }

    static class TokenHelper
    {
        private static readonly char[] s_opSet = new[] {'+', '-', '*', '/', '^', '(', ')', ',', '%','<','>','!'};
        private static readonly string[] s_opSetTwoBytes = new[] {"<=",">=","==","!=","&&","||"};
        private const char EOF = (char)0xffff;

        private static char GetChar(this char[] chars, int pos)
        {
            if (pos >= chars.Length) return EOF;
            return chars[pos];
        }

        public static Token ReadToken(char[] chars,ref int pos)
        {
            StringBuilder sb = new StringBuilder();
            char ch;
            do
            {
                ch = chars.GetChar(pos);
                if (ch == ' ')
                    pos++;
            } while (ch == ' ');
            if (ch == EOF) return new Token(TokenType.EOF);

            int nextPos = pos + 1;
            string op = s_opSetTwoBytes.FirstOrDefault(_op => _op[0] == ch && _op[1]== chars.GetChar(nextPos));
            if (op != null)
            {
                pos += 2;
                return new Token(TokenType.Op, op);
            }

            if (s_opSet.Contains(ch))
            {
                pos++;
                return new Token(TokenType.Op, ch.ToString());
            }

            if (ch >= '0' && ch <= '9' || ch == '.')
            {
                bool dotExist = false;
                do
                {
                    if (ch == '.')
                        dotExist = true;
                    sb.Append(ch);
                    pos++;
                    ch = chars.GetChar(pos);
                } while ((ch >= '0' && ch <= '9' || (ch == '.' && !dotExist)));
                return new Token(TokenType.Number, sb.ToString());
            }

            if (ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
            {
                do
                {
                    sb.Append(ch);
                    pos++;
                    ch = chars.GetChar(pos);
                } while (ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch >= '0' && ch <= '9');

                string id = sb.ToString();
                return new Token(TokenType.Id, id);
            }

            return new Token(TokenType.Unknown);
        }
    }

    enum TokenType
    {
        Op,
        Id,
        Number,
        EOF,
        Unknown
    }

    class Token
    {
        public TokenType Type { get; }
        public string Data { get; }
        public double Number { get; }

        public Token(TokenType type, string data = null)
        {
            Type = type;
            Data = data;
            switch (type)
            {
                case TokenType.Number:
                    if (double.TryParse(data,NumberStyles.Float,CultureInfo.InvariantCulture,out double dval))
                    {
                        Number = dval;
                    }
                    else
                    {
                        throw new ExprssionTokenException($"The number does not match the rules. Data: {data}");
                    }
                    break;
            }
        }

        public override string ToString()
        {
            return $"Type: {Type}  Data: {Data}";
        }
    }
}
