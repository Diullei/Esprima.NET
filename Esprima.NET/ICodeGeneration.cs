using Esprima.NET.Syntax;

namespace Esprima.NET
{
    public interface ICodeGeneration
    {
        string Generate(ISyntax syntax);
    }
}