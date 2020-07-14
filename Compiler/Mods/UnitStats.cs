using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class UnitStats
    {
        public struct ArmorValue
        {
            public readonly int Id;
            public readonly int Amount;

            public ArmorValue(int id, int amount)
            {
                Id = id;
                Amount = amount;
            }
        }

        public readonly Cost Cost;
        public readonly int Hitpoints;
        public readonly int Range;
        public readonly double ReloadTime;
        public readonly List<ArmorValue> Armors;
        public readonly List<ArmorValue> Attacks;

        public UnitStats(Unit unit, List<Effect> effects)
        {
            Cost = unit.BaseCost;
            Hitpoints = unit.BaseHitpoints;
            Range = unit.BaseRange;
            ReloadTime = unit.BaseReloadTime;
            Armors = unit.BaseArmors.ToList();
            Attacks = unit.BaseAttacks.ToList();
        }
    }
}
