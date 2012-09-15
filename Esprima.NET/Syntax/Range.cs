namespace Esprima.NET.Syntax
{
    public class Range
    {
        private int _start;

        public int Start
        {
            get { return _start == End ? End - 1 : _start; }
            set { _start = value; }
        }

        public int End { get; set; }
    }
}