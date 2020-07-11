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

        private readonly int FoodCost = 0;
        private readonly int WoodCost = 0;
        private readonly int GoldCost = 0;
        private readonly int StoneCost = 0;

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

            switch (research.Cost1Type)
            {
                case 0: FoodCost = research.Cost1Amount; break;
                case 1: WoodCost = research.Cost1Amount; break;
                case 2: StoneCost = research.Cost1Amount; break;
                case 3: GoldCost = research.Cost1Amount; break;
            }

            switch (research.Cost2Type)
            {
                case 0: FoodCost = research.Cost2Amount; break;
                case 1: WoodCost = research.Cost2Amount; break;
                case 2: StoneCost = research.Cost2Amount; break;
                case 3: GoldCost = research.Cost2Amount; break;
            }

            switch (research.Cost3Type)
            {
                case 0: FoodCost = research.Cost3Amount; break;
                case 1: WoodCost = research.Cost3Amount; break;
                case 2: StoneCost = research.Cost3Amount; break;
                case 3: GoldCost = research.Cost3Amount; break;
            }
        }

        public Cost GetCost(Civilization civilization)
        {
            return new Cost(FoodCost, WoodCost, GoldCost, StoneCost);
        }
    }
}
