using Compiler.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class CounterGenerator
    {
        public struct Counter
        {
            public readonly Unit EnemyUnit;
            public readonly int Age;
            public readonly Unit CounterUnit;

            public Counter(Unit enemy, int age, Unit counter)
            {
                EnemyUnit = enemy;
                Age = age;
                CounterUnit = counter;
            }
        }

        public readonly Civilization Civilization;

        public CounterGenerator(Civilization civilization)
        {
            Civilization = civilization;
        }

        public List<Counter> GetCounters(List<Unit> enemies, List<Unit> counters)
        {
            var results = new List<Counter>();

            var counters_by_age = new Dictionary<int, List<Unit>>();
            for (int i = 1; i <= 4; i++)
            {
                if (!counters_by_age.ContainsKey(i))
                {
                    counters_by_age.Add(i, new List<Unit>());
                }
            }

            foreach (var counter in counters)
            {
                counters_by_age[counter.GetAge(Civilization)].Add(counter);
            }

            Parallel.ForEach(enemies, enemy =>
            {
                for (int age = 1; age <= 4; age++)
                {
                    var best_score = double.NegativeInfinity;
                    Counter current = new Counter();

                    foreach (var counter in counters_by_age[age])
                    {
                        var score = GetScore(enemy, counter);
                        if (score > best_score)
                        {
                            current = new Counter(enemy, age, counter);
                            best_score = score;
                        }
                    }

                    if (current.CounterUnit != null)
                    {
                        lock (results)
                        {
                            results.Add(current);
                        }
                    }
                }
            });

            return results;
        }

        private double GetScore(Unit enemy, Unit counter)
        {
            var enemystats = new UnitStats(enemy, null);
            var counterstats = new UnitStats(counter, null);
            
            var attackenemy = 0d;
            foreach (var attack in enemystats.Attacks)
            {
                foreach (var armor in counterstats.Armors)
                {
                    if (attack.Id == armor.Id)
                    {
                        attackenemy += Math.Max(0, Math.Min(1, attack.Amount - armor.Amount));
                    }
                }
            }
            
            var attackcounter = 0d;
            foreach (var attack in counterstats.Attacks)
            {
                foreach (var armor in enemystats.Armors)
                {
                    if (attack.Id == armor.Id)
                    {
                        attackcounter += Math.Max(0, Math.Min(1, attack.Amount - armor.Amount));
                    }
                }
            }

            var cost = counterstats.Cost.Total;
            if (counter.GetAge(Civilization) == 4)
            {
                cost += counterstats.Cost.Gold;
            }

            var enemyscore = Math.Ceiling(counterstats.Hitpoints / attackenemy) * enemystats.ReloadTime * enemystats.Cost.Total;
            var counterscore = Math.Ceiling(enemystats.Hitpoints / attackcounter) * counterstats.ReloadTime * cost;

            enemyscore = 1d / Math.Max(1, enemyscore);
            counterscore = 1d / Math.Max(1, counterscore);

            return counterscore - enemyscore;
        }
    }
}
