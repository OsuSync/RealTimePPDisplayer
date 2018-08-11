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
        private static readonly char[] _opSet = new[] { '+', '-', '*', '/', '^', '(', ')' ,','};

        public static Token ReadToken(this StringReader sr)
        {
            StringBuilder sb = new StringBuilder();
            int ch;
            do
            {
                ch = sr.Peek();
                if (ch == ' ')
                    sr.Read();
            } while (ch == ' ');

            if (ch == -1) return new Token(TokenType.Eof);


            if (_opSet.Contains((char)ch))
            {
                return new Token(TokenType.Op, ((char)sr.Read()).ToString());
            }

            if (ch >= '0' && ch <= '9' || ch == '.')
            {
                bool dotExist = false;
                do
                {
                    if (ch == '.')
                        dotExist = true;
                    sb.Append((char)sr.Read());
                    ch = sr.Peek();
                } while ((ch >= '0' && ch <= '9' || (ch == '.' && !dotExist)));
                return new Token(TokenType.Number, sb.ToString());
            }

            if (ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
            {
                do
                {
                    sb.Append((char)sr.Read());
                    ch = sr.Peek();
                } while (ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch >= '0' && ch <= '9');
                return new Token(TokenType.Id, sb.ToString());
            }

            return new Token(TokenType.Unknown);
        }
    }

    enum TokenType
    {
        Op,
        Id,
        Number,
        Eof,
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
