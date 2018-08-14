///use recursive descent
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Expression
{
    /// <summary>
    /// E1 -> E1 + E2
    /// E1 -> E1 - E2
    ///
    /// E2 -> E2 * E3
    /// E2 -> E2 / E3
    ///
    /// E3 -> E3 ^ E4
    ///
    /// E4 -> E5 | -E5 | +E5
    /// 
    /// E5 -> (E1) | F | Func
    ///
    /// Func -> Id(Args)
    /// Args -> Args , Args
    /// Args -> E1
    /// Args -> ε
    ///
    /// F -> Id | Number
    /// </summary>
    class ExpressionParser
    {
        private List<Token> _tokens;
        private int _pos;

        public ExpressionParser()
        {
            _pos = 0;
        }

        public IAstNode Parse(string expr)
        {
            _tokens = new List<Token>();
            var sr = new StringReader(expr);

            Token token;
            do
            {
                token = sr.ReadToken();
                if (token.Type == TokenType.Unknown)
                {
                    throw new ExprssionTokenException($"Unkown Token. Data: {token.Data}");
                }
                _tokens.Add(token);
            } while (token.Type != TokenType.Eof);

            var root = E1();
            if (LookToken().Type == TokenType.Eof)
            {
                _tokens = null;
                return root;
            }

            _tokens = null;
            return null;
        }

        private Token GetToken()
        {
            if (_pos >= _tokens.Count) return new Token(TokenType.Eof);
            Token token = _tokens[_pos++];
            return token;
        }

        private Token LookToken()
        {
            if (_pos >= _tokens.Count) return new Token(TokenType.Eof);
            Token token = _tokens[_pos];
            return token;
        }

        private IAstNode E1()
        {
            IAstNode expr = E2();
            if (expr != null)
            {
                while ((LookToken().Type == TokenType.Op && LookToken().Data == "+") ||
                       (LookToken().Type == TokenType.Op && LookToken().Data == "-"))
                {
                    Token token = GetToken();
                    AstOpNode op = new AstOpNode(token.Data)
                    {
                        LNode = expr,
                        RNode = E2()
                    };
                    if (op.RNode != null)
                        expr = op;
                }
            }

            return expr;
        }

        private IAstNode E2()
        {
            IAstNode expr = E3();
            if (expr != null)
            {
                while ((LookToken().Type == TokenType.Op && LookToken().Data == "*") ||
                       (LookToken().Type == TokenType.Op && LookToken().Data == "/"))
                {
                    Token token = GetToken();
                    AstOpNode op = new AstOpNode(token.Data)
                    {
                        LNode = expr,
                        RNode = E3()
                    };
                    if (op.RNode != null)
                        expr = op;
                }
            }

            return expr;
        }

        private IAstNode E3()
        {
            IAstNode expr = E4();
            if (expr != null)
            {
                while ((LookToken().Type == TokenType.Op && LookToken().Data == "^"))
                {
                    Token token = GetToken();
                    AstOpNode op = new AstOpNode(token.Data)
                    {
                        LNode = expr,
                        RNode = E4()
                    };
                    if (op.RNode != null)
                        expr = op;
                }
            }

            return expr;
        }

        private IAstNode E4()
        {
            int oldPos = _pos;
            Token token = GetToken();
            if (token.Type == TokenType.Op && (token.Data == "-" || token.Data == "+"))
            {
                return new AstOpNode(token.Data)
                {
                    LNode = new AstNumberNode(0),
                    RNode = E5()
                };
            }

            _pos = oldPos;

            IAstNode node = E5();
            if (node != null)
                return node;

            _pos = oldPos;
            return null;
        }

        private IAstNode E5()
        {
            int oldPos = _pos;
            Token token = GetToken();
            IAstNode node;

            if (token.Type == TokenType.Op && token.Data == "(")
            {
                node = E1();
                if (node!=null)
                {
                    token = GetToken();
                    if (token.Type == TokenType.Op && token.Data == ")")
                    {
                        return node;
                    }
                }
            }
            _pos = oldPos;

            {
                node = Func();
                if (node != null)
                {
                    return node;
                }
            }

            return F();
        }

        private IAstNode Func()
        {
            int oldPos = _pos;
            Token token = GetToken();

            if (token.Type == TokenType.Id)
            {
                string id = token.Data;
                token = GetToken();
                if (token.Type == TokenType.Op && token.Data == "(")
                {
                    List<IAstNode> nodes = FuncArgs();
                    if (nodes != null)
                    {
                        token= GetToken();
                        if (token.Type == TokenType.Op && token.Data == ")")
                        {
                            return new AstFunctionNode(id,nodes);
                        }
                    }
                }
            }
            _pos = oldPos;

            return null;
        }

        private List<IAstNode> FuncArgs()
        {
            {
                IAstNode node = E1();
                if (node != null)
                {
                    List<IAstNode> args = new List<IAstNode>();
                    args.Add(node);
                    while (LookToken().Type == TokenType.Op && LookToken().Data == ",")
                    {
                        GetToken();
                        IAstNode expr = E1();
                        if(expr!=null)
                            args.Add(expr);
                    }

                    return args;
                }
            }

            return new List<IAstNode>();
        }

        private IAstNode F()
        {
            int oldPos = _pos;
            Token token = GetToken();
            if (token.Type == TokenType.Id)
            {
                return new AstVariableNode(token.Data);
            }
            else if (token.Type == TokenType.Number)
            {
                return new AstNumberNode(token.Number);
            }

            _pos = oldPos;
            return null;
        }
    }
}
