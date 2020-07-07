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
            public string ResultName { get; set; }
            public string FunctionName { get; set; }
            public List<string> ParameterNames { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(ResultName + " " + FunctionName + " (");
                foreach (var p in ParameterNames)
                {
                    sb.Append(p + ", ");
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
