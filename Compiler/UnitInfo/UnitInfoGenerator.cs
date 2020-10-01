using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Compiler.Mods.UnitStats;

namespace Compiler.UnitInfo
{
    class UnitInfoGenerator
    {
        private static string PrologueFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnitInfo", "Prologue.per");

        public string Generate(Mod mod)
        {
            var units = new Dictionary<int, Unit>();

            foreach (var unit in mod.AvailableUnits.Where(u => u.TrainLocation != null))
            {
                units[unit.Id] = unit;
            }

            var rules = new List<string>();

            var entry_count = units.Keys.Max() + 1;
            for (int i = 0; i < entry_count; i++)
            {
                var rule =
                    $"(defrule\n" +
                    $"\t(or\n" +
                    $"\t\t(unit-available {i})\n" +
                    $"\t\t(building-available {i})\n" +
                    $"\t)\n" +
                    $"=>\n" +
                    $"\t(set-goal gl-unitinfo-unit-available 1)\n" +
                    $")\n\n";

                rules.Add(rule);

                if (units.TryGetValue(i, out Unit unit))
                {
                    var armors = new List<ArmorValue>();
                    var attacks = new List<ArmorValue>();
                    armors.AddRange(unit.BaseArmors);
                    attacks.AddRange(unit.BaseAttacks);
                    armors.RemoveAll(av => av.Id == 3); // pierce
                    attacks.RemoveAll(av => av.Id == 3);
                    armors.RemoveAll(av => av.Id == 4); // melee
                    attacks.RemoveAll(av => av.Id == 4);

                    armors.Sort((a, b) => b.Amount.CompareTo(a.Amount));
                    attacks.Sort((a, b) => b.Amount.CompareTo(a.Amount));

                    while (armors.Count < 3)
                    {
                        armors.Add(new ArmorValue(-1, -1));
                    }

                    while (attacks.Count < 3)
                    {
                        attacks.Add(new ArmorValue(-1, -1));
                    }

                    rule =
                        $"(defrule\n" +
                        $"\t(up-compare-goal gl-unitinfo-unit-id c:== {i})\n" +
                        $"=>\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor1-id {armors[0].Id})\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor1-amount {armors[0].Amount})\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor2-id {armors[1].Id})\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor2-amount {armors[1].Amount})\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor3-id {armors[2].Id})\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor3-amount {armors[2].Amount})\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack1-id {attacks[0].Id})\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack1-amount {attacks[0].Amount})\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack2-id {attacks[1].Id})\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack2-amount {attacks[1].Amount})\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack3-id {attacks[2].Id})\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack3-amount {attacks[2].Amount})\n" +
                        $"\t(set-goal gl-unitinfo-unit-upgrade-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-upgrade-research -1)\n" +
                        $"\t(up-jump-direct g: gl-unitinfo-return-addr)\n" +
                        $")\n\n";

                    rules.Add(rule);
                }

                else
                {
                    rule =
                        $"(defrule\n" +
                        $"\t(up-compare-goal gl-unitinfo-unit-id c:== {i})\n" +
                        $"=>\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor1-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor1-amount -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor2-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor2-amount -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor3-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-armor3-amount -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack1-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack1-amount -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack2-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack2-amount -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack3-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-attack3-amount -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-upgrade-id -1)\n" +
                        $"\t(set-goal gl-unitinfo-unit-upgrade-research -1)\n" +
                        $"\t(up-jump-direct g: gl-unitinfo-return-addr)\n" +
                        $")\n\n";

                    rules.Add(rule);
                }
            }

            var lines = new List<string>();
            lines.AddRange(File.ReadAllLines(PrologueFile));

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                line = line.Replace("$TABLE_SIZE$", entry_count.ToString());
                line = line.Replace("$TABLE_RULES$", rules.Count.ToString());
                lines[i] = line;
            }

            lines.AddRange(rules);

            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            Log.Debug($"UnitInfo: {entry_count} entries");
            Log.Debug($"UnitInfo: {units.Count} units");

            return sb.ToString();
        }
    }
}
