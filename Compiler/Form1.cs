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
            var sw = new Stopwatch();
            sw.Start();

            ButtonCompile.Enabled = false;

            var settings = new Settings();

            Log.Debug("setting build orders");
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
            var name = Directory.GetFiles(settings.SourceFolder).Single(f => Path.GetExtension(f) == ".ai");
            name = Path.GetFileNameWithoutExtension(name);
            var folder = Path.Combine(settings.SourceFolder, name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            const int CIV = 11;
            const int UNIT = 75;

            var mod = new Mod();
            mod.Load(settings.DatFile);

            var sb = new StringBuilder();



            var rng = new Random();

            // add build orders
            foreach (var civ in mod.Civilizations.Where(c => c.Id != 0))
            {
                Log.Debug("doing civ " + civ.Name);
                sb.AppendLine("#if civ-selected " + civ.Id);

                // TODO counters
                //sb.AppendLine("sn-target = 75");

                var units = civ.TrainableUnits
                    .Where(u => u.Land)
                    .Select(u => u.BaseUnit)
                    .Where(u => u.BuildLocation != null)
                    .Distinct()
                    .OrderBy(u => rng.Next())
                    .ToList();

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

                    sb.AppendLine($"#if sn-target == OFF");
                    sb.AppendLine($"generate-random-number 100");
                    sb.AppendLine("#if random-number < 10");
                    sb.AppendLine($"sn-target = {current.Id}");
                    sb.AppendLine("#end-if");
                    sb.AppendLine("#end-if");

                    Log.Debug($"unit {current.Id} {current.Name} {current.GetAge(civ)}");

                    sb.AppendLine("#if sn-target == " + current.Id);

                    try
                    {
                        var bo = civ.GetBuildOrder(current, 100);
                        if (bo.Elements.Count >= 99)
                        {
                            throw new Exception($"too many bo elements {civ.Name} {current.Id}");
                        }
                        var cbo = bo.Compile();

                        sb.AppendLine(cbo);
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"error civ {civ.Name} unit {current.Id}: " + e.Message + "\n" + e.StackTrace);
                    }

                    sb.AppendLine("#end-if");
                }

                sb.AppendLine("#if sn-target == OFF");
                sb.AppendLine("sn-target = 75");
                sb.AppendLine("#end-if");

                sb.AppendLine("#end-if");
            }
            
            var file = Path.Combine(folder, "BuildOrder.per");
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
    }
}
