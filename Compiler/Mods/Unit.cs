using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class Unit
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int Type;
        public Unit BuildLocation { get; internal set; } = null;
        public readonly Technology TechInitiated;
        public readonly bool TechRequired;
        public readonly bool Available;

        public Unit(YTY.AocDatLib.Unit unit, List<Technology> technologies)
        {
            Id = unit.Id;
            Name = new string(Encoding.ASCII.GetChars(unit.Name).Where(c => char.IsLetterOrDigit(c)).ToArray());
            Type = unit.Type;

            TechRequired = false;
            Available = false;
            if (unit.Enabled == 1)
            {
                Available = true;
            }

            foreach (var tech in technologies.Where(t => t.Effect != null))
            {
                if (TechRequired)
                {
                    break;
                }

                foreach (var command in tech.Effect.Commands)
                {
                    if (command is EnableDisableUnitCommand ec)
                    {
                        if (ec.Enable && ec.UnitId == Id)
                        {
                            TechRequired = true;
                            break;
                        }
                    }

                    if (command is UpgradeUnitCommand uc)
                    {
                        if (uc.ToUnitId == Id)
                        {
                            TechRequired = true;
                            break;
                        }
                    }
                }
            }

            if (unit.TechId > 0)
            {
                TechInitiated = technologies.Single(t => t.Id == unit.TechId);
            }
            else
            {
                TechInitiated = null;
            }
        }
    }
}
