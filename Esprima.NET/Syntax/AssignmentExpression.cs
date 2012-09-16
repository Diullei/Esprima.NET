namespace Esprima.NET.Syntax
{
    public class AssignmentExpression : SyntaxBase
    {
        public AssignmentExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public string Operator { get; set; }
        public Identifier Left { get; set; }
        public dynamic Right { get; set; }
    }

    public class BinaryExpression : SyntaxBase
    {
        public BinaryExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public string Operator { get; set; }
        public dynamic Left { get; set; }
        public dynamic Right { get; set; }
    }
}