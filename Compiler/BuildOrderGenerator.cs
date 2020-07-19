using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Compiler.BuildOrderGenerator.BuildOrderElement;

namespace Compiler
{
    class BuildOrderGenerator
    {
        public abstract class BuildOrderElement
        {
            public enum BuildOrderElementCategory
            {
                NONE, AGE_UP, PRIMARY_UNITUPGR, PRIMARY_STATUPGR, SECONDARY_UNITUPGR, SECONDARY_STATUPGR, SIEGE_UNITUPGR, SIEGE_STATUPGR, ECO_UPGR,
                PRIMARY_TRAINSITE, SECONDARY_TRAINSITE, SIEGE_TRAINSITE, PRIMARY_TRAIN, SECONDARY_TRAIN, SIEGE_TRAIN, WOOD_DROPSITE, FOOD_DROPSITE,
                GOLD_DROPSITE, STONE_DROPSITE
            }

            public BuildOrderElementCategory Category { get; set; }
            public double Priority { get; set; }
            public abstract bool Buildable { get; }

            public class GatherersBuildElement : BuildOrderElement
            {
                public override bool Buildable => true;

                public readonly int Food;
                public readonly int Wood;
                public readonly int Gold;
                public readonly int Stone;

                public GatherersBuildElement(int food, int wood, int gold, int stone)
                {
                    Food = food;
                    Wood = wood;
                    Gold = gold;
                    Stone = stone;
                }

                public override string ToString()
                {
                    return $"Set gatherers to {Food} {Wood} {Gold} {Stone}";
                }

                public override bool Equals(object obj)
                {
                    if (obj is GatherersBuildElement other)
                    {
                        return Food == other.Food && Wood == other.Wood && Gold == other.Gold && Stone == other.Stone;
                    }
                    else
                    {
                        return false;
                    }
                }

                public override int GetHashCode()
                {
                    return Food.GetHashCode() ^ Wood.GetHashCode() ^ Gold.GetHashCode() ^ Stone.GetHashCode();
                }
            }

            public class ResearchBuildElement : BuildOrderElement
            {
                public override bool Buildable => Technology.Free == false && Technology.ResearchLocation != null;

                public readonly Technology Technology;

                public ResearchBuildElement(Technology technology)
                {
                    Technology = technology;
                }

                public override string ToString()
                {
                    return $"Research {Technology.Id} {Technology.Name} {Category} {(int)Priority}";
                }

                public override bool Equals(object obj)
                {
                    if (obj is ResearchBuildElement re)
                    {
                        return Technology == re.Technology;
                    }
                    else
                    {
                        return false;
                    }
                }

                public override int GetHashCode()
                {
                    return Technology.GetHashCode();
                }
            }

            public class BuildBuildElement : BuildOrderElement
            {
                public override bool Buildable => true;

                public readonly Unit Unit;

                public BuildBuildElement(Unit unit)
                {
                    Unit = unit;
                }

                public override string ToString()
                {
                    if (Unit.Type == 80)
                    {
                        return $"Build {Unit.Id} {Unit.Name} {Category} {(int)Priority}";
                    }
                    else
                    {
                        return $"Train {Unit.Id} {Unit.Name} {Category} {(int)Priority}";
                    }
                }

                public override bool Equals(object obj)
                {
                    if (obj is BuildBuildElement other)
                    {
                        return Unit == other.Unit;
                    }
                    else
                    {
                        return false;
                    }
                }
                public override int GetHashCode()
                {
                    return Unit.GetHashCode();
                }
            }
        }

        

        public readonly Civilization Civilization;

        private readonly Dictionary<Unit, UnitStats> PossibleUnits = new Dictionary<Unit, UnitStats>();
        private readonly Dictionary<Technology, TechnologyStats> PossibleTechnologies = new Dictionary<Technology, TechnologyStats>();
        private readonly Dictionary<Unit, HashSet<Technology>> UnitMakeAvailableTechnologies = new Dictionary<Unit, HashSet<Technology>>();
        private readonly HashSet<Technology> EcoUpgrades = new HashSet<Technology>();
        private readonly Dictionary<Unit, HashSet<Technology>> UnitUpgrades = new Dictionary<Unit, HashSet<Technology>>();
        private readonly Dictionary<Resource, Unit> Dropsites = new Dictionary<Resource, Unit>();

        private readonly Dictionary<Technology, List<BuildOrderElement>> SolvedTechnologies = new Dictionary<Technology, List<BuildOrderElement>>();
        private readonly Dictionary<Unit, List<BuildOrderElement>> SolvedUnits = new Dictionary<Unit, List<BuildOrderElement>>();
        private readonly HashSet<Technology> SearchedTechnologies = new HashSet<Technology>();
        private readonly HashSet<Unit> SearchedUnits = new HashSet<Unit>();

        private readonly Random Random;

        public BuildOrderGenerator(Civilization civilization)
        {
            Civilization = civilization;

            PossibleUnits.Clear();
            UnitUpgrades.Clear();
            foreach (var unit in civilization.Units.Where(u => u.Available || u.TechRequired).Where(u => u.Land))
            {
                PossibleUnits.Add(unit, new UnitStats(unit, null));
                UnitUpgrades.Add(unit, new HashSet<Technology>());
            }

            PossibleTechnologies.Clear();
            foreach (var tech in civilization.Technologies)
            {
                PossibleTechnologies.Add(tech, new TechnologyStats(tech, null));
            }

            UnitMakeAvailableTechnologies.Clear();
            EcoUpgrades.Clear();

            var vills = Civilization.Units.Where(u => u.Class == UnitClass.Civilian).ToList();
            foreach (var tech in PossibleTechnologies.Keys.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is EnableDisableUnitCommand ec)
                    {
                        if (ec.Enable)
                        {
                            var unit = PossibleUnits.Keys.FirstOrDefault(u => u.Id == ec.UnitId);
                            if (unit != null)
                            {
                                if (!UnitMakeAvailableTechnologies.ContainsKey(unit))
                                {
                                    UnitMakeAvailableTechnologies.Add(unit, new HashSet<Technology>());
                                }

                                UnitMakeAvailableTechnologies[unit].Add(tech);
                            }
                        }
                    }

                    if (command is UpgradeUnitCommand uc)
                    {
                        var unit = PossibleUnits.Keys.FirstOrDefault(u => u.Id == uc.ToUnitId);
                        if (unit != null)
                        {
                            if (!UnitMakeAvailableTechnologies.ContainsKey(unit))
                            {
                                UnitMakeAvailableTechnologies.Add(unit, new HashSet<Technology>());
                            }

                            UnitMakeAvailableTechnologies[unit].Add(tech);
                        }

                        unit = PossibleUnits.Keys.FirstOrDefault(u => u.Id == uc.FromUnitId);
                        if (unit != null)
                        {
                            UnitUpgrades[unit].Add(tech);
                        }
                    }

                    if (command is AttributeModifierCommand ac)
                    {
                        var vill = vills.FirstOrDefault(u => u.Id == ac.UnitId);
                        if (vill != null || ac.Class == UnitClass.Civilian)
                        {
                            EcoUpgrades.Add(tech);
                        }

                        foreach (var unit in PossibleUnits.Keys)
                        {
                            if (ac.UnitId == unit.Id || ac.Class == unit.Class)
                            {
                                UnitUpgrades[unit].Add(tech);
                            }
                        }
                    }
                }
            }

            Dropsites.Clear();
            foreach (var resource in Enum.GetValues(typeof(Resource)).Cast<Resource>())
            {
                var site = Civilization.GetDropSite(resource);
                if (site != null)
                {
                    Dropsites.Add(resource, site);
                }
            }

            Random = new Random(DateTime.UtcNow.Ticks.GetHashCode() ^ Guid.NewGuid().GetHashCode() ^ civilization.Id.GetHashCode());
        }

        public List<BuildOrderElement> GetBuildOrder(Unit primary, Unit secondary, Unit siege, bool upgrades, bool eco, int attempts)
        {
            if (primary == null)
            {
                throw new ArgumentNullException(nameof(primary));
            }

            var starting_techs = new List<Technology>() { Civilization.Age1Tech };

            var best = new List<BuildOrderElement>();
            var best_cost = double.MaxValue;

            var current = new List<BuildOrderElement>();

            for (int i = 0; i < attempts; i++)
            {
                SolvedTechnologies.Clear();
                SolvedUnits.Clear();
                SearchedTechnologies.Clear();
                SearchedUnits.Clear();

                foreach (var tech in starting_techs)
                {
                    SolvedTechnologies.Add(tech, new List<BuildOrderElement>());
                }

                current.Clear();

                SolveUnit(primary);

                if (!SolvedUnits.ContainsKey(primary))
                {
                    return null;
                }

                current.AddRange(SolvedUnits[primary]);

                if (secondary != null)
                {
                    SolveUnit(secondary);

                    if (!SolvedUnits.ContainsKey(secondary))
                    {
                        return null;
                    }

                    current.AddRange(SolvedUnits[secondary]);
                }

                if (siege != null)
                {
                    SolveUnit(siege);

                    if (!SolvedUnits.ContainsKey(siege))
                    {
                        return null;
                    }

                    current.AddRange(SolvedUnits[siege]);
                }

                if (upgrades == true)
                {
                    var trained = new List<Unit>() { primary, secondary, siege }.Where(u => u != null);
                    foreach (var unit in trained)
                    {
                        if (UnitUpgrades.TryGetValue(unit, out HashSet<Technology> techs))
                        {
                            foreach (var tech in techs)
                            {
                                SolveTechnology(tech);

                                if (SolvedTechnologies.TryGetValue(tech, out List<BuildOrderElement> bo))
                                {
                                    current.AddRange(bo);
                                }
                            }
                        }
                    }
                    
                }

                if (eco == true)
                {
                    foreach (var tech in EcoUpgrades)
                    {
                        SolveTechnology(tech);

                        if (SolvedTechnologies.TryGetValue(tech, out List<BuildOrderElement> bo))
                        {
                            current.AddRange(bo);
                        }
                    }
                }

                current = current.Where(e => e.Buildable).Distinct().ToList();


                var cost = OldSortAndScore(current, primary, secondary, siege);

                if (cost < best_cost)
                {
                    best.Clear();
                    best.AddRange(current);
                    best_cost = cost;
                }
            }

            return best;
        }

        private void SolveUnit(Unit unit)
        {
            const int TRACK = 0;
            if (unit.Id == TRACK)
            {
                Log.Debug("trying unit " + TRACK);
            }

            if (SearchedUnits.Contains(unit))
            {
                return;
            }

            if (SolvedUnits.ContainsKey(unit))
            {
                return;
            }

            if (unit.Id == 109)
            {
                var solution = new List<BuildOrderElement>();
                SolvedUnits.Add(unit, solution);

                return;
            }

            if (!PossibleUnits.ContainsKey(unit))
            {
                if (unit.Id == TRACK)
                {
                    Log.Debug("unit not possible " + TRACK);
                }
                return;
            }

            SearchedUnits.Add(unit);

            var bo = new List<BuildOrderElement>();

            // train site
            if (unit.BuildLocation != null)
            {
                SolveUnit(unit.BuildLocation);

                if (SolvedUnits.TryGetValue(unit.BuildLocation, out List<BuildOrderElement> b))
                {
                    bo.AddRange(b);
                }
                else
                {
                    if (unit.Id == TRACK)
                    {
                        Log.Debug("unit " + TRACK + " train site failed " + unit.BuildLocation.Id);
                    }
                    SearchedUnits.Remove(unit);
                    return;
                }
            }

            // req tech
            if (unit.TechRequired)
            {
                if (!UnitMakeAvailableTechnologies.ContainsKey(unit))
                {
                    if (unit.Id == TRACK)
                    {
                        Log.Debug("unit tech failed " + TRACK);
                    }

                    SearchedUnits.Remove(unit);

                    return;
                }

                var available_techs = new List<Technology>();

                foreach (var tech in UnitMakeAvailableTechnologies[unit])
                {
                    SolveTechnology(tech);

                    if (SolvedTechnologies.ContainsKey(tech))
                    {
                        if (unit.Id == TRACK)
                        {
                            Log.Debug("unit " + TRACK + " available tech " + tech.Id);
                        }

                        available_techs.Add(tech);
                    }
                }

                if (available_techs.Count > 0)
                {
                    while (available_techs.Count > 1)
                    {
                        available_techs.RemoveAt(Random.Next(available_techs.Count));
                    }

                    bo.AddRange(SolvedTechnologies[available_techs[0]]);
                }
                else
                {
                    if (unit.Id == TRACK)
                    {
                        Log.Debug("unit tech failed " + TRACK);
                    }

                    SearchedUnits.Remove(unit);

                    return;
                }
            }

            bo.Add(new BuildBuildElement(unit));
            
            SolvedUnits.Add(unit, bo.Distinct().ToList());
            SearchedUnits.Remove(unit);
        }

        private void SolveTechnology(Technology technology)
        {
            const int TRACK = 0;
            
            if (SearchedTechnologies.Contains(technology))
            {
                return;
            }

            if (SolvedTechnologies.ContainsKey(technology))
            {
                return;
            }

            if (technology.Id == TRACK)
            {
                Log.Debug("searching tech " + TRACK);
            }

            if (!PossibleTechnologies.ContainsKey(technology))
            {
                if (technology.Id == TRACK)
                {
                    Log.Debug("tech not possible " + TRACK);
                }
                return;
            }

            SearchedTechnologies.Add(technology);

            var bo = new List<BuildOrderElement>();

            if (technology.Id == TRACK)
            {
                Log.Debug("tech get research " + TRACK);
            }

            // research location
            if (technology.ResearchLocation != null)
            {
                SolveUnit(technology.ResearchLocation);

                if (SolvedUnits.TryGetValue(technology.ResearchLocation, out List<BuildOrderElement> rl))
                {
                    bo.AddRange(rl);
                }
                else
                {
                    if (technology.Id == TRACK)
                    {
                        Log.Debug("failed tech " + TRACK + " loc " + technology.ResearchLocation.Id);
                    }
                    SearchedTechnologies.Remove(technology);
                    return;
                }
            }

            if (technology.Id == TRACK)
            {
                Log.Debug("tech get prereqs " + TRACK);
            }

            // prereqs
            if (technology.MinPrerequisites > 0)
            {
                var prereqs = new List<Technology>();
                foreach (var tech in technology.Prerequisites)
                {
                    if (SolvedTechnologies.ContainsKey(tech))
                    {
                        prereqs.Add(tech);
                    }
                }

                if (prereqs.Count < technology.MinPrerequisites)
                {
                    prereqs.Clear();

                    foreach (var tech in technology.Prerequisites)
                    {
                        SolveTechnology(tech);

                        if (SolvedTechnologies.ContainsKey(tech))
                        {
                            prereqs.Add(tech);
                        }
                    }
                }

                if (prereqs.Count < technology.MinPrerequisites)
                {
                    if (technology.Id == TRACK)
                    {
                        Log.Debug("failed tech " + TRACK + " prereq " + prereqs.Count);
                        foreach (var p in prereqs)
                        {
                            Log.Debug("  prereq: " + p.Id + " " + p.Name);
                        }
                    }
                    SearchedTechnologies.Remove(technology);
                    return;
                }

                while (prereqs.Count > technology.MinPrerequisites)
                {
                    prereqs.RemoveAt(Random.Next(prereqs.Count));
                }

                foreach (var prereq in prereqs)
                {
                    if (technology.Id == TRACK)
                    {
                        Log.Debug("tech " + TRACK + " adding prereq " + prereq.Id);
                    }

                    bo.AddRange(SolvedTechnologies[prereq]);
                }
            }
            else
            {
                var available_units = PossibleUnits.Keys.Where(u => u.TechInitiated == technology).ToList();

                var possible_units = new List<Unit>();

                foreach (var unit in available_units)
                {
                    if (SolvedUnits.TryGetValue(unit, out List<BuildOrderElement> ubo))
                    {
                        possible_units.Add(unit);
                    }
                }

                if (possible_units.Count == 0)
                {
                    foreach (var unit in available_units)
                    {
                        SolveUnit(unit);

                        if (SolvedUnits.TryGetValue(unit, out List<BuildOrderElement> ubo))
                        {
                            possible_units.Add(unit);
                        }
                    }
                }

                if (possible_units.Count == 0)
                {
                    if (technology.Id == TRACK)
                    {
                        Log.Debug("failed tech " + TRACK + " units");
                    }
                    SearchedTechnologies.Remove(technology);
                    return;
                }

                bo.AddRange(SolvedUnits[possible_units[0]]);
            }

            bo.Add(new ResearchBuildElement(technology));

            SolvedTechnologies.Add(technology, bo.Distinct().ToList());
            SearchedTechnologies.Remove(technology);

            if (technology.Id == TRACK)
            {
                Log.Debug("found tech");
            }
        }

        private double OldSortAndScore(List<BuildOrderElement> bo, Unit primary, Unit secondary, Unit siege)
        {
            const double EXTRA_COST_FACTOR = 10d;

            const int AGE_COST = 100;
            const int ECO_COST = 150;
            const int UNIT_STATUPGRADE_COST = 200;
            const int UNIT_UPGRADE_COST = 300;
            const int UNIT_TRAINSITE_COST = 400;
            const int DROPSITE_COST = 450;
            const int UNIT_TRAIN_COST = 500;

            AssignCategories(bo, primary, secondary, siege);

            var unavailable = new Dictionary<BuildOrderElement, HashSet<BuildOrderElement>>();
            var available = new HashSet<BuildOrderElement>();

            foreach (var be in bo)
            {
                var deps = new HashSet<BuildOrderElement>();
                List<BuildOrderElement> bes = null;
                if (be is ResearchBuildElement re)
                {
                    bes = SolvedTechnologies[re.Technology].Where(e => e.Buildable).ToList();
                }
                else if (be is BuildBuildElement bbe)
                {
                    bes = SolvedUnits[bbe.Unit].Where(e => e.Buildable).ToList();
                }

                bes.RemoveAt(bes.Count - 1);

                foreach (var bobe in bes)
                {
                    if (bo.Contains(bobe))
                    {
                        deps.Add(bobe);
                    }
                }

                if (deps.Count == 0)
                {
                    available.Add(be);
                }
                else
                {
                    unavailable.Add(be, deps);
                }
            }

            bo.Clear();

            var score = 0d;

            var training_primary = false;
            var training_secondary = false;
            var training_siege = false;

            var age_primary = 1;
            var age_secondary = 1;
            var age_siege = 1;

            if (primary != null)
            {
                age_primary = primary.GetAge(Civilization);
            }
            if (secondary != null)
            {
                age_secondary = secondary.GetAge(Civilization);
            }
            if (siege != null)
            {
                age_siege = siege.GetAge(Civilization);
            }

            var current_age = 1;

            while (available.Count > 0)
            {
                var best = available.First();
                var best_cost = int.MinValue;

                foreach (var be in available)
                {
                    var cost = 0;

                    switch (be.Category)
                    {
                        case BuildOrderElementCategory.AGE_UP: cost = AGE_COST; break;
                        case BuildOrderElementCategory.ECO_UPGR: cost = ECO_COST; break;

                        case BuildOrderElementCategory.PRIMARY_STATUPGR: cost = training_primary ? UNIT_STATUPGRADE_COST + 30 : 0; break;
                        case BuildOrderElementCategory.PRIMARY_TRAIN: cost = UNIT_TRAIN_COST + 30; break;
                        case BuildOrderElementCategory.PRIMARY_TRAINSITE: cost = current_age >= age_primary ? UNIT_TRAINSITE_COST + 30 : 0; break;
                        case BuildOrderElementCategory.PRIMARY_UNITUPGR: cost = current_age >= age_primary ? UNIT_UPGRADE_COST + 30 : 0; break;

                        case BuildOrderElementCategory.SECONDARY_STATUPGR: cost = training_secondary ? UNIT_STATUPGRADE_COST + 20 : 0; break;
                        case BuildOrderElementCategory.SECONDARY_TRAIN: cost = UNIT_TRAIN_COST + 20; break;
                        case BuildOrderElementCategory.SECONDARY_TRAINSITE: cost = current_age >= age_secondary ? UNIT_TRAINSITE_COST + 20 : 0; break;
                        case BuildOrderElementCategory.SECONDARY_UNITUPGR: cost = current_age >= age_secondary ? UNIT_UPGRADE_COST + 20 : 0; break;

                        case BuildOrderElementCategory.SIEGE_STATUPGR: cost = training_siege ? UNIT_STATUPGRADE_COST + 10 : 0; break;
                        case BuildOrderElementCategory.SIEGE_TRAIN: cost = UNIT_TRAIN_COST + 10; break;
                        case BuildOrderElementCategory.SIEGE_TRAINSITE: cost = current_age >= age_siege ? UNIT_TRAINSITE_COST + 10 : 0; break;
                        case BuildOrderElementCategory.SIEGE_UNITUPGR: cost = current_age >= age_siege ? UNIT_UPGRADE_COST + 10 : 0; break;

                        case BuildOrderElementCategory.FOOD_DROPSITE: cost = DROPSITE_COST + 20; break;
                        case BuildOrderElementCategory.WOOD_DROPSITE: cost = DROPSITE_COST + 30; break;
                        case BuildOrderElementCategory.GOLD_DROPSITE: cost = DROPSITE_COST + 10; break;
                        case BuildOrderElementCategory.STONE_DROPSITE: cost = DROPSITE_COST; break;
                    }

                    if (cost > best_cost)
                    {
                        best = be;
                        best_cost = cost;
                    }
                }

                bo.Add(best);

                best.Priority = best_cost;

                score += EXTRA_COST_FACTOR * best_cost * bo.Count;
                if (best is ResearchBuildElement re)
                {
                    score += PossibleTechnologies[re.Technology].Cost.Total;
                }
                else if (best is BuildBuildElement bbe)
                {
                    score += PossibleUnits[bbe.Unit].Cost.Total;
                }

                available.Remove(best);
                foreach (var kvp in unavailable.ToList())
                {
                    kvp.Value.Remove(best);
                    if (kvp.Value.Count == 0)
                    {
                        unavailable.Remove(kvp.Key);
                        available.Add(kvp.Key);
                    }
                }

                switch (best.Category)
                {
                    case BuildOrderElementCategory.AGE_UP: current_age++; break;
                    case BuildOrderElementCategory.PRIMARY_TRAIN: training_primary = true; break;
                    case BuildOrderElementCategory.SECONDARY_TRAIN: training_secondary = true; break;
                    case BuildOrderElementCategory.SIEGE_TRAIN: training_siege = true; break;
                }
            }

            return score;
        }

        private void AssignCategories(List<BuildOrderElement> bo, Unit primary, Unit secondary, Unit siege)
        {
            foreach (var be in bo)
            {
                be.Category = BuildOrderElementCategory.NONE;

                if (be is ResearchBuildElement re)
                {
                    if (re.Technology == Civilization.Age1Tech || re.Technology == Civilization.Age2Tech
                        || re.Technology == Civilization.Age3Tech || re.Technology == Civilization.Age4Tech)
                    {
                        be.Category = BuildOrderElementCategory.AGE_UP;
                    }
                    else if (EcoUpgrades.Contains(re.Technology))
                    {
                        be.Category = BuildOrderElementCategory.ECO_UPGR;
                    }
                    else if (primary != null && UnitUpgrades[primary].Contains(re.Technology))
                    {
                        foreach (var command in re.Technology.Effect.Commands)
                        {
                            if (command is EnableDisableUnitCommand ec)
                            {
                                if (ec.Enable)
                                {
                                    be.Category = BuildOrderElementCategory.PRIMARY_UNITUPGR;
                                    break;
                                }
                            }
                            else if (command is UpgradeUnitCommand uc)
                            {
                                be.Category = BuildOrderElementCategory.PRIMARY_UNITUPGR;
                                break;
                            }
                            else if (command is AttributeModifierCommand ac)
                            {
                                be.Category = BuildOrderElementCategory.PRIMARY_STATUPGR;
                                break;
                            }
                        }
                    }
                    else if (secondary != null && UnitUpgrades[secondary].Contains(re.Technology))
                    {
                        foreach (var command in re.Technology.Effect.Commands)
                        {
                            if (command is EnableDisableUnitCommand ec)
                            {
                                if (ec.Enable)
                                {
                                    be.Category = BuildOrderElementCategory.SECONDARY_UNITUPGR;
                                    break;
                                }
                            }
                            else if (command is UpgradeUnitCommand uc)
                            {
                                be.Category = BuildOrderElementCategory.SECONDARY_UNITUPGR;
                                break;
                            }
                            else if (command is AttributeModifierCommand ac)
                            {
                                be.Category = BuildOrderElementCategory.SECONDARY_STATUPGR;
                                break;
                            }
                        }
                    }
                    else if (siege != null && UnitUpgrades[primary].Contains(re.Technology))
                    {
                        foreach (var command in re.Technology.Effect.Commands)
                        {
                            if (command is EnableDisableUnitCommand ec)
                            {
                                if (ec.Enable)
                                {
                                    be.Category = BuildOrderElementCategory.SIEGE_UNITUPGR;
                                    break;
                                }
                            }
                            else if (command is UpgradeUnitCommand uc)
                            {
                                be.Category = BuildOrderElementCategory.SIEGE_UNITUPGR;
                                break;
                            }
                            else if (command is AttributeModifierCommand ac)
                            {
                                be.Category = BuildOrderElementCategory.SIEGE_STATUPGR;
                                break;
                            }
                        }
                    }
                }
                else if (be is BuildBuildElement bbe)
                {
                    if (Dropsites.TryGetValue(Resource.Food, out Unit site))
                    {
                        if (bbe.Unit == site)
                        {
                            be.Category = BuildOrderElementCategory.FOOD_DROPSITE;
                            continue;
                        }
                    }

                    if (Dropsites.TryGetValue(Resource.Wood, out site))
                    {
                        if (bbe.Unit == site)
                        {
                            be.Category = BuildOrderElementCategory.WOOD_DROPSITE;
                            continue;
                        }
                    }

                    if (Dropsites.TryGetValue(Resource.Gold, out site))
                    {
                        if (bbe.Unit == site)
                        {
                            be.Category = BuildOrderElementCategory.GOLD_DROPSITE;
                            continue;
                        }
                    }

                    if (Dropsites.TryGetValue(Resource.Stone, out site))
                    {
                        if (bbe.Unit == site)
                        {
                            be.Category = BuildOrderElementCategory.STONE_DROPSITE;
                            continue;
                        }
                    }

                    if (primary != null)
                    {
                        if (primary == bbe.Unit)
                        {
                            be.Category = BuildOrderElementCategory.PRIMARY_TRAIN;
                            continue;
                        }
                        else if (primary.BuildLocation == bbe.Unit)
                        {
                            be.Category = BuildOrderElementCategory.PRIMARY_TRAINSITE;
                            continue;
                        }
                    }

                    if (secondary != null)
                    {
                        if (secondary == bbe.Unit)
                        {
                            be.Category = BuildOrderElementCategory.SECONDARY_TRAIN;
                            continue;
                        }
                        else if (secondary.BuildLocation == bbe.Unit)
                        {
                            be.Category = BuildOrderElementCategory.SECONDARY_TRAINSITE;
                            continue;
                        }
                    }

                    if (siege != null)
                    {
                        if (siege == bbe.Unit)
                        {
                            be.Category = BuildOrderElementCategory.SIEGE_TRAIN;
                            continue;
                        }
                        else if (siege.BuildLocation == bbe.Unit)
                        {
                            be.Category = BuildOrderElementCategory.SIEGE_TRAINSITE;
                            continue;
                        }
                    }
                }
            }
        }
    }
}
