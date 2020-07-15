using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Compiler.Mods.OldBuildOrder;

namespace Compiler.Mods
{
    class Civilization
    {
        public readonly int Id;
        public readonly string Name;
        public readonly List<Technology> Technologies = new List<Technology>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly Dictionary<Resource, float> Resources = new Dictionary<Resource, float>();

        public Technology Age1Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age1Tech]);
        public Technology Age2Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age2Tech]);
        public Technology Age3Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age3Tech]);
        public Technology Age4Tech => Technologies.Single(t => t.Id == (int)Resources[Resource.Age4Tech]);
        public List<Unit> AvailableUnits => Units.Where(u => u.Available || u.TechRequired).ToList();
        public List<Unit> TrainableUnits => AvailableUnits.Where(u => u.BuildLocation != null && new OldBuildOrder(this, u).Elements != null).ToList();

        public Civilization(int id, YTY.AocDatLib.Civilization civilization, Mod mod)
        {
            Id = id;
            Name = new string(Encoding.ASCII.GetChars(civilization.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
            var effect = mod.Effects[civilization.TechTreeId];

            // techs
            var set = new HashSet<int>();
            foreach (var tech in mod.Technologies.Where(t => t.CivId == id || t.CivId == -1))
            {
                set.Add(tech.Id);
            }

            foreach (var command in effect.Commands.Where(c => c is DisableTechCommand).Cast<DisableTechCommand>())
            {
                set.Remove(command.TechId);
            }

            foreach (var tech in mod.Technologies.Where(t => t.Effect != null && (t.CivId == id || t.CivId == -1)))
            {
                foreach (var command in tech.Effect.Commands)
                {
                    if (command is DisableTechCommand dc)
                    {
                        set.Remove(dc.TechId);
                    }
                }
            }

            foreach (var techid in set)
            {
                Technologies.Add(mod.Technologies[techid]);
            }

            // units
            set.Clear();

            foreach (var unit in mod.Units)
            {
                if (unit.TechRequired == false)
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

            foreach (var resource in Enum.GetValues(typeof(Resource)).Cast<Resource>().Where(r => r != Resource.None))
            {
                Resources.Add(resource, civilization.Resources[(int)resource]);
            }
        }

        public OldBuildOrder GetBuildOrder(Unit unit, int iterations = 1000)
        {
            var bo = new OldBuildOrder(this, unit);
            if (bo.Elements == null)
            {
                return null;
            }

            bo.AddUpgrades();
            bo.AddEcoUpgrades();
            bo.Sort();

            var rng = new Random();
            Parallel.For(0, iterations, i =>
            {
                int seed = -1;
                lock (rng)
                {
                    seed = Math.Abs(rng.Next() ^ rng.Next() ^ rng.Next());
                }

                var nbo = new OldBuildOrder(this, unit, false, seed);
                nbo.AddUpgrades();
                nbo.AddEcoUpgrades();
                nbo.Sort();
                lock (rng)
                {
                    if (nbo.Score > bo.Score)
                    {
                        bo = nbo;
                    }
                }
            });

            bo.Sort();
            bo.Sort();

            bo.InsertGatherers();

            return bo;
        }

        public override string ToString()
        {
            return $"{Name} with {Units.Count} units ({TrainableUnits.Count} buildable) and {Technologies.Count} techs";
        }

        public Unit GetDropSite(Resource resource)
        {
            foreach (var u in Units)
            {
                if (u.ResourceGathered == resource && u.DropSite != null)
                {
                    return u.DropSite;
                }
            }

            return null;
        }
    }
}
