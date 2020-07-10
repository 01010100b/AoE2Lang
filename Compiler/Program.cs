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
            var file = @"C:\Users\Tim\AppData\Roaming\Microsoft Games\Age of Empires ii\Data\Empires2_x1_p1.dat";
            var mod = new Mod();
            mod.Load(file);

            Debug.WriteLine("Mod effects: " + mod.Effects.Count);
            Debug.WriteLine("Mod techs: " + mod.Technologies.Count);
            Debug.WriteLine("Mod civs: " + mod.Civilizations.Count);

            foreach (var civ in mod.Civilizations)
            {
                Debug.WriteLine(civ.Name + " with " + civ.Technologies.Count + " techs and " + civ.Units.Count + " units");
            }

            var dat = new DatFile(file);
            for (int i = 0; i < dat.Technologies.Count; i++)
            {
                foreach (var effect in dat.Technologies[i].Effects)
                {
                    if (effect.Command == 3)
                    {
                        Debug.WriteLine("effect: " + i);
                        return;
                    }
                }
            }
        }
    }
}
