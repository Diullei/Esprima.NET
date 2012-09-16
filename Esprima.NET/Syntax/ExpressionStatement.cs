using System.Collections.Generic;

namespace Esprima.NET.Syntax
{
    public class ExpressionStatement : SyntaxBase
    {
        public ExpressionStatement(ICodeGeneration generation) : base(generation)
        {
        }

        public dynamic Expression { get; set; }
    }

    public class CallExpression : SyntaxBase
    {
        public CallExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public Identifier Callee { get; set; }
        public List<dynamic> Arguments { get; set; }
    }
}