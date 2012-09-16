using System.Text;

namespace Esprima.NET.Syntax
{
    public class Literal : SyntaxBase
    {
        private string _value;

        public Literal(ICodeGeneration generation) : base(generation)
        {
        }

        public string Value
        {
            get
            {
                return ((_value == "True" || _value == "False") && !IsString)
                           ? _value == "True" ? "true" : "false"
                           : _value;
            }
            set { _value = value; }
        }

        public bool IsString { get; set; }
    }
}