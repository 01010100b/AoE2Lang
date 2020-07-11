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
        public readonly string Name;
        public readonly int Type;
        public readonly int ClassId;
        
        public readonly bool Available;
        public readonly bool TechRequired;
        public readonly Technology TechInitiated;

        public Unit BuildLocation { get; internal set; } = null;
        public Unit StackUnit { get; internal set; } = null;
        public Unit HeadUnit { get; internal set; } = null;
        public Unit TransformUnit { get; internal set; } = null;
        public Unit PileUnit { get; internal set; } = null;

        private readonly int FoodCost;
        private readonly int WoodCost;
        private readonly int GoldCost;
        private readonly int StoneCost;

        public Unit(YTY.AocDatLib.Unit unit, List<Technology> technologies)
        {
            Id = unit.Id;
            Name = new string(Encoding.ASCII.GetChars(unit.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
            Type = unit.Type;
            ClassId = unit.UnitClass;

            TechRequired = false;
            Available = false;
            if (unit.Enabled == 1)
            {
                Available = true;
            }

            foreach (var tech in technologies.Where(t => t.Effect != null))
            {
                if (TechRequired)
                {
                    break;
                }

                foreach (var command in tech.Effect.Commands)
                {
                    if (command is EnableDisableUnitCommand ec)
                    {
                        if (ec.Enable && ec.UnitId == Id)
                        {
                            TechRequired = true;
                            break;
                        }
                    }

                    if (command is UpgradeUnitCommand uc)
                    {
                        if (uc.ToUnitId == Id)
                        {
                            TechRequired = true;
                            break;
                        }
                    }
                }
            }

            if (unit.TechId > 0)
            {
                TechInitiated = technologies.Single(t => t.Id == unit.TechId);
            }
            else
            {
                TechInitiated = null;
            }

            if (unit.Cost1Used == 1)
            {
                switch (unit.Cost1Id)
                {
                    case 0: FoodCost = unit.Cost1Amount; break;
                    case 1: WoodCost = unit.Cost1Amount; break;
                    case 2: StoneCost = unit.Cost1Amount; break;
                    case 3: GoldCost = unit.Cost1Amount; break;
                }
            }
            
            if (unit.Cost2Used == 1)
            {
                switch (unit.Cost2Id)
                {
                    case 0: FoodCost = unit.Cost2Amount; break;
                    case 1: WoodCost = unit.Cost2Amount; break;
                    case 2: StoneCost = unit.Cost2Amount; break;
                    case 3: GoldCost = unit.Cost2Amount; break;
                }
            }
            
            if (unit.Cost3Used == 1)
            {
                switch (unit.Cost3Id)
                {
                    case 0: FoodCost = unit.Cost3Amount; break;
                    case 1: WoodCost = unit.Cost3Amount; break;
                    case 2: StoneCost = unit.Cost3Amount; break;
                    case 3: GoldCost = unit.Cost3Amount; break;
                }
            }
        }

        public Cost GetCost(Civilization civilization)
        {
            return new Cost(FoodCost, WoodCost, GoldCost, StoneCost);
        }

        public Unit GetBaseUnit(Civilization civilization)
        {
            if (Available || TechRequired == false)
            {
                return this;
            }

            var current = this;
            var found = true;
            
            while (found)
            {
                found = false;

                foreach (var tech in civilization.Technologies.Where(t => t.Effect != null))
                {
                    foreach (var command in tech.Effect.Commands)
                    {
                        if (command is UpgradeUnitCommand uc)
                        {
                            if (uc.ToUnitId == current.Id)
                            {
                                current = civilization.Units.Single(u => u.Id == uc.FromUnitId);
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }
            }

            return current;
        }
    }
}
