using System.Text;

namespace Esprima.NET.Syntax
{
    public class Identifier : SyntaxBase
    {
        public Identifier(ICodeGeneration generation) : base(generation)
        {
        }

        public string Name { get; set; }
    }
}