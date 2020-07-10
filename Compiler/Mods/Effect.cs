using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    class Effect
    {
        public readonly int Id;
        public readonly List<EffectCommand> Commands = new List<EffectCommand>();

        public Effect(int id)
        {
            Id = id;
        }
    }
}
