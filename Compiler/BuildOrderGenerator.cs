using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Compiler.BuildOrderGenerator.BuildOrderElement;

namespace Compiler
{
    class BuildOrderGenerator
    {
        public abstract class BuildOrderElement
        {
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
                    return $"Research {Technology.Id} {Technology.Name}";
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
                        return $"Build {Unit.Id} {Unit.Name}";
                    }
                    else
                    {
                        return $"Train {Unit.Id} {Unit.Name}";
                    }
                }
            }
        }

        public readonly Civilization Civilization;

        private readonly Dictionary<Unit, UnitStats> PossibleUnits = new Dictionary<Unit, UnitStats>();
        private readonly Dictionary<Technology, TechnologyStats> PossibleTechnologies = new Dictionary<Technology, TechnologyStats>();

        private readonly Dictionary<Technology, List<BuildOrderElement>> SolvedTechnologies = new Dictionary<Technology, List<BuildOrderElement>>();
        private readonly Dictionary<Unit, List<BuildOrderElement>> SolvedUnits = new Dictionary<Unit, List<BuildOrderElement>>();
        private readonly HashSet<Technology> SearchedTechnologies = new HashSet<Technology>();
        private readonly HashSet<Unit> SearchedUnits = new HashSet<Unit>();

        private readonly Random Random;

        public BuildOrderGenerator(Civilization civilization)
        {
            Civilization = civilization;

            PossibleUnits.Clear();
            foreach (var unit in civilization.Units.Where(u => u.Available || u.TechRequired).Where(u => u.Land))
            {
                PossibleUnits.Add(unit, new UnitStats(unit, null));
            }

            PossibleTechnologies.Clear();
            foreach (var tech in civilization.Technologies)
            {
                PossibleTechnologies.Add(tech, new TechnologyStats(tech, null));
            }

            Random = new Random(DateTime.UtcNow.Ticks.GetHashCode() ^ Guid.NewGuid().GetHashCode() ^ civilization.Id.GetHashCode());
        }

        public List<BuildOrderElement> GetBuildOrder(Unit primary, Unit secondary, Unit siege, int attempts)
        {
            if (primary == null)
            {
                throw new ArgumentNullException(nameof(primary));
            }

            var best = new List<BuildOrderElement>();
            var best_cost = int.MaxValue;

            var current = new List<BuildOrderElement>();

            for (int i = 0; i < attempts; i++)
            {
                SolvedTechnologies.Clear();
                SolvedUnits.Clear();
                SearchedTechnologies.Clear();
                SearchedUnits.Clear();

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

                current = current.Where(e => e.Buildable).Distinct().ToList();

                var cost = 0;
                foreach (var be in current)
                {
                    if (be is ResearchBuildElement re)
                    {
                        cost += PossibleTechnologies[re.Technology].Cost.Total;
                    }
                    else if (be is BuildBuildElement bbe)
                    {
                        cost += PossibleUnits[bbe.Unit].Cost.Total;
                    }
                }

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
                Log.Debug("trying unit");
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
                    SearchedUnits.Remove(unit);
                    return;
                }
            }

            // req tech
            if (unit.TechRequired)
            {
                if (unit.EnablingTechnology != null)
                {
                    SolveTechnology(unit.EnablingTechnology);
                }
                if (unit.UpgradeTechnology != null)
                {
                    SolveTechnology(unit.UpgradeTechnology);
                }

                if (unit.EnablingTechnology != null && SolvedTechnologies.TryGetValue(unit.EnablingTechnology, out List<BuildOrderElement> et))
                {
                    bo.AddRange(et);
                }
                else if (unit.UpgradeTechnology != null && SolvedTechnologies.TryGetValue(unit.UpgradeTechnology, out List<BuildOrderElement> ut))
                {
                    bo.AddRange(ut);
                }
                else
                {
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
            if (technology.Id == TRACK)
            {
                Log.Debug("trying tech");
            }

            if (SearchedTechnologies.Contains(technology))
            {
                return;
            }

            if (SolvedTechnologies.ContainsKey(technology))
            {
                return;
            }

            if (!PossibleTechnologies.ContainsKey(technology))
            {
                if (technology.Id == TRACK)
                {
                    Log.Debug("tech not possible");
                }
                return;
            }

            SearchedTechnologies.Add(technology);

            var bo = new List<BuildOrderElement>();

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
                        Log.Debug("failed tech loc " + technology.ResearchLocation.Id);
                    }
                    SearchedTechnologies.Remove(technology);
                    return;
                }
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
                        Log.Debug("failed tech prereq " + prereqs.Count);
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
                    bo.AddRange(SolvedTechnologies[prereq]);
                }
            }
            else
            {
                var available_units = PossibleUnits.Keys.Where(u => u.TechInitiated == technology).ToList();

                if (available_units.Count > 0)
                {
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
                            Log.Debug("failed tech units");
                        }
                        SearchedTechnologies.Remove(technology);
                        return;
                    }

                    bo.AddRange(SolvedUnits[possible_units[0]]);
                }
            }

            bo.Add(new ResearchBuildElement(technology));

            SolvedTechnologies.Add(technology, bo.Distinct().ToList());
            SearchedTechnologies.Remove(technology);

            if (technology.Id == TRACK)
            {
                Log.Debug("found tech");
            }
        }
    }
}
