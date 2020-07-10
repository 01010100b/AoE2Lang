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
            var units = new Dictionary<int, Unit>();
            foreach (var unit in dat.Civilizations.SelectMany(c => c.Units))
            {
                units[unit.Id] = new Unit(unit, Technologies);
            }

            Units.AddRange(units.Values);

            // get civs & units

            for (int id = 0; id < dat.Civilizations.Count; id++)
            {
                var civ = new Civilization(id, dat.Civilizations[id], this);
                Civilizations.Add(civ);
            }

            

            // assign units to techs
            foreach (var tech in Technologies)
            {
                var dattech = dat.Researches[tech.Id];
                if (dattech.ResearchLocation > 0)
                {
                    tech.ResearchLocation = units[dattech.ResearchLocation];
                }
            }
        }
    }
}
