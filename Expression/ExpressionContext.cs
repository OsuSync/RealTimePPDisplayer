using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Expression
{
    class ExpressionContext
    {
        public ConcurrentDictionary<string, double> Variables { get; } = new ConcurrentDictionary<string, double>();
        public ConcurrentDictionary<string, Func<List<double>, double>> Functions = new ConcurrentDictionary<string, Func<List<double>, double>>();

        public ExpressionContext()
        {
            Variables["pi"] = Math.PI;
            Variables["e"] = Math.E;

            Functions["sin"] = (args) => Math.Sin(args[0]);
            Functions["cos"] = (args) => Math.Cos(args[0]);
            Functions["tan"] = (args) => Math.Tan(args[0]);
            Functions["asin"] = (args) => Math.Asin(args[0]);
            Functions["acos"] = (args) => Math.Acos(args[0]);
            Functions["atan"] = (args) => Math.Atan(args[0]);
            Functions["pow"] = (args) => Math.Pow(args[0], args[1]);
            Functions["sqrt"] = (args) => Math.Sqrt(args[0]);
            Functions["abs"] = (args) => Math.Abs(args[0]);
            Functions["max"] = (args) => Math.Max(args[0], args[1]);
            Functions["min"] = (args) => Math.Min(args[0], args[1]);
            Functions["exp"] = (args) => Math.Exp(args[0]);
            Functions["log"] = (args) => Math.Log(args[0]);
            Functions["log10"] = (args) => Math.Log10(args[0]);
            Functions["floor"] = (args) => Math.Floor(args[0]);
            Functions["ceil"] = (args) => Math.Ceiling(args[0]);
            Functions["round"] = (args) => Math.Round(args[0],(int)args[1], MidpointRounding.AwayFromZero);
            Functions["sign"] = (args) => Math.Sign(args[0]);
            Functions["truncate"] = (args) => Math.Truncate(args[0]);
            Functions["clamp"] = (args) => Math.Max(Math.Min(args[0], args[2]),args[1]);
            Functions["lerp"] = (args) => (1-args[2])*args[0]+args[2]*args[1];
        }

        public double ExecAst(IAstNode root)
        {
            switch (root)
            {
                case AstNumberNode numberNode:
                    return numberNode.Number;
                case AstVariableNode varNode:
                    if (Variables.TryGetValue(varNode.Id, out var val))
                    {
                        return val;
                    }
                    else
                    {
                        Sync.Tools.IO.CurrentIO.WriteColor($"No Variable found. Variable: { varNode.Id }",ConsoleColor.Yellow);
                        return 0;
                    }

                case AstOpNode opNode:
                    switch (opNode.Op)
                    {
                        case "+":
                            return ExecAst(opNode.LNode) + ExecAst(opNode.RNode);
                        case "-":
                            return ExecAst(opNode.LNode) - ExecAst(opNode.RNode);
                        case "*":
                            return ExecAst(opNode.LNode) * ExecAst(opNode.RNode);
                        case "/":
                            return ExecAst(opNode.LNode) / ExecAst(opNode.RNode);
                        case "^":
                            return Math.Pow(ExecAst(opNode.LNode), ExecAst(opNode.RNode));
                    }
                    break;

                case AstFunctionNode funcNode:
                    try
                    {
                        if (funcNode.Id == "set")
                        {
                            AstVariableNode varNode = funcNode.Args[0] as AstVariableNode;
                            string varName = varNode?.Id ?? throw new ExpressionException("The first parameter is the variable name.");

                            double varVal = ExecAst(funcNode.Args[1]);
                            Variables[varName] = varVal;
                            return 0;
                        }
                        else
                        {
                            if (Functions.TryGetValue(funcNode.Id, out var func))
                            {
                                return func(ComputeArgs(funcNode.Args));
                            }
                            else
                            {
                                throw new ExpressionException($"No function found. Fucntion: {funcNode.Id}");
                            }
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        throw new ExpressionException($"The function is missing a parameter. Fucntion: {funcNode.Id}");
                    }
            }
            return Double.NaN;
        }

        private List<double> ComputeArgs(List<IAstNode> argsNodes)
        {
            List<double> args = new List<double>();
            foreach (var argsNode in argsNodes)
            {
                double val = ExecAst(argsNode);
                args.Add(val);
            }

            return args;
        }
    }
}
