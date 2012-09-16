namespace Esprima.NET.Syntax
{
    public abstract class SyntaxBase : ISyntax
    {
        public Range Range { get; set; }
        public Loc Loc { get; set; }

        public string Type
        {
            get { return this.GetType().Name; }
        }

        protected ICodeGeneration Generation { get; set; }

        protected SyntaxBase(ICodeGeneration generation)
        {
            Generation = generation;
        }

        public string Generate()
        {
            return Generation.Generate(this);
        }

        public override string ToString()
        {
            return Generate();
        }
    }
}