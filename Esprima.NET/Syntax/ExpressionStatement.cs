namespace Esprima.NET.Syntax
{
    public class ExpressionStatement : SyntaxBase
    {
        public ExpressionStatement(ICodeGeneration generation) : base(generation)
        {
        }

        public dynamic Expression { get; set; }
    }
}