namespace Esprima.NET.Syntax
{
    public class UpdateExpression : SyntaxBase
    {
        public UpdateExpression(ICodeGeneration generation) : base(generation)
        {
        }

        public string Operator { get; set; }
        public dynamic Argument { get; set; }
        public bool Prefix { get; set; }
    }
}