using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YTY.AocDatLib;

namespace Compiler.Mods
{
    class Mod
    {
        public readonly List<Effect> Effects = new List<Effect>();
        public readonly List<Technology> Technologies = new List<Technology>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Civilization> Civilizations = new List<Civilization>();
        public List<Unit> AvailableUnits => Civilizations.SelectMany(c => c.AvailableUnits).Distinct().ToList();
        public List<Unit> TrainableUnits => Civilizations.SelectMany(c => c.TrainableUnits).Distinct().ToList();

        public void Load(string file)
        {
            Effects.Clear();
            Technologies.Clear();
            Units.Clear();
            Civilizations.Clear();

            var dat = new DatFile(file);

            // get effects
            for (int id = 0; id < dat.Technologies.Count; id++)
            {
                var effect = new Effect(id);
                foreach (var ec in dat.Technologies[id].Effects)
                {
                    var command = EffectCommand.Get(ec);
                    if (command != null)
                    {
                        effect.Commands.Add(command);
                    }
                }

                Effects.Add(effect);
            }

            // get techs

            for (int id = 0; id < dat.Researches.Count; id++)
            {
                var tech = new Technology(id, dat.Researches[id], Effects);
                Technologies.Add(tech);
            }

            foreach (var tech in Technologies)
            {
                var dattech = dat.Researches[tech.Id];
                if (dattech.RequiredTech1 >= 0)
                {
                    tech.Prerequisites.Add(Technologies.Single(t => t.Id == dattech.RequiredTech1));
                }
                if (dattech.RequiredTech2 >= 0)
                {
                    tech.Prerequisites.Add(Technologies.Single(t => t.Id == dattech.RequiredTech2));
                }
                if (dattech.RequiredTech3 >= 0)
                {
                    tech.Prerequisites.Add(Technologies.Single(t => t.Id == dattech.RequiredTech3));
                }
                if (dattech.RequiredTech4 >= 0)
                {
                    tech.Prerequisites.Add(Technologies.Single(t => t.Id == dattech.RequiredTech4));
                }
                if (dattech.RequiredTech5 >= 0)
                {
                    tech.Prerequisites.Add(Technologies.Single(t => t.Id == dattech.RequiredTech5));
                }
                if (dattech.RequiredTech6 >= 0)
                {
                    tech.Prerequisites.Add(Technologies.Single(t => t.Id == dattech.RequiredTech6));
                }
            }

            // get units
            var datunits = new Dictionary<int, YTY.AocDatLib.Unit>();
            var units = new Dictionary<int, Unit>();
            foreach (var unit in dat.Civilizations.SelectMany(c => c.Units))
            {
                datunits[unit.Id] = unit;
                units[unit.Id] = new Unit(unit, Technologies);
            }

            Units.AddRange(units.Values);

            // assign units to techs
            foreach (var tech in Technologies)
            {
                var dattech = dat.Researches[tech.Id];
                if (dattech.ResearchLocation > 0)
                {
                    tech.ResearchLocation = units[dattech.ResearchLocation];
                }
            }

            Units.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < Units.Count; i++)
            {
                Units[i].Commands.AddRange(dat.UnitCommands[i]);
            }

            var upgrades = Technologies
                .Where(t => t.Effect != null)
                .SelectMany(t => t.Effect.Commands)
                .Where(c => c is UpgradeUnitCommand)
                .ToList();

            foreach (var uc in upgrades.Cast<UpgradeUnitCommand>())
            {
                if (units.TryGetValue(uc.FromUnitId, out Unit from))
                {
                    if (units.TryGetValue(uc.ToUnitId, out Unit to))
                    {
                        from.UpgradesTo.Add(to);
                        to.UpgradedFrom.Add(from);
                    }
                }
            }

            // set unit refs
            foreach (var unit in Units)
            {
                var datunit = datunits[unit.Id];

                if (datunit.TrainLocationId > 0 && unit.Type != 80)
                {
                    unit.BuildLocation = units[datunit.TrainLocationId];
                }

                if (datunit.StackUnitId > 0)
                {
                    unit.StackUnit = units[datunit.StackUnitId];
                }

                if (datunit.HeadUnitId > 0)
                {
                    unit.HeadUnit = units[datunit.HeadUnitId];
                }

                if (datunit.TransformUnitId > 0)
                {
                    unit.TransformUnit = units[datunit.TransformUnitId];
                }

                if (datunit.PileUnit > 0)
                {
                    unit.PileUnit = units[datunit.PileUnit];
                }

                if (unit.Land)
                {
                    if (units.TryGetValue(datunit.DropSite0, out Unit d0))
                    {
                        unit.DropSite = d0;
                    }

                    if (units.TryGetValue(datunit.DropSite1, out Unit d1))
                    {
                        if (unit.DropSite == null)
                        {
                            unit.DropSite = d1;
                        }
                        else if (d1.GetCost(null).Total < unit.DropSite.GetCost(null).Total)
                        {
                            unit.DropSite = d1;
                        }
                    }
                }
            }

            // set tech resource improvements
            foreach (var tech in Technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand ac)
                    {
                        if (ac.Attribute == Attribute.CarryCapacity)
                        {
                            if (ac.ClassId == 4)
                            {
                                tech.ResourceImproved = Resource.Food;
                                break;
                            }
                            else if (units.TryGetValue(ac.UnitId, out Unit unit))
                            {
                                if (unit.ResourceGathered != Resource.None)
                                {
                                    tech.ResourceImproved = unit.ResourceGathered;
                                    break;
                                }
                                
                            }
                        }
                        else if (ac.Attribute == Attribute.WorkRate)
                        {
                            if (units.TryGetValue(ac.UnitId, out Unit unit))
                            {
                                if (unit.ResourceGathered != Resource.None)
                                {
                                    tech.ResourceImproved = unit.ResourceGathered;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // get civs

            for (int id = 0; id < dat.Civilizations.Count; id++)
            {
                var civ = new Civilization(id, dat.Civilizations[id], this);
                Civilizations.Add(civ);
            }
        }
    }
}
