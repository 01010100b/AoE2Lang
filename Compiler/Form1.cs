using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Compiler.BuildOrderGenerator;
using static Compiler.CounterGenerator;
using static Compiler.Lang.Statements;

namespace Compiler
{
    public partial class Form1 : Form
    {
        private class OldStrategy
        {
            public readonly List<Counter> Counters = new List<Counter>();
            public readonly Dictionary<Unit, OldBuildOrder> BuildOrders = new Dictionary<Unit, OldBuildOrder>();

        }
        public Form1()
        {
            InitializeComponent();
        }

        private void ButtonCompile_Click(object sender, EventArgs e)
        {
            ButtonCompile.Enabled = false;
            Refresh();

            Compile(true);

            ButtonCompile.Enabled = true;
        }

        private void ButtonTest_Click(object sender, EventArgs e)
        {
            ButtonTest.Enabled = false;
            Refresh();

            Compile(false);

            ButtonTest.Enabled = true;
        }

        private void Compile(bool parallel)
        {
            var settings = new Settings();

            var sw = new Stopwatch();
            sw.Start();

            var mod = new Mod();
            mod.Load(settings.DatFile);

            var sb = new StringBuilder();

            sb.AppendLine(";---- Auto generated ----");
            sb.AppendLine("");

            sb.AppendLine(";region Auto Counters");
            sb.AppendLine("var gl-target-count = 0");
            sb.AppendLine("var gl-current-count = 0");

            var enemies = mod.Civilizations.SelectMany(c => c.TrainableUnits).Where(u => u.Land && u.Class != UnitClass.Civilian).Distinct().ToList();

            foreach (var unit in enemies.OrderBy(u => u.Id))
            {
                sb.AppendLine("(defrule");
                sb.AppendLine($"\t(strategic-number sn-auto-counters == YES)");
                sb.AppendLine("=>");
                sb.AppendLine($"\t(up-get-target-fact unit-type-count {unit.Id} gl-current-count)");
                sb.AppendLine(")");

                sb.AppendLine("(defrule");
                sb.AppendLine($"\t(strategic-number sn-auto-counters == YES)");
                sb.AppendLine($"\t(up-compare-goal gl-current-count g:> gl-target-count)");
                sb.AppendLine($"\t(up-compare-goal gl-current-count c:>= 3)");
                sb.AppendLine("=>");
                sb.AppendLine($"\t(set-strategic-number sn-target-unit {unit.BaseUnit.Id})");
                sb.AppendLine($"\t(up-modify-goal gl-target-count g:= gl-current-count)");
                sb.AppendLine(")");
            }

            sb.AppendLine(";endregion");

            Dictionary<Civilization, Strategy> strategies = new Dictionary<Civilization, Strategy>();

            if (parallel)
            {
                Parallel.ForEach(mod.Civilizations.Where(c => c.Id > 0), civ =>
                {
                    Log.Debug($"starting civ {civ.Id} {civ.Name}");

                    var strat = new Strategy(mod, civ);
                    strat.Generate();

                    lock (strategies)
                    {
                        strategies.Add(civ, strat);
                    }
                });
            }
            else
            {
                foreach (var civ in mod.Civilizations.Where(c => c.Id > 0))
                {
                    Log.Debug($"starting civ {civ.Id} {civ.Name}");

                    var strat = new Strategy(mod, civ);
                    strat.Generate();

                    lock (strategies)
                    {
                        strategies.Add(civ, strat);
                    }

                    foreach (var bo in strat.BuildOrders.Values)
                    {
                        var current = bo.Primary;

                        Log.Debug("");
                        Log.Debug($"found bo for {current.Id} {current.Name} with {bo.Elements.Count} elements");
                        foreach (var be in bo.Elements)
                        {
                            Log.Debug(be.ToString());
                        }
                    }

                    
                }
            }

            foreach (var strat in strategies)
            {
                sb.AppendLine(strat.Value.Compile());
            }

            var name = Directory.GetFiles(settings.SourceFolder).Single(f => Path.GetExtension(f) == ".ai");
            name = Path.GetFileNameWithoutExtension(name);
            var folder = Path.Combine(settings.SourceFolder, name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, "Strategies.per");
            File.WriteAllText(file, sb.ToString());

            Log.Debug("compiling");

            var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old", "Script");
            if (Directory.Exists(script))
            {
                Directory.Delete(script, true);
            }

            Program.CopyFolder(settings.SourceFolder, script);

            var comp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old", "Binary.exe");
            var process = Process.Start(comp);
            process.WaitForExit();

            script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old", "ParsedScript");
            Program.CopyFolder(script, settings.AiFolder);

            Log.Debug("done");

            sw.Stop();
            Log.Debug("time: " + sw.Elapsed.TotalSeconds);
        }
    }
}
