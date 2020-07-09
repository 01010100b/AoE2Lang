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
    }
}
