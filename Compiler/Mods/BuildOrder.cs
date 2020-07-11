using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class BuildOrder
    {
        public class BuildElement
        {
            public readonly bool Research;
            public readonly Unit Unit;
            public readonly Technology Technology;

            public readonly bool Gatherers;
            public readonly int Food;
            public readonly int Wood;
            public readonly int Gold;
            public readonly int Stone;

            public BuildElement(bool research, Unit unit, Technology technology)
            {
                Research = research;
                Unit = unit;
                Technology = technology;
                Gatherers = false;

                if (research)
                {
                    Food = technology.FoodCost;
                    Wood = technology.WoodCost;
                    Gold = technology.GoldCost;
                    Stone = technology.StoneCost;
                }
                else
                {
                    Food = unit.FoodCost;
                    Wood = unit.WoodCost;
                    Gold = unit.GoldCost;
                    Stone = unit.StoneCost;
                }
            }

            public BuildElement(int food, int wood, int gold, int stone)
            {
                Gatherers = true;
                Food = food;
                Wood = wood;
                Gold = gold;
                Stone = stone;
            }

            public override bool Equals(object obj)
            {
                if (obj is BuildElement other)
                {
                    return (Research == other.Research) && (Unit == other.Unit) && (Technology == other.Technology);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                var hash = Research.GetHashCode();
                if (Unit != null)
                {
                    hash ^= Unit.GetHashCode();
                }
                if (Technology != null)
                {
                    hash ^= Technology.GetHashCode();
                }

                return hash;
            }

            public override string ToString()
            {
                if (Gatherers)
                {
                    return $"Set gatherers {Food} {Wood} {Gold} {Stone}";
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
        }

        public readonly Civilization Civilization;
        public readonly Unit Unit;
        public readonly List<BuildElement> Elements;

        public int FoodCost => Elements.Where(e => !e.Gatherers).Sum(e => e.Food);
        public int WoodCost => Elements.Where(e => !e.Gatherers).Sum(e => e.Wood);
        public int GoldCost => Elements.Where(e => !e.Gatherers).Sum(e => e.Gold);
        public int StoneCost => Elements.Where(e => !e.Gatherers).Sum(e => e.Stone);
        public int TotalCost => FoodCost + WoodCost + GoldCost + StoneCost;

        private readonly HashSet<Unit> SearchingUnits;
        private readonly HashSet<Technology> SearchingTechs;
        private readonly Dictionary<Unit, List<BuildElement>> KnownUnits;
        private readonly Dictionary<Technology, List<BuildElement>> KnownTechnologies;

        private readonly Random Random;

        public BuildOrder(Civilization civilization, Unit unit, int seed = -1)
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

            Elements = GetUnit(unit).Where(e => !(e.Research && e.Technology.ResearchLocation == null)).ToList();

            foreach (var tech in civilization.Technologies.Where(t => t.Effect != null))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is AttributeModifierCommand ac)
                    {
                        if (ac.UnitId == unit.Id || ac.ClassId == unit.ClassId)
                        {
                            var bo = GetTech(tech);
                            if (bo != null)
                            {
                                Elements.AddRange(bo.Where(e => !(e.Research && e.Technology.ResearchLocation == null)).ToList());
                            }
                        }
                    }
                }
            }

            Elements = Elements.Distinct().ToList();

            InsertGatherers();
        }

        public List<BuildElement> GetTechPartial(Technology tech)
        {
            return KnownTechnologies[tech];
        }

        public List<BuildElement> GetUnitPartial(Unit unit)
        {
            return KnownUnits[unit];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var be in Elements)
            {
                sb.AppendLine(be.ToString());
            }

            sb.AppendLine($"Total build order cost: {FoodCost} {WoodCost} {GoldCost} {StoneCost}");
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
                                    var from = Civilization.Units.Single(u => u.Id == uc.FromUnitId);
                                    var c = GetUnit(from);
                                    if (c != null)
                                    {
                                        bo.AddRange(c);
                                        bo.AddRange(b);
                                        SearchingUnits.Remove(unit);
                                        KnownUnits.Add(unit, bo);

                                        return bo;
                                    }
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
                foreach (var unit in Civilization.Units.Where(u => u.TechInitiated == tech))
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

        private void InsertGatherers()
        {
            var food = 0;
            var wood = 0;
            var gold = 0;
            var stone = 0;

            var start = 0;
            for (int i = 0; i < Elements.Count; i++)
            {
                var current = Elements[i];
                if (!current.Gatherers)
                {
                    var mul = 1;

                    if (current.Research == false && current.Unit == Unit)
                    {
                        mul = 20;
                    }

                    if (current.Research == false && current.Unit == Unit.BuildLocation)
                    {
                        mul = 3;
                    }

                    food += current.Food * mul;
                    wood += current.Wood * mul;
                    gold += current.Gold * mul;
                    stone += current.Stone * mul;
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
                    var sum = food + wood + gold + stone;
                    var gatherers = new BuildElement(100 * food / sum, 100 * wood / sum, 100 * gold / sum, 100 * stone / sum);
                    Elements.Insert(start, gatherers);

                    food = 0;
                    wood = 0;
                    gold = 0;
                    stone = 0;
                    
                    start = i + 2;
                    i = start - 1;
                }
            }

            var s = Unit.FoodCost + Unit.WoodCost + Unit.GoldCost + Unit.StoneCost;
            var gath = new BuildElement(100 * Unit.FoodCost / s, 100 * Unit.WoodCost / s, 100 * Unit.GoldCost / s, 100 * Unit.StoneCost / s);

            Elements.Add(gath);
        }
    }
}
