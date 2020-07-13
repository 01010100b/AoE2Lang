using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class BuildOrder
    {
        public struct BuildElement
        {
            public readonly bool Research;
            public readonly Unit Unit;
            public readonly Technology Technology;

            public readonly bool Gatherers;
            public readonly int FoodGatherers;
            public readonly int WoodGatherers;
            public readonly int GoldGatherers;
            public readonly int StoneGatherers;

            public bool Buildable => !(Research && Technology.ResearchLocation == null);

            public BuildElement(bool research, Unit unit, Technology technology)
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

            public BuildElement(int food, int wood, int gold, int stone)
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
                if (obj is BuildElement other)
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

            public static bool operator ==(BuildElement a, BuildElement b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(BuildElement a, BuildElement b)
            {
                return !a.Equals(b);
            }
        }

        public readonly Civilization Civilization;
        public readonly Unit Unit;
        public readonly List<BuildElement> Elements;
        public Cost Cost => GetCost();
        public double Score => GetScore();

        private readonly HashSet<Unit> SearchingUnits;
        private readonly HashSet<Technology> SearchingTechs;
        private readonly Dictionary<Unit, List<BuildElement>> KnownUnits;
        private readonly Dictionary<Technology, List<BuildElement>> KnownTechnologies;
        private readonly List<Unit> AvailableUnits;
        private readonly HashSet<Technology> UnitStateTechnologies;

        private readonly Random Random;

        internal BuildOrder(Civilization civilization, Unit unit, bool water = false, int seed = -1)
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
            KnownUnits = new Dictionary<Unit, List<BuildElement>>();
            KnownTechnologies = new Dictionary<Technology, List<BuildElement>>();
            AvailableUnits = Civilization.AvailableUnits.Where(u => u.Land).ToList();
            UnitStateTechnologies = new HashSet<Technology>();

            // get unit state techs
            foreach (var tech in civilization.Technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand || command is EnableDisableUnitCommand || command is UpgradeUnitCommand)
                    {
                        UnitStateTechnologies.Add(tech);
                    }
                }
            }

            Elements = GetUnit(unit).Where(e => e.Buildable).ToList();
            Clean();
        }

        public List<BuildElement> GetTechPartial(Technology tech)
        {
            return KnownTechnologies[tech];
        }

        public List<BuildElement> GetUnitPartial(Unit unit)
        {
            return KnownUnits[unit];
        }

        public void AddUpgrades()
        {
            // add upgrades
            foreach (var tech in UnitStateTechnologies)
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand ac)
                    {
                        if (ac.UnitId == Unit.Id || ac.ClassId == Unit.ClassId)
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

            while (found)
            {
                found = false;

                foreach (var tech in UnitStateTechnologies)
                {
                    foreach (var command in tech.Effect.Commands)
                    {
                        if (command is UpgradeUnitCommand uc)
                        {
                            if (uc.FromUnitId == current.Id)
                            {
                                current = AvailableUnits.Single(u => u.Id == uc.ToUnitId);
                                var bo = GetTech(tech);
                                Elements.AddRange(bo.Where(e => e.Buildable));

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
            var vills = Civilization.Units.Where(u => u.ClassId == 4).ToList();

            foreach (var tech in UnitStateTechnologies)
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand ac)
                    {
                        var unit = vills.FirstOrDefault(u => u.Id == ac.UnitId);
                        if (unit != null || ac.ClassId == 4)
                        {
                            if (ac.Attribute == Attribute.WorkRate || ac.Attribute == Attribute.TrainTime 
                                || ac.Attribute == Attribute.CarryCapacity)
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

            var sorts = new List<KeyValuePair<BuildElement, double>>();
            foreach (var be in Elements)
            {
                sorts.Add(new KeyValuePair<BuildElement, double>(be, GetPriority(be)));
            }

            var set = new HashSet<BuildElement>();
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

        public void Sort(Func<BuildElement, bool> predicate)
        {
            var set = new HashSet<BuildElement>();
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
                    var gatherers = new BuildElement(100 * cost.Food / sum, 100 * cost.Wood / sum, 100 * cost.Gold / sum, 100 * cost.Stone / sum);
                    Elements.Insert(start, gatherers);

                    cost = new Cost();

                    start = i + 2;
                    i = start - 1;
                }
            }

            var ucost = Unit.GetCost(Civilization);
            var s = ucost.Total;
            var gath = new BuildElement(100 * ucost.Food / s, 100 * ucost.Wood / s, 100 * ucost.Gold / s, 100 * ucost.Stone / s);

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

            foreach (var e in Elements)
            {
                if (e.Gatherers)
                {
                    lines.Add($"gl-bo-{lines.Count + 1} = {e.FoodGatherers + 13000}");
                    lines.Add($"gl-bo-{lines.Count + 1} = {e.WoodGatherers + 12000}");
                    lines.Add($"gl-bo-{lines.Count + 1} = {e.GoldGatherers + 11000}");
                    lines.Add($"gl-bo-{lines.Count + 1} = {e.StoneGatherers + 10000}");
                }
                else if (e.Research)
                {
                    lines.Add($"gl-bo-{lines.Count + 1} = {e.Technology.Id}");
                }
                else
                {
                    lines.Add($"gl-bo-{lines.Count + 1} = {-e.Unit.BaseUnit.Id}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"sn-primary-unit = {Unit.BaseUnit.Id}");
            sb.AppendLine($"gl-bo-count = " + lines.Count);
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private List<BuildElement> GetUnit(Unit unit)
        {
            const int TRACK = -1;

            if (SearchingUnits.Contains(unit))
            {
                return null;
            }

            if (Civilization.ExtraUnits.Contains(unit))
            {
                return new List<BuildElement>();
            }

            if (unit.Id == 109)
            {
                return new List<BuildElement>();
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

            var bo = new List<BuildElement>();

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
                foreach (var tech in UnitStateTechnologies)
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
                                    bo.Add(new BuildElement(false, unit, null));
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
                                    bo.Add(new BuildElement(false, unit, null));

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

            bo.Add(new BuildElement(false, unit, null));

            SearchingUnits.Remove(unit);
            KnownUnits.Add(unit, bo);

            return bo;
        }

        private List<BuildElement> GetTech(Technology tech)
        {
            const int TRACK = -1;

            if (SearchingTechs.Contains(tech))
            {
                return null;
            }

            if (tech == Civilization.Age1Tech)
            {
                return new List<BuildElement>();
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

            var bo = new List<BuildElement>();

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

                List<BuildElement> b = null;
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

            bo.Add(new BuildElement(true, null, tech));

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

            const int RES = 100;
            var food = (RES  * cost.Food) / cost.Total;
            var wood = (RES * cost.Wood) / cost.Total;
            var gold = (RES * cost.Gold) / cost.Total;
            var stone = (RES * cost.Stone) / cost.Total;

            for (int i = 0; i < Elements.Count; i++)
            {
                var current = Elements[i];
                if (current.Gatherers == false && current.Research == false)
                {
                    if (current.Unit == Civilization.GetDropSite(Resource.Food) && food > 0)
                    {
                        score += Math.Max(0, food * i);
                        food = 0;
                    }
                    if (current.Unit == Civilization.GetDropSite(Resource.Wood) && wood > 0)
                    {
                        score += Math.Max(0, wood * i);
                        wood = 0;
                    }
                    if (current.Unit == Civilization.GetDropSite(Resource.Gold) && gold > 0)
                    {
                        score += Math.Max(0, gold * i);
                        gold = 0;
                    }
                    if (current.Unit == Civilization.GetDropSite(Resource.Stone) && stone > 0)
                    {
                        score += Math.Max(0, stone * i);
                        stone = 0;
                    }
                }
            }

            const int AGE = 200;
            var age1 = 1 * AGE;
            var age2 = 1 * AGE;
            var age3 = 1 * AGE;
            var age4 = 1 * AGE;

            for (int i = 0; i < Elements.Count; i++)
            {
                var current = Elements[i];
                if (current.Gatherers == false && current.Research == true)
                {
                    if (current.Technology == Civilization.Age1Tech)
                    {
                        score += Math.Max(0, age1 * i);
                    }
                    if (current.Technology == Civilization.Age2Tech)
                    {
                        score += Math.Max(0, age2 * i);
                    }
                    if (current.Technology == Civilization.Age3Tech)
                    {
                        score += Math.Max(0, age3 * i);
                    }
                    if (current.Technology == Civilization.Age4Tech)
                    {
                        score += Math.Max(0, age4 * i);
                    }
                }
            }

            return 1 / score;
        }

        private int GetPriority(BuildElement be)
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

            if (be.Research == false && be.Unit == Unit.BuildLocation)
            {
                prior = 55;
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
