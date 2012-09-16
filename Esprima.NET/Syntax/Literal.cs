using System.Text;

namespace Esprima.NET.Syntax
{
    public class Literal : SyntaxBase
    {
        public Literal(ICodeGeneration generation) : base(generation)
        {
        }

        public object Value { get; set; }
        public bool IsString { get; set; }
    }

    public class AssignmentExpression : SyntaxBase
    {
        public AssignmentExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public string Operator { get; set; }
        public Identifier Left { get; set; }
        public dynamic Right { get; set; }
    }
}