using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    struct Cost
    {
        public readonly int Food;
        public readonly int Wood;
        public readonly int Gold;
        public readonly int Stone;

        public int Total => Food + Wood + Gold + Stone;

        public Cost(int food, int wood, int gold, int stone)
        {
            Food = food;
            Wood = wood;
            Gold = gold;
            Stone = stone;
        }

        public static Cost operator +(Cost a, Cost b)
        {
            return new Cost(a.Food + b.Food, a.Wood + b.Wood, a.Gold + b.Gold, a.Stone + b.Stone);
        }

        public static Cost operator -(Cost a, Cost b)
        {
            return new Cost(a.Food - b.Food, a.Wood - b.Wood, a.Gold - b.Gold, a.Stone - b.Stone);
        }

        public static Cost operator *(Cost a, double b)
        {
            return new Cost((int)(a.Food * b), (int)(a.Wood * b), (int)(a.Gold * b), (int)(a.Stone * b));
        }

        public static Cost operator /(Cost a, double b)
        {
            return new Cost((int)(a.Food / b), (int)(a.Wood / b), (int)(a.Gold / b), (int)(a.Stone / b));
        }
    }
}
