using System.Collections.Generic;

namespace Esprima.NET.Syntax
{
    public class BlockStatement : SyntaxBase
    {
        public BlockStatement(ICodeGeneration generation) : base(generation)
        {
        }

        public List<dynamic> Body { get; set; }
    }
}