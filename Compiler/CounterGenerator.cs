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

            return GetCounters(enemies, counters_by_age);
        }

        private List<Counter> GetCounters(List<Unit> enemies, Dictionary<int, List<Unit>> CountersByAge)
        {
            UnitStats.Clear();
            foreach (var unit in enemies.Concat(CountersByAge.Values.SelectMany(l => l)))
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

                    foreach (var counter in CountersByAge[age])
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
            var enemystats = UnitStats[enemy];
            var counterstats = UnitStats[counter];
            
            var attackenemy = 0d;
            foreach (var attack in enemystats.Attacks)
            {
                foreach (var armor in counterstats.Armors)
                {
                    if (attack.Id == armor.Id)
                    {
                        attackenemy += Math.Max(0, attack.Amount - armor.Amount);
                    }
                }
            }
            attackenemy = Math.Max(1, attackenemy);
            
            var attackcounter = 0d;
            foreach (var attack in counterstats.Attacks)
            {
                foreach (var armor in enemystats.Armors)
                {
                    if (attack.Id == armor.Id)
                    {
                        attackcounter += Math.Max(0, attack.Amount - armor.Amount);
                    }
                }
            }
            attackcounter = Math.Max(1, attackcounter);

            var enemyscore = Math.Ceiling(counterstats.Hitpoints / attackenemy) * enemystats.ReloadTime;
            var counterscore = Math.Ceiling(enemystats.Hitpoints / attackcounter) * counterstats.ReloadTime;

            enemyscore = 1d / Math.Max(1, enemyscore);
            counterscore = 1d / Math.Max(1, counterscore);

            var exp = enemystats.Range > 2 ? 1.9 : 1.6;
            enemyscore *= Math.Pow(1000d / enemystats.Cost.Total, exp);
            
            exp = counterstats.Range > 2 ? 1.9 : 1.6;
            counterscore *= Math.Pow(1000d / counterstats.Cost.Total, exp);

            return counterscore - enemyscore;
        }
    }
}
