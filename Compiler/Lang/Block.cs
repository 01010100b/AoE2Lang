using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Compiler.Lang.Statements;

namespace Compiler.Lang
{
    class Block : Block.IBlockElement
    {
        public interface IBlockElement
        {

        }

        public readonly List<IBlockElement> Elements = new List<IBlockElement>();
        public readonly List<Variable> LocalVariables = new List<Variable>();
    }
}
