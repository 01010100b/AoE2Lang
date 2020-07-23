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
        public static double GetScore(UnitStats counter, UnitStats enemy)
        {
            var attackenemy = 0d;
            foreach (var attack in enemy.Attacks)
            {
                foreach (var armor in counter.Armors)
                {
                    if (attack.Id == armor.Id)
                    {
                        attackenemy += Math.Max(0, attack.Amount - armor.Amount);
                    }
                }
            }
            attackenemy = Math.Max(1, attackenemy);

            var attackcounter = 0d;
            foreach (var attack in counter.Attacks)
            {
                foreach (var armor in enemy.Armors)
                {
                    if (attack.Id == armor.Id)
                    {
                        attackcounter += Math.Max(0, attack.Amount - armor.Amount);
                    }
                }
            }
            attackcounter = Math.Max(1, attackcounter);

            var enemyscore = Math.Ceiling(counter.Hitpoints / attackenemy) * enemy.ReloadTime;
            var counterscore = Math.Ceiling(enemy.Hitpoints / attackcounter) * counter.ReloadTime;

            enemyscore = 1d / Math.Max(1, enemyscore);
            counterscore = 1d / Math.Max(1, counterscore);

            var exp = enemy.Range > 2 ? 1.9 : 1.6;
            enemyscore *= Math.Pow(1000d / enemy.Cost.Total, exp);

            exp = counter.Range > 2 ? 1.9 : 1.6;
            counterscore *= Math.Pow(1000d / counter.Cost.Total, exp);

            return counterscore - enemyscore;
        }

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

        private readonly Dictionary<Unit, UnitStats> UnitStats = new Dictionary<Unit, UnitStats>();

        public CounterGenerator(Civilization civilization)
        {
            Civilization = civilization;
        }

        public List<Counter> GetCounters(List<Unit> enemies, List<Unit> counters)
        {
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
                var age = counter.GetAge(Civilization);
                for (int i = age; i <= 4; i++)
                {
                    counters_by_age[i].Add(counter);
                }
            }

            UnitStats.Clear();
            foreach (var unit in enemies.Concat(counters_by_age.Values.SelectMany(l => l)))
            {
                UnitStats[unit] = new UnitStats(unit, null);
            }

            var results = new List<Counter>();

            Parallel.ForEach(enemies, enemy =>
            {
                for (int age = 1; age <= 4; age++)
                {
                    var best_score = double.NegativeInfinity;
                    Counter current = new Counter();

                    foreach (var counter in counters_by_age[age])
                    {
                        var enemystats = UnitStats[enemy];
                        var counterstats = UnitStats[counter];
                        var score = GetScore(counterstats, enemystats);
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
    }
}
