using System;
using System.Collections.Generic;
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

            public BuildElement(bool research, Unit unit, Technology technology)
            {
                Research = research;
                Unit = unit;
                Technology = technology;
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
        }

        public readonly Civilization Civilization;
        public readonly Unit Unit;
        public readonly List<BuildElement> Elements;

        private readonly HashSet<Unit> SearchingUnits;
        private readonly HashSet<Technology> SearchingTechs;
        private readonly Dictionary<Unit, List<BuildElement>> KnownUnits;
        private readonly Dictionary<Technology, List<BuildElement>> KnownTechnologies;

        public BuildOrder(Civilization civilization, Unit unit)
        {
            Civilization = civilization;
            Unit = unit;

            SearchingUnits = new HashSet<Unit>();
            SearchingTechs = new HashSet<Technology>();
            KnownUnits = new Dictionary<Unit, List<BuildElement>>();
            KnownTechnologies = new Dictionary<Technology, List<BuildElement>>();

            Elements = GetUnit(unit);
            Clean();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var be in Elements)
            {
                if (be.Research)
                {
                    sb.AppendLine("Research " + be.Technology.Id + " " + be.Technology.Name);
                }
                else
                {
                    if (be.Unit.Type == 80)
                    {
                        sb.AppendLine("Build " + be.Unit.Id + " " + be.Unit.Name);
                    }
                    else
                    {
                        sb.AppendLine("Train " + be.Unit.Id + " " + be.Unit.Name);
                    }
                }
            }

            return sb.ToString();
        }

        private List<BuildElement> GetUnit(Unit unit)
        {
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

            var bo = new List<BuildElement>();

            // train site
            if (unit.BuildLocation != null)
            {
                var b = GetUnit(unit.BuildLocation);
                if (b == null)
                {
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
            if (SearchingTechs.Contains(tech))
            {
                return null;
            }

            if (KnownTechnologies.ContainsKey(tech))
            {
                return KnownTechnologies[tech];
            }

            SearchingTechs.Add(tech);

            var bo = new List<BuildElement>();
            
            var prereq_count = 0;
            foreach (var prereq in tech.Prerequisites)
            {
                var b = GetTech(prereq);
                if (b != null)
                {
                    bo.AddRange(b);
                    prereq_count++;
                }

                if (prereq_count >= tech.MinPrerequisites)
                {
                    break;
                }
            }

            if (prereq_count < tech.MinPrerequisites)
            {
                SearchingTechs.Remove(tech);
                return null;
            }

            foreach (var unit in Civilization.Units.Where(u => u.TechInitiated == tech))
            {
                var b = GetUnit(unit);
                if (b != null)
                {
                    bo.AddRange(b);
                    break;
                }
            }

            bo.Add(new BuildElement(true, null, tech));

            SearchingTechs.Remove(tech);
            KnownTechnologies.Add(tech, bo);

            return bo;
        }

        private void Clean()
        {
            var bo = new List<BuildElement>();
            foreach (var e in Elements)
            {
                if (!(e.Research && e.Technology.ResearchLocation == null))
                {
                    bo.Add(e);
                }
            }

            Elements.Clear();

            foreach (var e in bo)
            {
                if (!Elements.Contains(e))
                {
                    Elements.Add(e);
                }
            }
        }
    }
}
