using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler.Lang
{
    class Function
    {
        public Types.Type ReturnType { get; set; }
        public string Name { get; set; }
        public List<Variable> Parameters { get; set; }
        public Block Block { get; set; }
    }
}
