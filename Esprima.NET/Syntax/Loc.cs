namespace Esprima.NET.Syntax
{
    public class Loc
    {
        public class Position
        {
            public int Line { get; set; }
            public int Column { get; set; }
        }

        public Position Start { get; set; }
        public Position End { get; set; }
    }
}