using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler.Lang
{
    class Variable
    { 
        public Types.Type Type { get; set; }
        public string Name { get; set; }
        public int Register { get; set; }
    }
}
