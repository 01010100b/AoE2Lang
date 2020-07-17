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
            var sw = new Stopwatch();
            sw.Start();

            ButtonCompile.Enabled = false;
            Refresh();

            var settings = new Settings();

            Log.Debug("creating strategies");
            SetBuildOrders(settings);

            Log.Debug("compiling");
            Compile(settings);

            Log.Debug("done");

            sw.Stop();
            Log.Debug("seconds: " + sw.Elapsed.TotalSeconds);

            ButtonCompile.Enabled = true;
        }

        private void SetBuildOrders(Settings settings)
        {
            var rng = new Random();

            var name = Directory.GetFiles(settings.SourceFolder).Single(f => Path.GetExtension(f) == ".ai");
            name = Path.GetFileNameWithoutExtension(name);
            var folder = Path.Combine(settings.SourceFolder, name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var mod = new Mod();
            mod.Load(settings.DatFile);
            var enemies = mod.Civilizations.SelectMany(c => c.TrainableUnits).Where(u => u.Land && u.Class != UnitClass.Civilian).Distinct().ToList();

            var strategies = new Dictionary<Civilization, OldStrategy>();

            foreach (var civ in mod.Civilizations.Where(c => c.Id != 0))
            {
                Log.Debug("doing civ " + civ.Name + " " + civ.Id);

                var strat = new OldStrategy();

                var units = civ.TrainableUnits
                    .Where(u => u.Land)
                    .Select(u => u.BaseUnit)
                    .Where(u => u.Available || u.TechRequired)
                    .Where(u => u.BuildLocation != null)
                    .Distinct()
                    .OrderBy(u => rng.Next())
                    .ToList();

                var counters = new HashSet<Unit>();

                // get build orders
                foreach (var unit in units)
                {
                    var current = unit;

                    if (current.GetAge(civ) <= 1)
                    {
                        var bestage = 4;
                        foreach (var upgr in unit.UpgradesTo)
                        {
                            var age = upgr.GetAge(civ);
                            if (age > 1 && age < bestage)
                            {
                                current = upgr;
                                bestage = age;
                            }
                        }
                    }
                    
                    if (current.GetAge(civ) <= 1)
                    {
                        continue;
                    }

                    var good = false;
                    switch (current.Class)
                    {
                        case UnitClass.Archer:
                        case UnitClass.Ballista:
                        case UnitClass.Cavalry:
                        case UnitClass.CavalryArcher:
                        case UnitClass.CavalryRaider:
                        case UnitClass.Conquistador:
                        case UnitClass.ElephantArcher:
                        case UnitClass.HandCannoneer:
                        case UnitClass.Infantry:
                        case UnitClass.Monk:
                        case UnitClass.Phalanx:
                        case UnitClass.Pikeman:
                        case UnitClass.Raider:
                        case UnitClass.Scout:
                        //case UnitClass.SiegeWeapon:
                        case UnitClass.Spearman:
                        case UnitClass.TwoHandedSwordsMan:
                        //case UnitClass.PackedUnit:
                        //case UnitClass.UnpackedSiegeUnit:
                        case UnitClass.WarElephant: good = true; break;
                    }

                    if (!good)
                    {
                        continue;
                    }

                    if (current.Id == 74)
                    {
                        Log.Debug("id 74 age " + current.GetAge(civ));
                    }

                    var bo = civ.GetBuildOrder(current, 100);
                    if (bo == null)
                    {
                        continue;
                    }

                    if (bo.Elements.Count >= 99)
                    {
                        Log.Debug($"too many bo elements {civ.Name} {current.Id}");
                        continue;
                    }

                    counters.Add(current);

                    strat.BuildOrders.Add(current, bo);
                    Log.Debug($"unit {current.Id} {current.Name}");
                }

                // get counters
                foreach (var counter in counters.ToList())
                {
                    foreach (var upgr in counter.UpgradedFrom.Concat(counter.UpgradesTo))
                    {
                        if (upgr.GetAge(civ) >= 2)
                        {
                            counters.Add(upgr);
                        }
                    }
                }

                var cgen = new CounterGenerator(civ);
                var cs = cgen.GetCounters(enemies, counters.ToList());
                var baseunits = new HashSet<Unit>();
                foreach (var unit in cs.Select(c => c.EnemyUnit))
                {
                    baseunits.Add(unit.BaseUnit);
                }

                foreach (var counter in cs.Where(c => baseunits.Contains(c.EnemyUnit)))
                {
                    if (strat.BuildOrders.ContainsKey(counter.CounterUnit))
                    {
                        strat.Counters.Add(counter);
                    }
                    else
                    {
                        foreach (var upgr in counter.CounterUnit.UpgradedFrom)
                        {
                            if (strat.BuildOrders.ContainsKey(upgr))
                            {
                                strat.Counters.Add(new Counter(counter.EnemyUnit, counter.Age, upgr));
                                break;
                            }
                        }
                    }
                }

                foreach (var counter in strat.Counters)
                {
                    Log.Debug($"counter {counter.EnemyUnit.Id} {counter.EnemyUnit.Name} in age {counter.Age} with {counter.CounterUnit.Id} {counter.CounterUnit.Name}");
                }

                strategies.Add(civ, strat);
            }

            var sb = new StringBuilder();

            sb.AppendLine(";---- Auto generated ----");
            sb.AppendLine("");

            sb.AppendLine(";region Auto Counters");
            sb.AppendLine("var gl-target-count = 0");
            sb.AppendLine("var gl-current-count = 0");

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
                sb.AppendLine($"\t(up-modify-goal gl-target-count g:== gl-current-count)");
                sb.AppendLine(")");
            }

            sb.AppendLine(";endregion");

            foreach (var civ in strategies.Keys)
            {
                sb.AppendLine($"; ------ STARTING CIV {civ.Id} {civ.Name} ------");
                sb.AppendLine(";region " + civ.Name);

                sb.AppendLine("#if civ-selected " + civ.Id);

                var strat = strategies[civ];


                // write counters
                sb.AppendLine(";region ---- Counters");
                foreach (var counter in strat.Counters)
                {
                    var age = civ.Age1Tech.Id;
                    if (counter.Age == 2)
                    {
                        age = civ.Age2Tech.Id;
                    }
                    if (counter.Age == 3)
                    {
                        age = civ.Age3Tech.Id;
                    }
                    if (counter.Age == 4)
                    {
                        age = civ.Age4Tech.Id;
                    }

                    sb.AppendLine($";counter {counter.EnemyUnit.Id} {counter.EnemyUnit.Name} with {counter.CounterUnit.Id} {counter.CounterUnit.Name} in age {age}");
                    sb.AppendLine($"(defrule");
                    sb.AppendLine($"\t(civ-selected {civ.Id})");
                    sb.AppendLine($"\t(strategic-number sn-target-unit == {counter.EnemyUnit.Id})");
                    sb.AppendLine($"\t(current-age == {age})");
                    sb.AppendLine($"=>");
                    sb.AppendLine($"\t(set-strategic-number sn-strategy {counter.CounterUnit.Id})");
                    sb.AppendLine($")");
                    sb.AppendLine("");
                }

                sb.AppendLine(";endregion");

                // write build orders
                sb.AppendLine(";region ---- Build Orders");
                foreach (var unit in strat.BuildOrders.Keys)
                {
                    var bo = strat.BuildOrders[unit];

                    var cbo = bo.Compile();

                    sb.AppendLine($"; -- Build order for {unit.Id} {unit.Name}");

                    sb.AppendLine("#if sn-strategy == " + unit.Id);

                    sb.AppendLine(cbo);

                    sb.AppendLine("#end-if");
                }
                sb.AppendLine(";endregion");

                // set initial bo
                sb.AppendLine("; choose initial bo");
                foreach (var unit in strat.BuildOrders.Keys.Where(u => u.GetAge(civ) == 2))
                {
                    sb.AppendLine($"#if sn-strategy == OFF");
                    sb.AppendLine($"    generate-random-number 100");
                    sb.AppendLine("     #if random-number < 30");
                    sb.AppendLine($"            sn-strategy = {unit.Id}");
                    sb.AppendLine("     #end-if");
                    sb.AppendLine("#end-if");
                }

                sb.AppendLine("#if sn-strategy == OFF");
                sb.AppendLine(" sn-strategy = 75");
                sb.AppendLine("#end-if");

                sb.AppendLine("#end-if");
                sb.AppendLine(";endregion");
            }

            var file = Path.Combine(folder, "Strategies.per");
            File.WriteAllText(file, sb.ToString());
        }

        private void Compile(Settings settings)
        {
            CompileOld(settings);
        }

        private void CompileOld(Settings settings)
        {
            var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old", "Script");
            if (Directory.Exists(script))
            {
                Directory.Delete(script, true);
            }

            CopyFolder(settings.SourceFolder, script);

            var comp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old", "Binary.exe");
            var process = Process.Start(comp);
            process.WaitForExit();

            script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Old", "ParsedScript");
            CopyFolder(script, settings.AiFolder);
        }

        private void CopyFolder(string from, string to)
        {
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            var queue = new Queue<string>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                var rel = current.Replace(from, "");
                var outdir = to + rel;
                if (!Directory.Exists(outdir))
                {
                    Directory.CreateDirectory(outdir);
                }

                foreach (var file in Directory.GetFiles(current))
                {
                    var outfile = Path.Combine(outdir, Path.GetFileName(file));
                    File.Copy(file, outfile, true);
                }

                foreach (var dir in Directory.GetDirectories(current))
                {
                    queue.Enqueue(dir);
                }
            }
        }

        private void ButtonTest_Click(object sender, EventArgs e)
        {
            ButtonTest.Enabled = false;
            Refresh();

            

            var settings = new Settings();
            var mod = new Mod();
            mod.Load(settings.DatFile);

            var sw = new Stopwatch();
            sw.Start();

            var strategies = new Dictionary<Civilization, Strategy>();
            AddBuildOrders(mod, strategies);
            AddCounters(mod, strategies);

            sw.Stop();
            Log.Debug("time: " + sw.Elapsed.TotalSeconds);

            ButtonTest.Enabled = true;
        }

        private void AddBuildOrders(Mod mod, Dictionary<Civilization, Strategy> strategies)
        {
            foreach (var civ in mod.Civilizations.Where(c => c.Id > 0))
            {
                if (!strategies.ContainsKey(civ))
                {
                    strategies.Add(civ, new Strategy());
                }

                var strat = strategies[civ];

                Log.Debug($"---- {civ.Id} {civ.Name} ----");

                var units = civ.Units
                    .Where(u => u.Land)
                    .Select(u => u.BaseUnit)
                    .Where(u => u.Available || u.TechRequired)
                    .Where(u => u.BuildLocation != null)
                    .Where(u => u.Military)
                    .Distinct()
                    .ToList();

                var bogen = new BuildOrderGenerator(civ);

                foreach (var unit in units)
                {
                    var current = unit;

                    if (current.GetAge(civ) <= 1)
                    {
                        var bestage = int.MaxValue;
                        foreach (var upgr in unit.UpgradesTo)
                        {
                            var age = upgr.GetAge(civ);
                            if (age > 1 && age < bestage)
                            {
                                current = upgr;
                                bestage = age;
                            }
                        }
                    }

                    if (current.GetAge(civ) <= 1)
                    {
                        continue;
                    }

                    var good = false;
                    switch (current.Class)
                    {
                        case UnitClass.Archer:
                        case UnitClass.Ballista:
                        case UnitClass.Cavalry:
                        case UnitClass.CavalryArcher:
                        case UnitClass.CavalryRaider:
                        case UnitClass.Conquistador:
                        case UnitClass.ElephantArcher:
                        case UnitClass.HandCannoneer:
                        case UnitClass.Infantry:
                        case UnitClass.Monk:
                        case UnitClass.Phalanx:
                        case UnitClass.Pikeman:
                        case UnitClass.Raider:
                        case UnitClass.Scout:
                        case UnitClass.SiegeWeapon:
                        case UnitClass.Spearman:
                        case UnitClass.TwoHandedSwordsMan:
                        case UnitClass.PackedUnit:
                        case UnitClass.UnpackedSiegeUnit:
                        case UnitClass.WarElephant: good = true; break;
                    }

                    if (!good)
                    {
                        continue;
                    }

                    var bo = bogen.GetBuildOrder(current, null, null, true, true, 100);

                    if (bo != null && bo.Count <= 100)
                    {
                        Log.Debug($"found bo for {current.Id} {current.Name} with {bo.Count} elements");
                        foreach (var be in bo)
                        {
                            Log.Debug(be.ToString());
                        }

                        strat.BuildOrders.Add(unit, bo);
                    }
                    else
                    {
                        Log.Debug($"no build order for {current.Id} {current.Name}");
                    }
                }
            }
        }

        private void AddCounters(Mod mod, Dictionary<Civilization, Strategy> strategies)
        {
            var enemies = mod.Civilizations.SelectMany(c => c.TrainableUnits).Where(u => u.Land && u.Class != UnitClass.Civilian).Distinct().ToList();

            foreach (var civ in mod.Civilizations.Where(c => c.Id > 0))
            {
                if (!strategies.ContainsKey(civ))
                {
                    strategies.Add(civ, new Strategy());
                }

                var strat = strategies[civ];

                var counters = new HashSet<Unit>();
                // get counters
                foreach (var counter in strat.BuildOrders.Keys)
                {
                    counters.Add(counter);
                    foreach (var upgr in counter.UpgradedFrom.Concat(counter.UpgradesTo))
                    {
                        if (upgr.GetAge(civ) >= 2)
                        {
                            counters.Add(upgr);
                        }
                    }
                }

                var cgen = new CounterGenerator(civ);
                var cs = cgen.GetCounters(enemies, counters.ToList());

                var baseunits = new HashSet<Unit>();
                foreach (var unit in cs.Select(c => c.EnemyUnit))
                {
                    baseunits.Add(unit.BaseUnit);
                }

                foreach (var counter in cs.Where(c => baseunits.Contains(c.EnemyUnit)))
                {
                    if (strat.BuildOrders.ContainsKey(counter.CounterUnit))
                    {
                        strat.Counters.Add(counter);
                    }
                    else
                    {
                        foreach (var upgr in counter.CounterUnit.UpgradedFrom)
                        {
                            if (strat.BuildOrders.ContainsKey(upgr))
                            {
                                strat.Counters.Add(new Counter(counter.EnemyUnit, counter.Age, upgr));
                                break;
                            }
                        }
                    }
                }

                Log.Debug("found " + strat.Counters.Count + " counters");
            }
        }
    }
}
