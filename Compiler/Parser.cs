using Compiler.Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms.VisualStyles;
using static Compiler.Lang.Statements;

namespace Compiler
{
    class Parser
    {
        public class ParserException : Exception
        {
            public readonly int Line;

            public ParserException(int line, string message) : base(message)
            {
                Line = line;
            }
        }

        public static Regex GetStatementRegex(string pattern)
        {
            return new Regex(@"^\s*" + pattern + @"\s*$");
        }

        private const string NamePattern = @"(\b[_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ]+([0123456789_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ])*)";
        private string TypePattern = null;
        private string VariableDeclarationPattern => @"(?<Type>\b" + TypePattern + @")\s+\b(?<Name>" + NamePattern + ")";
        private string FunctionDeclarationPattern => @"(?<ReturnType>\b" + TypePattern + @")\s+\b(?<Name>" + NamePattern + @")\s*(?<Params>\(.*\))";
        private string FunctionCallPattern => @"(?<Result>\b" + NamePattern + @")\s*=\s*\b(?<Name>" + NamePattern + @")\s*(?<Params>\(.*\))";
        public Script Parse(string source)
        {
            if (source == null)
            {
                throw new Exception("source is null");
            }

            var lines = source.Split('\n').Where(l => l != "\r").ToList();
            var script = new Script();
            
            var types = Types.BuiltinTypes;
            // TODO user-defined types

            TypePattern = "(" + types[0].Name;
            for (int i = 1; i < types.Count; i++)
            {
                TypePattern += "|" + types[i].Name;
            }
            TypePattern += ")";

            Log.Debug("type regex: " + TypePattern);

            var variable_regex = GetStatementRegex(VariableDeclarationPattern);
            var function_regex = GetStatementRegex(FunctionDeclarationPattern);
            var call_regex = GetStatementRegex(FunctionCallPattern);

            var blocks = new Stack<Block>();
            Block current_block = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (variable_regex.IsMatch(line))
                {
                    var m = variable_regex.Match(line);
                    Log.Debug("decl: " + line);

                    var variable = new Variable()
                    {
                        Type = types.Single(t => t.Name == m.Groups["Type"].Value),
                        Name = m.Groups["Name"].Value
                    };

                    if (current_block == null)
                    {
                        script.GlobalVariables.Add(variable);
                    }
                    else
                    {
                        current_block.LocalVariables.Add(variable);
                    }
                    
                }
                else if (function_regex.IsMatch(line))
                {
                    var m = function_regex.Match(line);
                    Log.Debug("func: " + line);

                    current_block = new Block();
                    var function = new Function()
                    {
                        ReturnType = types.Single(t => t.Name == m.Groups["ReturnType"].Value),
                        Name = m.Groups["Name"].Value,
                        Parameters = new List<Variable>(),
                        Block = current_block
                    };

                    var pars = m.Groups["Params"].Value.Replace("(", "").Replace(")", "").Split(',');
                    foreach (var par in pars.Select(p => p.Trim()).Where(p => p.Length > 0))
                    {
                        if (variable_regex.IsMatch(par))
                        {
                            var pm = variable_regex.Match(par);
                            var parameter = new Variable()
                            {
                                Type = types.Single(t => t.Name == pm.Groups["Type"].Value),
                                Name = pm.Groups["Name"].Value
                            };

                            function.Parameters.Add(parameter);
                        }
                        else
                        {
                            throw new ParserException(i, "Function parameter error: " + par);
                        }
                    }

                    script.Functions.Add(function);
                }
                else if (call_regex.IsMatch(line))
                {
                    var m = call_regex.Match(line);
                    Log.Debug("call: " + line);

                    var call = new CallStatement()
                    {
                        ResultName = m.Groups["Result"].Value,
                        FunctionName = m.Groups["Name"].Value,
                        ParameterNames = new List<string>()
                    };

                    var pars = m.Groups["Params"].Value.Replace("(", "").Replace(")", "").Split(',');
                    foreach (var par in pars.Select(p => p.Trim()).Where(p => p.Length > 0))
                    {
                        if (Regex.IsMatch(par, NamePattern))
                        {
                            call.ParameterNames.Add(par);
                        }
                    }

                    current_block.Elements.Add(call);
                }
            }

            return script;
        }
    }
}
