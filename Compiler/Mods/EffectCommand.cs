using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YTY.AocDatLib;

namespace Compiler.Mods
{
    public abstract class EffectCommand
    {
        public enum ModifierMode
        {
            Set,
            Add,
            Multiply
        }

        public static EffectCommand Get(YTY.AocDatLib.Effect effect)
        {
            switch (effect.Command)
            {
                case 0:
                case 4:
                case 5: return new AttributeModifierCommand(effect);
                case 2: return new EnableDisableUnitCommand(effect);
                case 3: return new UpgradeUnitCommand(effect);
                case 102: return new DisableTechCommand(effect);
            }

            return null;
        }
    }

    public class AttributeModifierCommand : EffectCommand
    {
        public readonly int UnitId;
        public readonly UnitClass ClassId;
        public readonly Attribute Attribute;
        public readonly ModifierMode Mode;
        public readonly float Amount;
        public readonly int ArmorId;

        public AttributeModifierCommand(YTY.AocDatLib.Effect effect)
        {
            UnitId = effect.Arg1;
            ClassId = (UnitClass)effect.Arg2;
            Attribute = (Attribute)effect.Arg3;
            Amount = effect.Arg4;

            switch (effect.Command)
            {
                case 0: Mode = ModifierMode.Set; break;
                case 4: Mode = ModifierMode.Add; break;
                case 5: Mode = ModifierMode.Multiply; break;
            }

            ArmorId = 0;
            if (Attribute == Attribute.Armor || Attribute == Attribute.Attack)
            {
                var a = (int)Amount;
                ArmorId = a / 256;
                Amount = a % 256;
            }
        }
    }

    public class EnableDisableUnitCommand : EffectCommand
    {
        public readonly int UnitId;
        public readonly bool Enable;

        public EnableDisableUnitCommand(YTY.AocDatLib.Effect effect)
        {
            UnitId = effect.Arg1;
            if (effect.Arg2 == 1)
            {
                Enable = true;
            }
            else
            {
                Enable = false;
            }
        }
    }

    public class UpgradeUnitCommand : EffectCommand
    {
        public readonly int FromUnitId;
        public readonly int ToUnitId;

        public UpgradeUnitCommand(YTY.AocDatLib.Effect effect)
        {
            FromUnitId = effect.Arg1;
            ToUnitId = effect.Arg2;
        }
    }

    public class DisableTechCommand : EffectCommand
    {
        public readonly int TechId;

        public DisableTechCommand(YTY.AocDatLib.Effect effect)
        {
            TechId = (int)effect.Arg4;
        }
    }
}
