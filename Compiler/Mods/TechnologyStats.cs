using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class TechnologyStats
    {
        public readonly Cost Cost;

        public TechnologyStats(Technology technology, List<Effect> effects)
        {
            Cost = technology.GetCost(effects);
        }
    }
}
