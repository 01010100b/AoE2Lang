using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Compiler.BuildOrderGenerator;
using static Compiler.CounterGenerator;

namespace Compiler
{
    class Strategy
    {
        public readonly Dictionary<Unit, List<BuildOrderElement>> BuildOrders = new Dictionary<Unit, List<BuildOrderElement>>();
        public readonly List<Counter> Counters = new List<Counter>();
    }
}
