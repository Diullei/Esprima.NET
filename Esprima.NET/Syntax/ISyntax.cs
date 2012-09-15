namespace Esprima.NET.Syntax
{
    public interface ISyntax
    {
        Range Range { get; set; }
        Loc Loc { get; set; }
        string Generate();
    }
}
