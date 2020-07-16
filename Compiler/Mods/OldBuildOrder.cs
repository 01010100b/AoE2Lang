using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class OldBuildOrder
    {
        public struct OldBuildElement
        {
            public readonly bool Research;
            public readonly Unit Unit;
            public readonly Technology Technology;

            public readonly bool Gatherers;
            public readonly int FoodGatherers;
            public readonly int WoodGatherers;
            public readonly int GoldGatherers;
            public readonly int StoneGatherers;

            public bool Buildable => Research == false || (Technology.Free == false && Technology.ResearchLocation != null);

            public OldBuildElement(bool research, Unit unit, Technology technology)
            {
                Research = research;
                Unit = unit;
                Technology = technology;
                Gatherers = false;
                FoodGatherers = 0;
                WoodGatherers = 0;
                GoldGatherers = 0;
                StoneGatherers = 0;
            }

            public OldBuildElement(int food, int wood, int gold, int stone)
            {
                Research = false;
                Unit = null;
                Technology = null;
                Gatherers = true;
                FoodGatherers = food;
                WoodGatherers = wood;
                GoldGatherers = gold;
                StoneGatherers = stone;
            }

            public override string ToString()
            {
                if (Gatherers)
                {
                    return $"Set gatherers {FoodGatherers} {WoodGatherers} {GoldGatherers} {StoneGatherers}";
                }
                else
                {
                    if (Research)
                    {
                        return "Research " + Technology.Id + " " + Technology.Name;
                    }
                    else
                    {
                        if (Unit.Type == 80)
                        {
                            return "Build " + Unit.Id + " " + Unit.Name;
                        }
                        else
                        {
                            return "Train " + Unit.Id + " " + Unit.Name;
                        }
                    }
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is OldBuildElement other)
                {
                    return (Research == other.Research) && (Unit == other.Unit) && (Technology == other.Technology)
                        && (Gatherers == other.Gatherers) && (FoodGatherers == other.FoodGatherers)
                        && (WoodGatherers == other.WoodGatherers) && (GoldGatherers == other.GoldGatherers)
                        && (StoneGatherers == other.StoneGatherers);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public static bool operator ==(OldBuildElement a, OldBuildElement b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(OldBuildElement a, OldBuildElement b)
            {
                return !a.Equals(b);
            }
        }

        public readonly Civilization Civilization;
        public readonly Unit Unit;
        public readonly List<OldBuildElement> Elements;
        public Cost Cost => GetCost();
        public double Score => GetScore();

        private readonly HashSet<Unit> SearchingUnits;
        private readonly HashSet<Technology> SearchingTechs;
        private readonly Dictionary<Unit, List<OldBuildElement>> KnownUnits;
        private readonly Dictionary<Technology, List<OldBuildElement>> KnownTechnologies;
        private readonly List<Unit> AvailableUnits;

        private readonly Random Random;

        internal OldBuildOrder(Civilization civilization, Unit unit, bool water = false, int seed = -1)
        {
            Random = new Random();
            if (seed >= 0)
            {
                Random = new Random(seed);
            }

            Civilization = civilization;
            Unit = unit;

            SearchingUnits = new HashSet<Unit>();
            SearchingTechs = new HashSet<Technology>();
            KnownUnits = new Dictionary<Unit, List<OldBuildElement>>();
            KnownTechnologies = new Dictionary<Technology, List<OldBuildElement>>();
            AvailableUnits = Civilization.AvailableUnits.Where(u => u.Land).ToList();

            var bo = GetUnit(unit);
            if (bo == null)
            {
                Elements = null;
            }
            else
            {
                Elements = bo.Where(e => e.Buildable).ToList();
                Clean();
            }
        }

        public List<OldBuildElement> GetTechPartial(Technology tech)
        {
            return KnownTechnologies[tech];
        }

        public List<OldBuildElement> GetUnitPartial(Unit unit)
        {
            return KnownUnits[unit];
        }

        public void AddUpgrades()
        {
            // add upgrades
            foreach (var tech in Civilization.Technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand ac)
                    {
                        if (ac.UnitId == Unit.Id || ac.Class == Unit.Class)
                        {
                            var bo = GetTech(tech);
                            if (bo != null)
                            {
                                Elements.AddRange(bo.Where(e => e.Buildable));
                            }
                        }
                    }
                }
            }

            // add unit upgrades
            var current = Unit;
            var found = true;

            while (found && current != null)
            {
                found = false;

                foreach (var tech in Civilization.Technologies.Where(t => t.Effect != null))
                {
                    foreach (var command in tech.Effect.Commands)
                    {
                        if (command is UpgradeUnitCommand uc)
                        {
                            if (uc.FromUnitId == current.Id)
                            {
                                
                                var bo = GetTech(tech);
                                Elements.AddRange(bo.Where(e => e.Buildable));

                                current = AvailableUnits.FirstOrDefault(u => u.Id == uc.ToUnitId);
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

            Clean();
        }

        public void AddEcoUpgrades()
        {
            var vills = Civilization.Units.Where(u => u.Class == UnitClass.Civilian).ToList();

            foreach (var tech in Civilization.Technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand ac)
                    {
                        var unit = vills.FirstOrDefault(u => u.Id == ac.UnitId);
                        if (unit != null || ac.Class == UnitClass.Civilian)
                        {
                            if (true)
                            {
                                var bo = GetTech(tech);
                                if (bo != null)
                                {
                                    Elements.AddRange(bo.Where(e => e.Buildable));
                                }
                            }
                        }
                    }
                }
            }

            Clean();
        }

        public void Sort()
        {
            if (Elements.Count(e => e.Gatherers) > 0)
            {
                throw new Exception("can't sort with gatherer commands");
            }

            var sorts = new List<KeyValuePair<OldBuildElement, double>>();
            foreach (var be in Elements)
            {
                sorts.Add(new KeyValuePair<OldBuildElement, double>(be, GetPriority(be)));
            }

            var set = new HashSet<OldBuildElement>();
            for (int i = 0; i < sorts.Count; i++)
            {
                var current = sorts[i];

                set.Clear();
                if (current.Key.Research)
                {
                    if (current.Key.Technology.ResearchLocation != null)
                    {
                        var bes = KnownTechnologies[current.Key.Technology].Where(e => e.Buildable).ToList();
                        foreach (var be in bes)
                        {
                            set.Add(be);
                        }
                    }
                }
                else
                {
                    var bes = KnownUnits[current.Key.Unit].Where(e => e.Buildable).ToList();
                    foreach (var be in bes)
                    {
                        set.Add(be);
                    }
                }

                for (int j = 0; j < i; j++)
                {
                    if (set.Count == 0 || (set.Count == 1 && set.First() == current.Key))
                    {
                        var other = sorts[j];
                        if (current.Value > other.Value)
                        {
                            sorts.RemoveAt(i);
                            sorts.Insert(j, current);
                            break;
                        }
                    }

                    set.Remove(sorts[j].Key);
                }
            }

            Elements.Clear();
            Elements.AddRange(sorts.Select(s => s.Key));
        }

        public void Sort(Func<OldBuildElement, bool> predicate)
        {
            var set = new HashSet<OldBuildElement>();
            var age2 = -1;
            for (int i = 0; i < Elements.Count; i++)
            {
                set.Clear();
                var current = Elements[i];

                if (current.Gatherers == false && current.Research == true && current.Technology == Civilization.Age2Tech)
                {
                    age2 = i + 1;
                }

                if (age2 == -1)
                {
                    continue;
                }

                if (current.Gatherers)
                {
                    continue;
                }

                if (!predicate(current))
                {
                    continue;
                }

                if (current.Research)
                {
                    if (current.Technology.ResearchLocation != null)
                    {
                        var bes = KnownTechnologies[current.Technology].Where(e => e.Buildable).ToList();
                        foreach (var be in bes)
                        {
                            set.Add(be);
                        }
                    }
                }
                else
                {
                    var bes = KnownUnits[current.Unit].Where(e => e.Buildable).ToList();
                    foreach (var be in bes)
                    {
                        set.Add(be);
                    }
                }

                for (int j = 0; j < age2; j++)
                {
                    set.Remove(Elements[j]);
                }

                for (int j = age2; j < i; j++)
                {
                    if (set.Count == 0 || (set.Count == 1 && set.First() == current))
                    {
                        var other = Elements[i];
                        

                        Elements.RemoveAt(i);
                        Elements.Insert(j, current);
                        break;
                    }

                    set.Remove(Elements[j]);
                }
            }
        }

        public void InsertGatherers()
        {
            var cost = new Cost();

            var start = 0;
            for (int i = 0; i < Elements.Count; i++)
            {
                var current = Elements[i];
                if (!current.Gatherers)
                {
                    Cost ccost;
                    if (current.Research)
                    {
                        ccost = current.Technology.GetCost(Civilization);
                    }
                    else
                    {
                        ccost = current.Unit.GetCost(Civilization);
                    }

                    if (current.Research == false && current.Unit == Unit)
                    {
                        ccost *= 20;
                        
                        if (Unit.BuildLocation != null)
                        {
                            ccost += Unit.BuildLocation.GetCost(Civilization) * 3;
                        }
                    }

                    cost += ccost;
                }

                bool age = false;
                if (current.Research)
                {
                    if (Civilization.Age1Tech == current.Technology)
                    {
                        age = true;
                    }
                    if (Civilization.Age2Tech == current.Technology)
                    {
                        age = true;
                    }
                    if (Civilization.Age3Tech == current.Technology)
                    {
                        age = true;
                    }
                    if (Civilization.Age4Tech == current.Technology)
                    {
                        age = true;
                    }
                }

                if (i == Elements.Count - 1)
                {
                    age = true;
                }

                if (age)
                {
                    var sum = cost.Total;
                    var gatherers = new OldBuildElement(100 * cost.Food / sum, 100 * cost.Wood / sum, 100 * cost.Gold / sum, 100 * cost.Stone / sum);
                    Elements.Insert(start, gatherers);

                    cost = new Cost();

                    start = i + 2;
                    i = start - 1;
                }
            }

            var ucost = Unit.GetCost(Civilization);
            var s = ucost.Total;
            var gath = new OldBuildElement(100 * ucost.Food / s, 100 * ucost.Wood / s, 100 * ucost.Gold / s, 100 * ucost.Stone / s);

            Elements.Add(gath);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var be in Elements)
            {
                sb.AppendLine(be.ToString());
            }

            sb.AppendLine($"Total build order cost: {Cost.Food} {Cost.Wood} {Cost.Gold} {Cost.Stone}");
            return sb.ToString();
        }

        public string Compile()
        {
            var lines = new List<string>();

            var index = 1;
            foreach (var e in Elements)
            {
                if (e.Gatherers)
                {
                    lines.Add($"; set food % to {e.FoodGatherers}");
                    lines.Add($"gl-bo-{index} = {e.FoodGatherers + 13000}");
                    index++;

                    lines.Add($"; set wood % to {e.WoodGatherers}");
                    lines.Add($"gl-bo-{index} = {e.WoodGatherers + 12000}");
                    index++;

                    lines.Add($"; set gold % to {e.GoldGatherers}");
                    lines.Add($"gl-bo-{index} = {e.GoldGatherers + 11000}");
                    index++;

                    lines.Add($"; set stone % to {e.StoneGatherers}");
                    lines.Add($"gl-bo-{index} = {e.StoneGatherers + 10000}");
                    index++;
                }
                else if (e.Research)
                {
                    lines.Add($"; research {e.Technology.Id} {e.Technology.Name}");
                    lines.Add($"gl-bo-{index} = {e.Technology.Id}");
                    index++;
                }
                else
                {
                    lines.Add($"; build/train {e.Unit.Id} {e.Unit.Name}");
                    lines.Add($"gl-bo-{index} = {-e.Unit.BaseUnit.Id}");
                    index++;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"sn-primary-unit = {Unit.BaseUnit.Id}");
            sb.AppendLine($"gl-bo-count = {index - 1}");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private List<OldBuildElement> GetUnit(Unit unit)
        {
            const int TRACK = -1;

            if (SearchingUnits.Contains(unit))
            {
                return null;
            }

            if (unit.Id == 109)
            {
                return new List<OldBuildElement>();
            }

            if (KnownUnits.ContainsKey(unit))
            {
                return KnownUnits[unit];
            }

            SearchingUnits.Add(unit);

            if (unit.Id == TRACK)
            {
                Debug.WriteLine("getting unit 1");
            }

            var bo = new List<OldBuildElement>();

            // train site
            if (unit.BuildLocation != null)
            {
                var b = GetUnit(unit.BuildLocation);
                if (b == null)
                {
                    if (unit.Id == TRACK)
                    {
                        Debug.WriteLine("getting unit 2");
                    }
                    SearchingUnits.Remove(unit);
                    return null;
                }

                bo.AddRange(b);
            }

            // required tech
            if (unit.TechRequired)
            {
                foreach (var tech in Civilization.Technologies.Where(t => t.Effect != null))
                {
                    foreach (var command in tech.Effect.Commands)
                    {
                        if (command is EnableDisableUnitCommand ec)
                        {
                            if (ec.Enable && ec.UnitId == unit.Id)
                            {
                                var b = GetTech(tech);
                                if (b != null)
                                {
                                    bo.AddRange(b);
                                    bo.Add(new OldBuildElement(false, unit, null));
                                    SearchingUnits.Remove(unit);
                                    KnownUnits.Add(unit, bo);

                                    return bo;
                                }
                            }
                        }

                        if (command is UpgradeUnitCommand uc)
                        {
                            if (uc.ToUnitId == unit.Id)
                            {
                                var b = GetTech(tech);
                                if (b != null)
                                {
                                    bo.AddRange(b);
                                    bo.Add(new OldBuildElement(false, unit, null));

                                    SearchingUnits.Remove(unit);
                                    KnownUnits.Add(unit, bo);

                                    return bo;
                                }
                            }
                        }
                    }
                }

                if (unit.Id == TRACK)
                {
                    Debug.WriteLine("getting unit 3");
                }

                SearchingUnits.Remove(unit);
                return null;
            }

            bo.Add(new OldBuildElement(false, unit, null));

            SearchingUnits.Remove(unit);
            KnownUnits.Add(unit, bo);

            return bo;
        }

        private List<OldBuildElement> GetTech(Technology tech)
        {
            const int TRACK = -1;

            if (SearchingTechs.Contains(tech))
            {
                return null;
            }

            if (tech == Civilization.Age1Tech)
            {
                return new List<OldBuildElement>();
            }

            if (KnownTechnologies.ContainsKey(tech))
            {
                return KnownTechnologies[tech];
            }

            if (tech.ResearchLocation != null)
            {
                SearchingTechs.Add(tech);
            }

            if (tech.Id == TRACK)
            {
                Debug.WriteLine("getting tech 1");
            }

            var bo = new List<OldBuildElement>();

            var prereqs = new List<Technology>();
            foreach (var prereq in tech.Prerequisites)
            {
                if (tech.Id == TRACK)
                {
                    Debug.WriteLine("getting tech preq " + prereq.Id);
                }

                var b = GetTech(prereq);
                if (b != null)
                {
                    prereqs.Add(prereq);
                }
            }

            if (prereqs.Count < tech.MinPrerequisites)
            {
                if (tech.Id == TRACK)
                {
                    Debug.WriteLine("getting tech 2");
                }

                SearchingTechs.Remove(tech);
                return null;
            }

            if (tech.MinPrerequisites > 0)
            {
                while (prereqs.Count > tech.MinPrerequisites)
                {
                    prereqs.RemoveAt(Random.Next(prereqs.Count));
                }

                foreach (var prereq in prereqs)
                {
                    bo.AddRange(GetTech(prereq));
                }
            }
            else
            {
                if (tech.Id == TRACK)
                {
                    Debug.WriteLine("getting tech 3");
                }

                List<OldBuildElement> b = null;
                foreach (var unit in AvailableUnits.Where(u => u.TechInitiated == tech))
                {
                    if (tech.Id == TRACK)
                    {
                        Debug.WriteLine("checking tech unit " + unit.Id);
                    }

                    b = GetUnit(unit);
                    if (b != null)
                    {
                        bo.AddRange(b);
                        break;
                    }
                }

                if (b == null)
                {
                    return null;
                }
            }

            if (tech.ResearchLocation != null)
            {
                if (tech.Id == TRACK)
                {
                    Debug.WriteLine("getting tech 4");
                }

                var b = GetUnit(tech.ResearchLocation);
                if (b == null)
                {
                    SearchingTechs.Remove(tech);
                    return null;
                }

                bo.AddRange(b);
            }

            bo.Add(new OldBuildElement(true, null, tech));

            SearchingTechs.Remove(tech);
            KnownTechnologies.Add(tech, bo);

            if (tech.Id == TRACK)
            {
                Debug.WriteLine("getting tech 5");
            }

            return bo;
        }

        private Cost GetCost()
        {
            var cost = new Cost();

            foreach (var be in Elements)
            {
                if (!be.Gatherers)
                {
                    if (be.Research)
                    {
                        cost += be.Technology.GetCost(Civilization);
                    }
                    else
                    {
                        cost += be.Unit.GetCost(Civilization);
                    }
                }
            }

            return cost;
        }

        private double GetScore()
        {
            var score = 0d;
            
            var cost = GetCost();
            score += cost.Total;

            return 1 / score;
        }

        private int GetPriority(OldBuildElement be)
        {
            if (be.Gatherers)
            {
                return -1;
            }

            var prior = 0;

            if (be.Research == true && be.Technology.ResourceImproved == Resource.Stone)
            {
                prior = 20;
            }

            if (be.Research == true && be.Technology.ResourceImproved == Resource.Gold)
            {
                prior = 30;
            }

            if (be.Research == true && be.Technology.ResourceImproved == Resource.Food)
            {
                prior = 40;
            }

            if (be.Research == true && be.Technology.ResourceImproved == Resource.Wood)
            {
                prior = 50;
            }

            if (be.Research == false && be.Unit == Civilization.GetDropSite(Resource.Stone))
            {
                prior = 60;
            }

            if (be.Research == false && be.Unit == Civilization.GetDropSite(Resource.Gold))
            {
                prior = 70;
            }

            if (be.Research == false && be.Unit == Civilization.GetDropSite(Resource.Food))
            {
                prior = 80;
            }

            if (be.Research == false && be.Unit == Civilization.GetDropSite(Resource.Wood))
            {
                prior = 90;
            }

            if (be.Research == true && (be.Technology == Civilization.Age1Tech || be.Technology == Civilization.Age2Tech
                || be.Technology == Civilization.Age3Tech || be.Technology == Civilization.Age4Tech))
            {
                prior = 100;
            }

            if (be.Research == true && be.Technology.Effect.Commands.Count(c => c is AttributeModifierCommand) > 0)
            {
                prior = 120;
            }

            if (be.Research == false && be.Unit == Unit.BuildLocation)
            {
                prior = 150;
            }

            if (be.Research == true && be.Technology.Effect.Commands.Count(c =>
            {
                if (c is EnableDisableUnitCommand ec)
                {
                    if (ec.Enable == true && ec.UnitId == Unit.Id)
                    {
                        return true;
                    }
                }

                return false;
            }) > 0)
            {
                prior = 150;
            }

            if (be.Research == true && be.Technology.Effect.Commands.Count(c => c is UpgradeUnitCommand) > 0)
            {
                prior = 200;
            }

            if (be.Research == false && be.Unit == Unit)
            {
                prior = 300;
            }

            return prior;
        }

        private void Clean()
        {
            var clean = Elements.Distinct().ToList();
            Elements.Clear();
            Elements.AddRange(clean);
        }
    }
}
