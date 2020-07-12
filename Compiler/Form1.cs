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
            CompileOld(new Settings());
            Debug.WriteLine("done");
        }

        private void Compile(Settings settings)
        {
            
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
                foreach (var file in Directory.GetFiles(current))
                {
                    var outfile = Path.Combine(to + rel, Path.GetFileName(file));
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
