using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class Technology
    {
        public readonly int Id;
        public readonly int CivId;
        public readonly Effect Effect;

        public Technology(int id, YTY.AocDatLib.Research research, List<Effect> effects)
        {
            Id = id;
            CivId = research.Civilization;

            var eid = research.EffectId;
            if (eid >= 0)
            {
                Effect = effects.Single(e => e.Id == eid);
            }
            else
            {
                Effect = null;
            }
        }
    }
}
