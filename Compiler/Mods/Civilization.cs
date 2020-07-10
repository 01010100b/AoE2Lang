using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Compiler.Mods.BuildOrder;

namespace Compiler.Mods
{
    class Civilization
    {
        public readonly int Id;
        public readonly string Name;
        public readonly List<Technology> Technologies = new List<Technology>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Unit> ExtraUnits = new List<Unit>();
        public readonly Dictionary<Resource, float> Resources = new Dictionary<Resource, float>();
        public Technology Age1Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age1Tech]);
        public Technology Age2Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age2Tech]);
        public Technology Age3Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age3Tech]);
        public Technology Age4Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age4Tech]);

        public Civilization(int id, YTY.AocDatLib.Civilization civilization, Mod mod)
        {
            Id = id;
            Name = new string(Encoding.ASCII.GetChars(civilization.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
            var effect = mod.Effects[civilization.TechTreeId];

            var set = new HashSet<int>();
            foreach (var tech in mod.Technologies.Where(t => t.CivId == id || t.CivId == -1))
            {
                set.Add(tech.Id);
            }

            foreach (var command in effect.Commands.Where(c => c is DisableTechCommand).Cast<DisableTechCommand>())
            {
                set.Remove(command.TechId);
            }

            foreach (var techid in set)
            {
                Technologies.Add(mod.Technologies[techid]);
            }

            set.Clear();

            foreach (var unit in mod.Units)
            {
                if (unit.Available)
                {
                    set.Add(unit.Id);
                }
            }

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

            var civ_ids = new HashSet<int>();
            foreach (var unit in civilization.Units)
            {
                civ_ids.Add(unit.Id);
            }

            set.RemoveWhere(i => !civ_ids.Contains(i));

            foreach (var unit in mod.Units)
            {
                if (set.Contains(unit.Id))
                {
                    Units.Add(unit);
                }
            }

            // fix tc not available
            foreach (var unit in mod.Units)
            {
                if (!set.Contains(unit.Id))
                {
                    var name = unit.Name;
                    foreach (var other in Units)
                    {
                        if (other.Name.StartsWith(name))
                        {
                            Units.Add(unit);
                            ExtraUnits.Add(unit);
                            break;
                        }
                    }
                }
            }

            // set unit build locations
            foreach (var unit in Units.Where(u => u.Type != 80))
            {
                var datunit = civilization.Units.Single(u => u.Id == unit.Id);
                var loc = datunit.TrainLocationId;

                if (loc > 0)
                {
                    unit.BuildLocation = Units.FirstOrDefault(u => u.Id == loc);
                }
            }

            foreach (var resource in Enum.GetValues(typeof(Resource)).Cast<Resource>())
            {
                Resources.Add(resource, civilization.Resources[(int)resource]);
            }
        }

        
    }
}
