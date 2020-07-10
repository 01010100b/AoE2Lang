using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class Unit
    {
        public readonly int Id;
        public readonly int Type;
        public readonly bool Available;

        public Unit(YTY.AocDatLib.Unit unit)
        {
            Id = unit.Id;
            Type = unit.Type;
            if (unit.Enabled == 1)
            {
                Available = true;
            }
            else
            {
                Available = false;
            }
        }
    }
}
