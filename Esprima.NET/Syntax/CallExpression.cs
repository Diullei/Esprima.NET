using System.Collections.Generic;

namespace Esprima.NET.Syntax
{
    public class CallExpression : SyntaxBase
    {
        public CallExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public Identifier Callee { get; set; }
        public List<dynamic> Arguments { get; set; }
    }
}