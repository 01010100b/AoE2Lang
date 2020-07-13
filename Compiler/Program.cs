using Compiler.Mods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YTY.AocDatLib;

namespace Compiler
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Test();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        internal static void Test()
        {
            const int CIV = 11;
            const int UNIT = 75;
            const int TECH = 102;

            var file = @"C:\Users\Tim\AppData\Roaming\Microsoft Games\Age of Empires ii\Data\Empires2_x1_p1.dat";
            var mod = new Mod();
            mod.Load(file);

            Trace.WriteLine("Mod effects: " + mod.Effects.Count);
            Trace.WriteLine("Mod techs: " + mod.Technologies.Count);
            Trace.WriteLine("Mod units: " + mod.Units.Count);
            Trace.WriteLine("Mod civs: " + mod.Civilizations.Count);
            Trace.WriteLine("Mod available units: " + mod.AvailableUnits.Count);
            Trace.WriteLine("Mod trainable units: " + mod.TrainableUnits.Count);
            Trace.WriteLine("Mod unit lines: " + mod.TrainableUnits.Select(u => u.BaseUnit).Distinct().Count());

            foreach (var u in mod.Units)
            {
                if (u.ResourceGathered != Resource.None)
                {
                    Debug.WriteLine($"unit {u.Id} {u.Name} gathers resource {u.ResourceGathered}");
                }
            }

            foreach (var t in mod.Technologies)
            {
                if (t.ResourceImproved != Resource.None)
                {
                    Debug.WriteLine($"tech {t.Id} {t.Name} improves resource {t.ResourceImproved}");
                }
            }

            var civ = mod.Civilizations[CIV];
            var unit = civ.Units.Single(u => u.Id == UNIT);
            var tech = civ.Technologies.Single(t => t.Id == TECH);

            Debug.WriteLine("food " + civ.GetDropSite(Resource.Food).Id);

            Trace.WriteLine(civ + $" going for unit {unit.Id} {unit.Name}");

            var sw = new Stopwatch();
            sw.Start();

            var bo = civ.GetBuildOrder(unit);

            sw.Stop();
            Trace.WriteLine(bo);

            Trace.WriteLine($"Took {sw.ElapsedMilliseconds} ms");
        }
    }
}
