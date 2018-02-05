using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer.Displayer
{
    public class Expression<T>where T:struct
    {
        public Dictionary<string, T> Data;

        private DataTable m_eval;
        private string m_expr;

        public Expression(string expr)
        {
            m_eval = new DataTable();
            m_expr = expr;
        }

        private object Eval()
        {
            var builder = new StringBuilder(m_expr);
            foreach (var pair in Data)
                builder.Replace(pair.Key, $"{pair.Value:F8}");

            return m_eval.Compute(builder.ToString(), null);
        }

        public int EvalInt()
        {
            return (int)(decimal)Eval();
        }


        public double EvalDouble()
        {
            return (double)(decimal)Eval();
        }
    }
}
