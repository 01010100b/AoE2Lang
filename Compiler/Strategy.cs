using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Compiler.BuildOrderGenerator;
using static Compiler.BuildOrderGenerator.BuildOrderElement;
using static Compiler.CounterGenerator;

namespace Compiler
{
    class Strategy
    {
        public readonly Mod Mod;
        public readonly Civilization Civilization;
        public readonly Dictionary<Unit, BuildOrder> BuildOrders = new Dictionary<Unit, BuildOrder>();
        public readonly List<Counter> Counters = new List<Counter>();

        public Strategy(Mod mod, Civilization civilization)
        {
            Mod = mod;
            Civilization = civilization;
        }

        public void Generate()
        {
            AddBuildOrders();
            AddCounters();
        }

        private void AddBuildOrders()
        {
            var units = Civilization.Units
                .Where(u => u.Land)
                .Select(u => u.BaseUnit)
                .Where(u => u.Available || u.TechRequired)
                .Where(u => u.TrainLocation != null)
                .Where(u => u.Military)
                .Distinct()
                .ToList();

            var best_siege = units.First();
            var best_siege_score = double.MinValue;
            foreach (var siege in units.Where(u => u.Class == UnitClass.SiegeWeapon))
            {
                var score = (double)siege.BaseAttacks.Where(a => a.Id == 11 || a.Id == 21 || a.Id == 26).Sum(a => a.Amount);
                score /= Math.Max(1, siege.BaseReloadTime);
                score /= Math.Max(1, siege.BaseCost.Total);

                //Log.Debug($"siege {siege.Id} {siege.Name} score {score}");

                if (score > best_siege_score)
                {
                    best_siege = siege;
                    best_siege_score = score;
                }
            }

            var bogen = new BuildOrderGenerator(Civilization);

            foreach (var unit in units)
            {
                var current = unit;

                if (current.GetAge(Civilization) <= 1)
                {
                    var bestage = int.MaxValue;
                    foreach (var upgr in unit.UpgradesTo)
                    {
                        var age = upgr.GetAge(Civilization);
                        if (age > 1 && age < bestage)
                        {
                            current = upgr;
                            bestage = age;
                        }
                    }
                }

                if (current.GetAge(Civilization) <= 1)
                {
                    continue;
                }

                var good = false;
                switch (current.Class)
                {
                    case UnitClass.Archer:
                    case UnitClass.Ballista:
                    case UnitClass.Cavalry:
                    case UnitClass.CavalryArcher:
                    case UnitClass.CavalryRaider:
                    case UnitClass.Conquistador:
                    case UnitClass.ElephantArcher:
                    case UnitClass.HandCannoneer:
                    case UnitClass.Infantry:
                    case UnitClass.Monk:
                    case UnitClass.Phalanx:
                    case UnitClass.Pikeman:
                    case UnitClass.Raider:
                    case UnitClass.Scout:
                    //case UnitClass.SiegeWeapon:
                    case UnitClass.Spearman:
                    case UnitClass.TwoHandedSwordsMan:
                    //case UnitClass.PackedUnit:
                    //case UnitClass.UnpackedSiegeUnit:
                    case UnitClass.WarElephant: good = true; break;
                }

                if (!good)
                {
                    continue;
                }

                var bo = bogen.GetBuildOrder(current, null, best_siege, true, true, 100);

                if (bo != null && bo.Elements.Count <= 100)
                {
                    BuildOrders.Add(unit, bo);
                }
                else
                {
                    //Log.Debug($"no bo for {unit.Id} {unit.Name}");
                }
            }
        }

        private void AddCounters()
        {
            var enemies = Mod.Civilizations.SelectMany(c => c.TrainableUnits).Where(u => u.Land && u.Class != UnitClass.Civilian).Distinct().ToList();

            var counters = new HashSet<Unit>();
            // get counters
            foreach (var counter in BuildOrders.Keys)
            {
                counters.Add(counter);
                foreach (var upgr in counter.UpgradedFrom.Concat(counter.UpgradesTo))
                {
                    if (upgr.GetAge(Civilization) >= 2)
                    {
                        counters.Add(upgr);
                    }
                }
            }

            var cgen = new CounterGenerator(Civilization);
            var cs = cgen.GetCounters(enemies, counters.ToList());

            var baseunits = new HashSet<Unit>();
            foreach (var unit in cs.Select(c => c.EnemyUnit))
            {
                baseunits.Add(unit.BaseUnit);
            }

            foreach (var counter in cs.Where(c => baseunits.Contains(c.EnemyUnit)))
            {
                if (BuildOrders.ContainsKey(counter.CounterUnit))
                {
                    Counters.Add(counter);
                }
                else
                {
                    foreach (var upgr in counter.CounterUnit.UpgradedFrom)
                    {
                        if (BuildOrders.ContainsKey(upgr))
                        {
                            Counters.Add(new Counter(counter.EnemyUnit, counter.Age, upgr));
                            break;
                        }
                    }
                }
            }

            //Log.Debug("found " + Counters.Count + " counters");
        }

        public string Compile()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"; ------ STARTING CIV {Civilization.Id} {Civilization.Name} ------");
            sb.AppendLine(";region " + Civilization.Name);

            sb.AppendLine("#if civ-selected " + Civilization.Id);

            sb.AppendLine("; Builders");
            var buildings = Civilization.BuildableUnits.Where(u => u.Land && u.Type == 80).Distinct().ToList();
            buildings.Sort((a, b) => b.TrainTime.CompareTo(a.TrainTime));
            buildings = buildings.Take(14).ToList();

            sb.AppendLine("(defrule");
            sb.AppendLine("\t(true)");
            sb.AppendLine("=>");
            foreach (var building in buildings.Where(b => b.TrainTime > TimeSpan.FromSeconds(60)))
            {
                var vills = 2;
                if (building.TrainTime > TimeSpan.FromSeconds(120))
                {
                    vills = 4;
                }
                if (building.TrainTime > TimeSpan.FromSeconds(180))
                {
                    vills = 6;
                }
                if (building.TrainTime > TimeSpan.FromSeconds(1000))
                {
                    vills = 20;
                }

                sb.AppendLine($"\t(up-assign-builders c: {building.Id} c: {vills})");

                Log.Debug($"Builders for {building.Id} {building.Name} = {vills}");
            }
            sb.AppendLine(")");
            sb.AppendLine("");

            // write counters
            sb.AppendLine(";region ---- Counters");
            foreach (var counter in Counters)
            {
                var age = Civilization.Age1Tech.Id;
                if (counter.Age == 2)
                {
                    age = Civilization.Age2Tech.Id;
                }
                if (counter.Age == 3)
                {
                    age = Civilization.Age3Tech.Id;
                }
                if (counter.Age == 4)
                {
                    age = Civilization.Age4Tech.Id;
                }

                sb.AppendLine($";counter {counter.EnemyUnit.Id} {counter.EnemyUnit.Name} with {counter.CounterUnit.Id} {counter.CounterUnit.Name} in age {age}");
                sb.AppendLine($"(defrule");
                sb.AppendLine($"\t(civ-selected {Civilization.Id})");
                sb.AppendLine($"\t(strategic-number sn-target-unit == {counter.EnemyUnit.Id})");
                sb.AppendLine($"\t(current-age == {age})");
                sb.AppendLine($"=>");
                sb.AppendLine($"\t(set-strategic-number sn-strategy {counter.CounterUnit.Id})");
                sb.AppendLine($")");
                sb.AppendLine("");
            }

            sb.AppendLine(";endregion");

            // write build orders
            sb.AppendLine(";region ---- Build Orders");
            foreach (var unit in BuildOrders.Keys)
            {
                sb.AppendLine($"; -- Build order for {unit.Id} {unit.Name}");

                sb.AppendLine("#if sn-strategy == " + unit.Id);
                sb.AppendLine($"sn-primary-unit = {unit.BaseUnit.Id}");

                var bo = BuildOrders[unit];

                if (bo.Secondary != null)
                {
                    sb.AppendLine($"sn-secondary-unit = {bo.Secondary.BaseUnit.Id}");
                }
                else
                {
                    sb.AppendLine($"sn-secondary-unit = OFF");
                }

                if (bo.Siege != null)
                {
                    sb.AppendLine($"sn-siege-unit = {bo.Siege.BaseUnit.Id}");
                }
                else
                {
                    sb.AppendLine($"sn-siege-unit = OFF");
                }

                var index = 1;
                foreach (var e in bo.Elements)
                {
                    if (e is GatherersBuildElement ge)
                    {
                        sb.AppendLine($"; set food % to {ge.Food}");
                        sb.AppendLine($"gl-bo-{index} = {ge.Food + 13000}");
                        index++;

                        sb.AppendLine($"; set wood % to {ge.Wood}");
                        sb.AppendLine($"gl-bo-{index} = {ge.Wood + 12000}");
                        index++;

                        sb.AppendLine($"; set gold % to {ge.Gold}");
                        sb.AppendLine($"gl-bo-{index} = {ge.Gold + 11000}");
                        index++;

                        sb.AppendLine($"; set stone % to {ge.Stone}");
                        sb.AppendLine($"gl-bo-{index} = {ge.Stone + 10000}");
                        index++;
                    }
                    else if (e is ResearchBuildElement re)
                    {
                        sb.AppendLine($"; research {re.Technology.Id} {re.Technology.Name}");
                        sb.AppendLine($"gl-bo-{index} = {re.Technology.Id}");
                        index++;
                    }
                    else if (e is BuildBuildElement be)
                    {
                        sb.AppendLine($"; build/train {be.Unit.Id} {be.Unit.Name}");
                        sb.AppendLine($"gl-bo-{index} = {-be.Unit.BaseUnit.Id}");
                        index++;
                    }
                }

                sb.AppendLine($"gl-bo-count = {index - 1}");

                sb.AppendLine("#end-if");
            }
            sb.AppendLine(";endregion");

            // set initial bo
            sb.AppendLine("; choose initial bo");
            foreach (var unit in BuildOrders.Keys.Where(u => u.GetAge(Civilization) == 2))
            {
                sb.AppendLine($"#if sn-strategy == OFF");
                sb.AppendLine($"    generate-random-number 100");
                sb.AppendLine("     #if random-number < 30");
                sb.AppendLine($"            sn-strategy = {unit.Id}");
                sb.AppendLine("     #end-if");
                sb.AppendLine("#end-if");
            }

            sb.AppendLine("#if sn-strategy == OFF");
            sb.AppendLine(" sn-strategy = 74");
            sb.AppendLine("#end-if");

            sb.AppendLine("#end-if");
            sb.AppendLine(";endregion");

            return sb.ToString();
        }
    }
}
