using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using YTY.AocDatLib;
using static Compiler.Mods.UnitStats;

namespace Compiler.Mods
{
    class Unit
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int Type;
        public readonly UnitClass Class;
        public readonly bool Land;
        public bool Military => BaseAttacks.Count(a => a.Amount > 0) > 0 || Commands.Count(c => c.Kind == 104) > 0;

        public readonly bool Available;
        public readonly bool TechRequired;
        public readonly Technology TechInitiated;
        public Resource ResourceGathered => GetResourceGathered();

        public Unit TrainLocation { get; internal set; } = null;
        public readonly TimeSpan TrainTime;
        public HashSet<Unit> UpgradedFrom { get; internal set; } = new HashSet<Unit>();
        public HashSet<Unit> UpgradesTo { get; internal set; } = new HashSet<Unit>();
        public Unit BaseUnit => UpgradedFrom.FirstOrDefault(u => u.UpgradedFrom.Count == 0) ?? this;
        public List<Unit> Line
        {
            get
            {
                var line = new List<Unit>();
                
                line.AddRange(UpgradedFrom);
                line.AddRange(UpgradesTo);
                line.Add(this);

                line.Sort((a, b) => b.UpgradesTo.Count.CompareTo(a.UpgradesTo.Count));

                return line;
            }
        }
        public Unit StackUnit { get; internal set; } = null;
        public Unit HeadUnit { get; internal set; } = null;
        public Unit TransformUnit { get; internal set; } = null;
        public Unit PileUnit { get; internal set; } = null;
        public Unit DropSite { get; internal set; } = null;

        public readonly int BaseHitpoints;
        public readonly int BaseRange;
        public readonly List<ArmorValue> BaseArmors;
        public readonly List<ArmorValue> BaseAttacks;
        public readonly double BaseReloadTime;

        public readonly List<UnitCommand> Commands = new List<UnitCommand>();
        public Cost BaseCost => new Cost(FoodCost, WoodCost, GoldCost, StoneCost);

        private readonly int FoodCost;
        private readonly int WoodCost;
        private readonly int GoldCost;
        private readonly int StoneCost;

        public Unit(YTY.AocDatLib.Unit unit, List<Technology> technologies)
        {
            Id = unit.Id;
            Name = new string(Encoding.ASCII.GetChars(unit.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
            Type = unit.Type;
            Class = (UnitClass)unit.UnitClass;
            TrainTime = TimeSpan.FromSeconds(unit.TrainTime);

            BaseHitpoints = unit.HitPoints;
            BaseRange = Math.Max(1, (int)unit.MaxRange);
            BaseArmors = new List<ArmorValue>();
            if (unit.Armors != null)
            {
                BaseArmors.AddRange(unit.Armors.Select(a => new ArmorValue(a.Id, a.Amount)));
            }
            BaseAttacks = new List<ArmorValue>();
            if (unit.Attacks != null)
            {
                BaseAttacks.AddRange(unit.Attacks.Select(a => new ArmorValue(a.Id, a.Amount)));
            }

            BaseReloadTime = unit.ReloadTime;

            if (unit.TerrainRestriction == 4 || unit.TerrainRestriction == 7 || unit.TerrainRestriction == 20)
            {
                Land = true;
            }
            else
            {
                Land = false;
            }

            Available = false;
            if (unit.Enabled == 1)
            {
                Available = true;
            }

            TechRequired = false;
            foreach (var tech in technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is EnableDisableUnitCommand ec)
                    {
                        if (ec.Enable && ec.UnitId == Id)
                        {
                            TechRequired = true;
                        }
                    }

                    if (command is UpgradeUnitCommand uc)
                    {
                        if (uc.ToUnitId == Id)
                        {
                            TechRequired = true;
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
            var bo = new OldBuildOrder(civilization, this);
            if (bo.Elements == null)
            {
                return -1;
            }

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

        public Technology GetRequiredTech(Civilization civilization)
        {
            foreach (var tech in civilization.Technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is EnableDisableUnitCommand ec)
                    {
                        if (ec.Enable == true && ec.UnitId == Id)
                        {
                            return tech;
                        }
                    }

                    if (command is UpgradeUnitCommand uc)
                    {
                        if (uc.ToUnitId == Id)
                        {
                            return tech;
                        }
                    }
                }
            }

            return null;
        }
    }
}
