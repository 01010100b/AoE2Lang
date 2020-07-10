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
        public readonly List<Civilization> Civilizations = new List<Civilization>();

        public void Load(string file)
        {
            Effects.Clear();
            Technologies.Clear();
            Civilizations.Clear();

            var dat = new DatFile(file);

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

            for (int id = 0; id < dat.Researches.Count; id++)
            {
                var tech = new Technology(id, dat.Researches[id], Effects);
                Technologies.Add(tech);
            }

            for (int id = 0; id < dat.Civilizations.Count; id++)
            {
                var civ = new Civilization(id, dat.Civilizations[id], Technologies, Effects);
                Civilizations.Add(civ);
            }
        }
    }
}
