using System.Collections.Generic;
using System.Text;

namespace Esprima.NET.Syntax
{
    public class Identifier : SyntaxBase
    {
        public Identifier(ICodeGeneration generation) : base(generation)
        {
        }

        public string Name { get; set; }
    }

    public class ArrayExpression : SyntaxBase
    {
        public ArrayExpression(ICodeGeneration generation) : base(generation)
        {
            Elements = new List<object>();
        }

        public List<object> Elements { get; set; }
    }
}