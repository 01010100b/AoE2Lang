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
            public ParserException(string file, int line, string message) : base("Error in file " + file + " at line " + line + ": " + message) { }
        }

        public static Regex GetStatementRegex(string pattern)
        {
            return new Regex(@"^\s*" + pattern + @"\s*$");
        }

        private readonly Dictionary<string, Types.Type> DefinedTypes = new Dictionary<string, Types.Type>();
        private readonly Dictionary<string, Function> DefinedFunctions = new Dictionary<string, Function>();
        private readonly Dictionary<string, Variable> GlobalVariables = new Dictionary<string, Variable>();

        private string TypePattern = null;
        private string NamePattern => @"(\b[_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ]+([0123456789_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ])*)";
        private string VariableDeclarationPattern => @"(?<Type>\b" + TypePattern + @")\s+\b(?<Name>" + NamePattern + ")";
        private string FunctionDeclarationPattern => @"(?<ReturnType>\b" + TypePattern + @")\s+\b(?<Name>" + NamePattern + @")\s*(?<Params>\(.*\))";
        private string FunctionCallPattern => @"(?<Result>\b" + NamePattern + @")\s*=\s*\b(?<Name>" + NamePattern + @")\s*(?<Params>\(.*\))";
        
        public Script Parse(Dictionary<string, string> source_files)
        {
            DefinedTypes.Clear();
            DefinedFunctions.Clear();
            GlobalVariables.Clear();

            // intrinsic functions

            foreach (var intr in Compiler.GetInstrinsics())
            {
                DefinedFunctions.Add(intr.Name, intr);
            }

            // get types

            foreach (var type in Types.BuiltinTypes)
            {
                DefinedTypes.Add(type.Name, type);
            }

            // TODO user-defined types

            var types = DefinedTypes.Values.ToList();
            TypePattern = "(" + types[0].Name;
            for (int i = 1; i < types.Count; i++)
            {
                TypePattern += "|" + types[i].Name;
            }
            TypePattern += ")";

            Log.Debug("type regex: " + TypePattern);

            // get definitions

            foreach (var kvp in source_files)
            {
                ParseDefinitions(kvp.Key, kvp.Value);
            }

            // parse statements

            foreach (var kvp in source_files)
            {
                ParseStatements(kvp.Key, kvp.Value);
            }

            // output script

            var script = new Script();
            script.GlobalVariables.AddRange(GlobalVariables.Values);
            script.Functions.AddRange(DefinedFunctions.Values);

            return script;
        }

        private void ParseDefinitions(string file, string source)
        {
            var lines = source.Split('\n').Where(l => l != "\r").ToList();
            var variable_regex = GetStatementRegex(VariableDeclarationPattern);
            var function_regex = GetStatementRegex(FunctionDeclarationPattern);

            var height = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (variable_regex.IsMatch(line) && height == 0)
                {
                    var m = variable_regex.Match(line);
                    Log.Debug("decl: " + line);

                    var variable = new Variable()
                    {
                        Type = DefinedTypes[m.Groups["Type"].Value],
                        Name = m.Groups["Name"].Value
                    };

                    GlobalVariables.Add(variable.Name, variable);

                }
                else if (function_regex.IsMatch(line))
                {
                    if (height != 0)
                    {
                        throw new ParserException(file, i, "function defined inside other function: " + line);
                    }

                    var m = function_regex.Match(line);
                    Log.Debug("func: " + line);

                    var function = new Function()
                    {
                        ReturnType = DefinedTypes[m.Groups["ReturnType"].Value],
                        Name = m.Groups["Name"].Value,
                        Parameters = new List<Variable>(),
                        Block = new Block()
                    };

                    var pars = m.Groups["Params"].Value.Replace("(", "").Replace(")", "").Split(',');
                    foreach (var par in pars.Select(p => p.Trim()).Where(p => p.Length > 0))
                    {
                        if (variable_regex.IsMatch(par))
                        {
                            var pm = variable_regex.Match(par);
                            var parameter = new Variable()
                            {
                                Type = DefinedTypes[pm.Groups["Type"].Value],
                                Name = pm.Groups["Name"].Value
                            };

                            function.Parameters.Add(parameter);
                        }
                        else
                        {
                            throw new ParserException(file, i, "Function parameter error: " + par);
                        }
                    }

                    DefinedFunctions.Add(function.Name, function);

                    if (line.Contains("{"))
                    {
                        height++;
                    }
                }
                else if (line == "{")
                {
                    height++;
                }
                else if (line == "}")
                {
                    if (height == 0)
                    {
                        throw new ParserException(file, i, "encountered } without current block");
                    }

                    height--;
                }
            }
        }

        private void ParseStatements(string file, string source)
        {
            var lines = source.Split('\n').Where(l => l != "\r").ToList();

            var variable_regex = GetStatementRegex(VariableDeclarationPattern);
            var function_regex = GetStatementRegex(FunctionDeclarationPattern);
            var call_regex = GetStatementRegex(FunctionCallPattern);

            Function current_function = null;
            Block current_block = null;
            Stack<Block> block_stack = new Stack<Block>();
            bool entered = false;

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
                    
                    var variable = new Variable()
                    {
                        Type = DefinedTypes[m.Groups["Type"].Value],
                        Name = m.Groups["Name"].Value
                    };

                    if (current_block != null && entered == true)
                    {
                        var defined = GlobalVariables.Values.ToList();
                        defined.AddRange(current_function.Parameters);
                        defined.AddRange(current_block.LocalVariables);
                        defined.AddRange(block_stack.SelectMany(b => b.LocalVariables));

                        if (defined.Select(v => v.Name).Contains(variable.Name))
                        {
                            throw new ParserException(file, i, "variable already defined: " + line);
                        }

                        Log.Debug("decl: " + line);
                        current_block.LocalVariables.Add(variable);
                    }
                    else if (!GlobalVariables.ContainsKey(variable.Name))
                    {
                        throw new ParserException(file, i, "variable declaration unexpected: " + line);
                    }
                }
                else if (function_regex.IsMatch(line))
                {
                    var m = function_regex.Match(line);

                    var name = m.Groups["Name"].Value;
                    current_function = DefinedFunctions[name];
                    current_block = current_function.Block;

                    entered = false;

                    if (line.Contains("{"))
                    {
                        entered = true;
                    }
                }
                else if (call_regex.IsMatch(line))
                {
                    if (entered == false)
                    {
                        throw new ParserException(file, i, "unexpected statement: " + line);
                    }

                    var m = call_regex.Match(line);
                    var result_name = m.Groups["Result"].Value;
                    var function_name = m.Groups["Name"].Value;
                    var parameter_names = m.Groups["Params"].Value.Replace("(", "").Replace(")", "").Split(',')
                        .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

                    var variables = GlobalVariables.Values.ToList();
                    variables.AddRange(current_function.Parameters);
                    variables.AddRange(current_block.LocalVariables);
                    variables.AddRange(block_stack.SelectMany(b => b.LocalVariables));

                    var result = variables.Single(v => v.Name == result_name);
                    var function = DefinedFunctions[function_name];

                    var parameters = new List<Variable>();
                    foreach (var pn in parameter_names)
                    {
                        var parameter = variables.FirstOrDefault(v => v.Name == pn);

                        if (parameter == null)
                        {
                            throw new ParserException(file, i, $"parameter {pn} not found: {line}");
                        }

                        parameters.Add(parameter);
                    }

                    var statement = new CallStatement()
                    {
                        Result = result,
                        Function = function,
                        Parameters = parameters
                    };

                    current_block.Elements.Add(statement);
                }
                else if (line == "{")
                {
                    if (entered)
                    {
                        throw new ParserException(file, i, "did not expect {");
                    }

                    entered = true;
                }
                else if (line == "}")
                {
                    if (!entered)
                    {
                        throw new ParserException(file, i, "did not expect }");
                    }

                    if (block_stack.Count > 0)
                    {
                        current_block = block_stack.Pop();
                    }
                    else if (current_function != null)
                    {
                        current_function = null;
                    }
                    else
                    {
                        throw new ParserException(file, i, "did not expect }");
                    }
                }
                else
                {
                    throw new ParserException(file, i, "parser error: " + line);
                }
            }
        }
    }
}
