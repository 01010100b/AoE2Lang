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
            Debug.WriteLine((int)Resource.SpiesDiscount);
            var path = @"C:\Users\Tim\AppData\Roaming\Microsoft Games\Age of Empires ii\Data\Empires2_x1_p1.dat";
            var dat = new DatFile(path);
            
            for (int i = 0; i < dat.Technologies.Count; i++)
            {
                var tech = dat.Technologies[i];

                foreach (var effect in tech.Effects)
                {
                    if (effect.Command == 1)
                    {
                        //Debug.WriteLine("effect: " + i);
                    }
                }
            }
        }
    }
}
