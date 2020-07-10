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
        public readonly string Name;
        public readonly int CivId;
        public Unit ResearchLocation { get; internal set; } = null;
        public readonly Effect Effect;
        public readonly List<Technology> Prerequisites = new List<Technology>();
        public int MinPrerequisites => Math.Min(RequiredTechCount, Prerequisites.Count);

        private readonly int RequiredTechCount;

        public Technology(int id, YTY.AocDatLib.Research research, List<Effect> effects)
        {
            Id = id;
            Name = new string(Encoding.ASCII.GetChars(research.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
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

            RequiredTechCount = research.RequiredTechCount;
        }
    }
}
