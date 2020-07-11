using Compiler.Mods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            Test();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        internal static void Test()
        {
            const int CIV = 7;
            const int UNIT = 553;

            var file = @"C:\Users\Tim\AppData\Roaming\Microsoft Games\Age of Empires ii\Data\Empires2_x1_p1.dat";
            var mod = new Mod();
            mod.Load(file);

            Debug.WriteLine("Mod effects: " + mod.Effects.Count);
            Debug.WriteLine("Mod techs: " + mod.Technologies.Count);
            Debug.WriteLine("Mod units: " + mod.Units.Count);
            Debug.WriteLine("Mod civs: " + mod.Civilizations.Count);
            Debug.WriteLine("Mod available units: " + mod.AvailableUnits.Count);

            var civ = mod.Civilizations[CIV];
            Debug.WriteLine($"{civ.Name} with {civ.Units.Count} units and {civ.Technologies.Count} techs going for unit {UNIT}");

            var sw = new Stopwatch();
            sw.Start();

            var unit = civ.Units.Single(u => u.Id == UNIT);
            var bo = new BuildOrder(civ, unit);

            var rng = new Random();
            for (int i = 0; i < 200; i++)
            {
                int seed = -1;
                lock (rng)
                {
                    seed = rng.Next() ^ rng.Next() ^ rng.Next();
                }

                var nbo = new BuildOrder(civ, unit, seed);

                lock (rng)
                {
                    if (nbo.TotalCost < bo.TotalCost)
                    {
                        bo = nbo;
                    }
                }
            }

            sw.Stop();
            Debug.WriteLine(bo);

            Debug.WriteLine($"Took {sw.ElapsedMilliseconds} ms");
        }
    }
}
