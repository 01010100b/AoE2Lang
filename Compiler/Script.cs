using Compiler.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler
{
    class Script
    {
        public readonly List<Variable> GlobalVariables = new List<Variable>();
        public readonly List<Function> Functions = new List<Function>();
    }
}
