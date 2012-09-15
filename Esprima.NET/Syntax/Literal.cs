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
}