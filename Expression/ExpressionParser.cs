///LL(1) Grammar, but use recursive descent
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Expression
{
    /// <summary>
    /// E1 -> E2E1'
    /// E1' -> + E2E1'
    ///      | - E2E1'
    ///      |ε
    ///
    /// E2 -> E3E2'
    /// E2' -> * E3E2'
    ///      |/ E3E2'
    ///      |ε
    ///
    /// E3 ->E4E3'
    /// E3' -> ^ E4E3'
    ///      |ε
    /// E4 -> (E1) | F | Func
    ///
    /// Func -> Id(Args)
    /// Args -> E1Args'
    ///         |ε
    /// Args' -> ,E1Args'
    ///         | E1
    ///
    /// F -> Id | Number
    /// </summary>
    class ExpressionParser
    {
        private readonly List<Token> _tokens = new List<Token>();
        private int _pos;

        public ExpressionParser()
        {
            _pos = 0;
        }

        public IAstNode Parse(string expr)
        {

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
            if (_tokens[_pos].Type == TokenType.Eof)
            {
                return root;
            }
            return null;
        }

        private Token GetToken()
        {
            Token token = _tokens[_pos++];
            return token;
        }

        private IAstNode E1()
        {
            IAstNode lnode = E2();
            if (lnode!=null)
            {
                AstOpNode rnode = E1P(lnode);
                if (rnode != null)
                {
                    return rnode;
                }
                else
                {
                    return lnode;
                }
            }

            return null;
        }

        public AstOpNode E1P(IAstNode node)
        {
            int oldPos = _pos;
            Token token = GetToken();

            if ((token.Type == TokenType.Op && token.Data == "+")||
                (token.Type == TokenType.Op && token.Data == "-"))
            {
                AstOpNode op = new AstOpNode(token.Data);
                IAstNode e2Node = E2();

                if (e2Node!= null)
                {
                    AstOpNode rnode = E1P(node);
                    if (rnode != null)
                    {
                        rnode.LNode = op;
                        op.LNode = node;
                        op.RNode = e2Node;
                        return rnode;
                    }
                    else
                    {
                        op.LNode = node;
                        op.RNode = e2Node;
                        return op;
                    }
                }
            }
            _pos = oldPos;

            return null;
        }

        private IAstNode E2()
        {
            IAstNode lnode = E3();
            if (lnode != null)
            {
                AstOpNode rnode = E2P(lnode);
                if (rnode != null)
                {
                    return rnode;
                }
                else
                {
                    return lnode;
                }
            }

            return null;
        }

        private AstOpNode E2P(IAstNode node)
        {
            int oldPos = _pos;
            Token token = GetToken();

            {
                if ((token.Type == TokenType.Op && token.Data == "*")||
                    (token.Type == TokenType.Op && token.Data == "/"))
                {
                    AstOpNode op = new AstOpNode(token.Data);
                    IAstNode e3Node = E3();

                    if (e3Node != null)
                    {
                        AstOpNode rnode = E2P(node);
                        if (rnode != null)
                        {
                            rnode.LNode = op;
                            op.LNode = node;
                            op.RNode = e3Node;
                            return rnode;
                        }
                        else
                        {
                            op.LNode = node;
                            op.RNode = e3Node;
                            return op;
                        }
                    }
                }
            }
            _pos = oldPos;

            return null;
        }

        private IAstNode E3()
        {
            IAstNode lnode = E4();
            if (lnode!=null)
            {
                AstOpNode rnode = E3P(lnode);

                if (rnode != null)
                {
                    return rnode;
                }
                else
                {
                    return lnode;
                }
            }

            return null;
        }

        private AstOpNode E3P(IAstNode node)
        {
            int oldPos = _pos;

            {
                Token token = GetToken();

                if (token.Type == TokenType.Op && token.Data == "^")
                {
                    AstOpNode op = new AstOpNode(token.Data);
                    IAstNode e4Node = E4();

                    if (e4Node != null)
                    {
                        AstOpNode rnode = E3P(node);
                        if (rnode != null)
                        {
                            rnode.LNode = op;
                            op.LNode = node;
                            op.RNode = e4Node;
                            return rnode;
                        }
                        else
                        {
                            op.LNode = node;
                            op.RNode = e4Node;
                            return op;
                        }
                    }

                }
            }
            _pos = oldPos;

            return null;
        }

        private IAstNode E4()
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

        class AstFuncArgsTempNode:IAstNode
        {
            public IAstNode Content { get; }
            public AstFuncArgsTempNode Next { get; }

            public AstFuncArgsTempNode(IAstNode content,AstFuncArgsTempNode next)
            {
                Content = content;
                Next = next;
            }
        }


        private List<IAstNode> FuncArgs()
        {
            {
                IAstNode node = E1();
                if (node != null)
                {
                    var argNode = FuncArgsP();
                    List<IAstNode> args = new List<IAstNode> {node};
                    while (argNode != null)
                    {
                        args.Add(argNode.Content);
                        argNode = argNode.Next;
                    }

                    return args;
                }
            }

            return new List<IAstNode>();
        }

        private AstFuncArgsTempNode FuncArgsP()
        {
            int oldPos = _pos;
            {
                Token token = GetToken();

                if (token.Type == TokenType.Op && token.Data == ",")
                {
                    IAstNode node = E1();
                    if (node != null)
                    {
                        AstFuncArgsTempNode temp = FuncArgsP();
                        var argsNode = new AstFuncArgsTempNode(node, temp);
                        return argsNode;
                    }

                }

                _pos = oldPos;
            }

            {
                IAstNode node = E1();
                if (node != null)
                {
                    return new AstFuncArgsTempNode(node,null);
                }
            }

            return null;
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
