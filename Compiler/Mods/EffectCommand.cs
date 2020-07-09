using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Mods
{
    

    public abstract class EffectCommand
    {
        public static EffectCommand Get(YTY.AocDatLib.Effect effect)
        {
            throw new NotImplementedException();
        }
    }

    public class AttributeSetCommand : EffectCommand
    {
        public readonly int UnitId;
        public readonly int ClassId;
        public readonly Attribute Attribute;
        public readonly float Amount;

        public AttributeSetCommand(YTY.AocDatLib.Effect effect)
        {
            UnitId = effect.Arg1;
            ClassId = effect.Arg2;
            Attribute = (Attribute)effect.Arg3;
            Amount = effect.Arg4;
        }
    }
}
