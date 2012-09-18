namespace Esprima.NET.Syntax
{
    public class IfStatement : SyntaxBase
    {
        public IfStatement(ICodeGeneration generation) : base(generation)
        {
        }

        public dynamic Test { get; set; }
        public dynamic Consequent { get; set; }
        public dynamic Alternate { get; set; }
    }
}