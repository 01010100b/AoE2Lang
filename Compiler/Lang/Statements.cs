using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using static Compiler.Lang.Block;

namespace Compiler.Lang
{
    static class Statements
    {
        public abstract class Statement : IBlockElement
        {

        }

        public class CallStatement : Statement
        {
            public Variable Result { get; set; }
            public Function Function { get; set; }
            public List<Variable> Parameters { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(Result.Name + " " + Function.Name + " (");
                foreach (var p in Parameters)
                {
                    sb.Append(p.Name + ", ");
                }
                sb.Append(")");

                return sb.ToString();
            }
        }

        public class RuleStatement : Statement
        {
            public string Rule { get; set; }

            public override string ToString()
            {
                return Rule;
            }
        }
    }
}
