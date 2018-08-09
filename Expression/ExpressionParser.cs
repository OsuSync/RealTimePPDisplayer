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
        List<Token> _tokens = new List<Token>();
        private int _pos = 0;

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

            IAstNode root;
            root = E1();
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
            int old_pos = _pos;
            Token token = GetToken();

            if ((token.Type == TokenType.Op && token.Data == "+")||
                (token.Type == TokenType.Op && token.Data == "-"))
            {
                AstOpNode op = new AstOpNode(token.Data);
                IAstNode e2node = E2();

                if (e2node!= null)
                {
                    AstOpNode rnode = E1P(node);
                    if (rnode != null)
                    {
                        rnode.LNode = op;
                        op.LNode = node;
                        op.RNode = e2node;
                        return rnode;
                    }
                    else
                    {
                        op.LNode = node;
                        op.RNode = e2node;
                        return op;
                    }
                }
            }
            _pos = old_pos;

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
            int old_pos = _pos;
            Token token = GetToken();

            {
                if ((token.Type == TokenType.Op && token.Data == "*")||
                    (token.Type == TokenType.Op && token.Data == "/"))
                {
                    AstOpNode op = new AstOpNode(token.Data);
                    IAstNode e3node = E3();

                    if (e3node != null)
                    {
                        AstOpNode rnode = E2P(node);
                        if (rnode != null)
                        {
                            rnode.LNode = op;
                            op.LNode = node;
                            op.RNode = e3node;
                            return rnode;
                        }
                        else
                        {
                            op.LNode = node;
                            op.RNode = e3node;
                            return op;
                        }
                    }
                }
            }
            _pos = old_pos;

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
            int old_pos = _pos;

            {
                Token token = GetToken();

                if (token.Type == TokenType.Op && token.Data == "^")
                {
                    AstOpNode op = new AstOpNode(token.Data);
                    IAstNode e4node = E4();

                    if (e4node != null)
                    {
                        AstOpNode rnode = E3P(node);
                        if (rnode != null)
                        {
                            rnode.LNode = op;
                            op.LNode = node;
                            op.RNode = e4node;
                            return rnode;
                        }
                        else
                        {
                            op.LNode = node;
                            op.RNode = e4node;
                            return op;
                        }
                    }

                }
            }
            _pos = old_pos;

            return null;
        }

        private IAstNode E4()
        {
            int old_pos = _pos;
            Token token = GetToken();
            IAstNode node = null;

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
            _pos = old_pos;

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
            int old_pos = _pos;
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
            _pos = old_pos;

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
                    List<IAstNode> args=new List<IAstNode>();
                    args.Add(node);
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
            int old_pos = _pos;
            {
                Token token = GetToken();
                AstFuncArgsTempNode argsNode = null;

                if (token.Type == TokenType.Op && token.Data == ",")
                {
                    IAstNode node = E1();
                    if (node != null)
                    {
                        AstFuncArgsTempNode temp = FuncArgsP();
                        argsNode = new AstFuncArgsTempNode(node, temp);
                        return argsNode;
                    }

                }

                _pos = old_pos;
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
            int old_pos = _pos;
            Token token = GetToken();
            if (token.Type == TokenType.Id)
            {
                return new AstVariableNode(token.Data);
            }
            else if (token.Type == TokenType.Number)
            {
                return new AstNumberNode(token.Number);
            }

            _pos = old_pos;
            return null;
        }
    }
}
