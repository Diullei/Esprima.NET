using System;

namespace Esprima.NET.Ex
{
    public class ReferenceError : Exception
    {
        public ReferenceError(string @ref)
            : base(@ref.Trim().Split(' ').Length > 1 ? @ref : @ref + " is not defined")
        {
        }
    }
}
