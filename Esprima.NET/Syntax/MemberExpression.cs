namespace Esprima.NET.Syntax
{
    public class MemberExpression : SyntaxBase
    {
        public MemberExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public bool Computed { get; set; }
        public dynamic Object { get; set; }
        public dynamic Property { get; set; }
    }
}