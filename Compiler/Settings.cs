using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class Settings
    {
        public string SourceFolder { get; set; } = @"C:\Users\Tim\source\repos\AoE2Lang\Compiler\Script";
        public string AiFolder { get; set; } = @"C:\Users\Tim\AppData\Roaming\Microsoft Games\Age of Empires ii\Ai";
        public string DatFile { get; set; } = @"C:\Users\Tim\AppData\Roaming\Microsoft Games\Age of Empires ii\Data\Empires2_x1_p1.dat";
        public bool UseBinaryCompiler { get; set; } = true;
    }
}
