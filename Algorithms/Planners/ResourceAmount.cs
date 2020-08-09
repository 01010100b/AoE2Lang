using System;
using System.Collections.Generic;
using System.Text;

namespace Algorithms.Planners
{
    public struct ResourceAmount
    {
        public readonly int ResourceId;
        public readonly int CurrentAmount;
        public readonly int MinimumAmount;
        public readonly int MaximumAmount;

        public ResourceAmount(int id, int amount, int min, int max)
        {
            ResourceId = id;
            CurrentAmount = amount;
            MinimumAmount = min;
            MaximumAmount = max;

            if (min > max)
            {
                throw new ArgumentException("min can not be greater than max");
            }

            if (amount < min || amount > max)
            {
                throw new ArgumentException("current must be between min and max");
            }
        }

        public ResourceAmount(int id, int amount) : this(id, amount, int.MinValue, int.MaxValue) { }
    }
}
