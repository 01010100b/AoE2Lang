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
        private static List<string> INTRINSICS => GetInstrinsics().Select(i => i.Name).ToList();

        public static List<Function> GetInstrinsics()
        {
            var int_type = Types.BuiltinTypes.Single(t => t.Name == "int");

            var intrinsics = new List<Function>();

            var intr = new Function()
            {
                ReturnType = int_type,
                Name = "_push",
                Parameters = new List<Variable>() { new Variable() { Type = int_type, Name = "e" } }
            };
            intrinsics.Add(intr);

            intr = new Function()
            {
                ReturnType = int_type,
                Name = "_pop",
                Parameters = new List<Variable>()
            };
            intrinsics.Add(intr);

            return intrinsics;
        }

        public int RegistersUsed { get; private set; } = 0;

        public void Compile(Script script)
        {
            script.Functions.RemoveAll(f => INTRINSICS.Contains(f.Name));

            AssignRegisters(script);

            foreach (var function in script.Functions)
            {
                CompileBlock(function.Block);
            }
        }

        private void AssignRegisters(Script script)
        {
            var next_register = 0;

            foreach (var global in script.GlobalVariables)
            {
                global.Register = next_register;
                next_register++;

                RegistersUsed = Math.Max(RegistersUsed, next_register);
            }

            foreach (var function in script.Functions)
            {
                foreach (var parameter in function.Parameters)
                {
                    parameter.Register = next_register;
                    next_register++;

                    RegistersUsed = Math.Max(RegistersUsed, next_register);
                }

                AssignRegisters(function.Block, next_register);
            }
        }

        private void AssignRegisters(Block block, int next_register)
        {
            foreach (var local in block.LocalVariables)
            {
                local.Register = next_register;
                next_register++;

                RegistersUsed = Math.Max(RegistersUsed, next_register);
            }

            foreach (var element in block.Elements)
            {
                if (element is Block b)
                {
                    AssignRegisters(b, next_register);
                }
            }
        }

        private void CompileBlock(Block block)
        {
            for (int i = 0; i < block.Elements.Count; i++)
            {
                if (block.Elements[i] is CallStatement call)
                {
                    Log.Debug("call: " + call.Function.Name);

                    if (INTRINSICS.Contains(call.Function.Name))
                    {
                        var rules = CompileIntrinsic(call);

                        block.Elements.RemoveAt(i);
                        block.Elements.InsertRange(i, rules);

                        i += rules.Count - 1;
                    }
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
