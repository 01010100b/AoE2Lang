using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class BuildOrderGenerator
    {
        public abstract class BuildOrderElement
        {
            public class GatherersBuildElement : BuildOrderElement
            {

            }

            public class ResearchBuildElement : BuildOrderElement
            {

            }

            public class BuildBuildElement : BuildOrderElement
            {

            }
        }

        public readonly Civilization Civilization;

        private readonly Dictionary<Unit, UnitStats> PossibleUnits = new Dictionary<Unit, UnitStats>();
        private readonly Dictionary<Technology, TechnologyStats> PossibleTechnologies = new Dictionary<Technology, TechnologyStats>();

        private readonly Dictionary<Technology, List<BuildOrderElement>> SolvedTechnologies = new Dictionary<Technology, List<BuildOrderElement>>();
        private readonly Dictionary<Unit, List<BuildOrderElement>> SolvedUnits = new Dictionary<Unit, List<BuildOrderElement>>();
        private readonly HashSet<Technology> SearchedTechnologies = new HashSet<Technology>();
        private readonly HashSet<Unit> SearchedUnits = new HashSet<Unit>();

        public BuildOrderGenerator(Civilization civilization)
        {
            Civilization = civilization;
        }

        public List<BuildOrderElement> GetBuildOrder(Unit primary, Unit secondary, Unit siege, int attempts)
        {
            throw new NotImplementedException();
        }

        private List<BuildOrderElement> GetUnit(Unit unit)
        {
            if (SearchedUnits.Contains(unit))
            {
                return null;
            }

            if (SolvedUnits.ContainsKey(unit))
            {
                return SolvedUnits[unit];
            }

            if (!PossibleUnits.ContainsKey(unit))
            {
                return null;
            }

            throw new NotImplementedException();
        }

        private List<BuildOrderElement> GetTechnology(Technology technology)
        {
            if (SearchedTechnologies.Contains(technology))
            {
                return null;
            }

            if (SolvedTechnologies.ContainsKey(technology))
            {
                return SolvedTechnologies[technology];
            }

            if (!PossibleTechnologies.ContainsKey(technology))
            {
                return null;
            }

            throw new NotImplementedException();
        }
    }
}
