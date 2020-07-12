using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using YTY.AocDatLib;

namespace Compiler.Mods
{
    class Unit
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int Type;
        public readonly int ClassId;
        public readonly bool Land;

        public readonly bool Available;
        public readonly bool TechRequired;
        public readonly Technology TechInitiated;
        public Resource ResourceGathered => GetResourceGathered();

        public Unit BuildLocation { get; internal set; } = null;
        public Unit UpgradedFrom { get; internal set; } = null;
        public Unit UpgradesTo { get; internal set; } = null;
        public Unit BaseUnit { get
            {
                var current = this;
                while (current.UpgradedFrom != null)
                {
                    current = current.UpgradedFrom;
                }

                return current;
            }
        }
        public Unit StackUnit { get; internal set; } = null;
        public Unit HeadUnit { get; internal set; } = null;
        public Unit TransformUnit { get; internal set; } = null;
        public Unit PileUnit { get; internal set; } = null;
        public Unit DropSite { get; internal set; } = null;

        public readonly List<UnitCommand> Commands = new List<UnitCommand>();

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
            

            if (unit.TerrainRestriction == 4 || unit.TerrainRestriction == 7)
            {
                Land = true;
            }
            else
            {
                Land = false;
            }

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

        public int GetAge(Civilization civilization)
        {
            var bo = new BuildOrder(civilization, this);
            var age = 1;
            if (bo.Elements.Count(be => be.Gatherers == false && be.Research == true && be.Technology == civilization.Age2Tech) > 0)
            {
                age = 2;
            }
            if (bo.Elements.Count(be => be.Gatherers == false && be.Research == true && be.Technology == civilization.Age3Tech) > 0)
            {
                age = 3;
            }
            if (bo.Elements.Count(be => be.Gatherers == false && be.Research == true && be.Technology == civilization.Age4Tech) > 0)
            {
                age = 4;
            }

            return age;
        }

        private Resource GetResourceGathered()
        {
            var resource = Resource.None;
            foreach (var command in Commands)
            {
                if (command.Attr1 != (int)Resource.None)
                {
                    resource = (Resource)command.Attr1;

                    if (command.Attr3 != (int)Resource.None)
                    {
                        resource = (Resource)command.Attr3;
                    }
                }
            }

            return resource;
        }
    }
}
