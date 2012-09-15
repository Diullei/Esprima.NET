namespace Esprima.NET.Syntax
{
    public class ThisExpression : SyntaxBase
    {
        public ThisExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public override string ToString()
        {
            return "this";
        }
    }
}