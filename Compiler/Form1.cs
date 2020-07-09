using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static Compiler.Lang.Statements;

namespace Compiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ButtonCompile_Click(object sender, EventArgs e)
        {
            var sources = new Dictionary<string, string>();

            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "example.txt");
            var source = File.ReadAllText(file);
            sources.Add(file, source);

            var parser = new Parser();
            var script = parser.Parse(sources);
            var compiler = new Compiler();
            compiler.Compile(script);

            Log.Debug("");
            Log.Debug("--- OUTPUT ---");
            Log.Debug("");
            
            Log.Debug("Global Variables:");
            foreach (var v in script.GlobalVariables)
            {
                Log.Debug($"{v.Type.Name} {v.Name}");
            }
            Log.Debug("");

            Log.Debug("Functions:");
            foreach (var f in script.Functions)
            {
                Log.Debug($"{f.ReturnType.Name} {f.Name} with {f.Parameters.Count} parameters and {f.Block.Elements.Count} statements");

                foreach (var statement in f.Block.Elements.Where(s => s is Statement).Cast<Statement>())
                {
                    Log.Debug(statement.ToString());
                }
            }

            

        }
    }
}
