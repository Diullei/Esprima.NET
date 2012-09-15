using System.Collections.Generic;
using System.Text;

namespace Esprima.NET.Syntax
{
    public class VariableDeclaration : SyntaxBase
    {
        public VariableDeclaration(ICodeGeneration generation)
            : base(generation)
        {
            Declarations = new List<dynamic>();
        }

        public List<dynamic> Declarations { get; set; }
        public string Kind { get; set; }
    }
}