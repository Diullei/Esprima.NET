using System.Text;

namespace Esprima.NET.Syntax
{
    public class VariableDeclarator : SyntaxBase
    {
        public VariableDeclarator(ICodeGeneration generation) : base(generation)
        {
        }

        public Identifier Id { get; set; }
        public dynamic Init { get; set; }
    }
}