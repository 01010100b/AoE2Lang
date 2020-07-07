using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler.Lang
{
    static class Types
    {
        public static List<Type> BuiltinTypes => new List<Type>() { new IntType() };

        public abstract class Type
        {
            public abstract string Name { get; }
            public abstract int Size { get; }
        }

        public sealed class IntType : Type
        {
            public override string Name => "int";
            public override int Size => 1;
        }
    }
}
