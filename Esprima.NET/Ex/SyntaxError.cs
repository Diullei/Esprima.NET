using System;

namespace Esprima.NET.Ex
{
    public class SyntaxError : Exception
    {
        public SyntaxError(string message)
            : base(message)
        {
        }
    }
}