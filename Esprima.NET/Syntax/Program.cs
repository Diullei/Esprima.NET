using System.Collections.Generic;
using System.Text;

namespace Esprima.NET.Syntax
{
    public class Program : SyntaxBase
    {
        public Program(ICodeGeneration generation) : base(generation)
        {
        }

        public List<dynamic> Body { get; set; }
        public Esprima.Extra Extra { get; set; }
    }
}