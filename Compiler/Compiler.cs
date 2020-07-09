using Compiler.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Compiler.Lang.Statements;

namespace Compiler
{
    class Compiler
    {
        public readonly string[] INTRINSICS = { "_push", "_pop" };

        public void Compile(Script script)
        {
            foreach (var function in script.Functions)
            {
                CompileBlock(function.Block);
            }
        }

        private void CompileBlock(Block block)
        {
            for (int i = 0; i < block.Elements.Count; i++)
            {
                if (block.Elements[i] is CallStatement call)
                {
                    Log.Debug("intr: " + call.Function.Name);
                    var rules = new List<RuleStatement>();

                    if (INTRINSICS.Contains(call.Function.Name))
                    {
                        
                        rules = CompileIntrinsic(call);
                    }

                    block.Elements.RemoveAt(i);
                    block.Elements.InsertRange(i, rules);

                    i += rules.Count - 1;
                }
            }
        }

        private List<RuleStatement> CompileIntrinsic(CallStatement call)
        {
            if (call.Function.Name == "_push")
            {
                var rule = "(defrule\n\t(true)\n=>";
                rule += $"\n\t(up-set-indirect-goal g: stack-pointer g: {call.Parameters[0].Name})";
                rule += $"\n\t(up-modify-goal stack-pointer c:+ 1)";
                rule += "\n)";

                return new List<RuleStatement>() { new RuleStatement() { Rule = rule } };
            }
            else if (call.Function.Name == "_pop")
            {
                var rule = "(defrule\n\t(true)\n=>";
                rule += $"\n\t(up-modify-goal stack-pointer c:- 1)";
                rule += $"\n\t(up-get-indirect-goal g: stack-pointer g: {call.Result.Name})";
                rule += "\n)";

                return new List<RuleStatement>() { new RuleStatement() { Rule = rule } };
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
