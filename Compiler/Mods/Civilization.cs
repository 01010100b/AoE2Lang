using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class Civilization
    {
        public readonly int Id;
        public readonly string Name;
        public readonly List<Technology> Technologies = new List<Technology>();
        public readonly List<Unit> Units = new List<Unit>();

        public Civilization(int id, YTY.AocDatLib.Civilization civilization, List<Technology> technologies, List<Effect> effects)
        {
            Id = id;
            Name = new string(Encoding.ASCII.GetChars(civilization.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
            var effect = effects[civilization.TechTreeId];

            var set = new HashSet<int>();
            foreach (var tech in technologies.Where(t => t.CivId == id || t.CivId == -1))
            {
                set.Add(tech.Id);
            }

            foreach (var command in effect.Commands.Where(c => c is DisableTechCommand).Cast<DisableTechCommand>())
            {
                set.Remove(command.TechId);
            }

            foreach (var techid in set)
            {
                Technologies.Add(technologies[techid]);
            }

            set.Clear();
            foreach (var ec in Technologies.Where(t => t.Effect != null).Select(t => t.Effect).SelectMany(e => e.Commands))
            {
                if (ec is EnableDisableUnitCommand command)
                {
                    if (command.Enable)
                    {
                        set.Add(command.UnitId);
                    }
                }
                else if (ec is UpgradeUnitCommand uc)
                {
                    set.Add(uc.ToUnitId);
                }
            }

            foreach (var unit in civilization.Units)
            {
                if (set.Contains(unit.Id) || unit.Enabled == 1)
                {
                    Units.Add(new Unit(unit));
                }
            }
        }
    }
}
