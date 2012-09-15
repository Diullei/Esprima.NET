using Esprima.NET.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esprima.NET.Test
{
    [TestClass]
    public class BaseTeste
    {
        protected void VerifyRange(ISyntax syntax, int start, int end)
        {
            Assert.AreEqual(start, syntax.Range.Start);
            Assert.AreEqual(end, syntax.Range.End);
        }

        protected void VerifyLocation(ISyntax syntax, int startLine, int startColumn, int endLine, int endColumn)
        {
            Assert.AreEqual(startLine, syntax.Loc.Start.Line);
            Assert.AreEqual(startColumn, syntax.Loc.Start.Column);
            Assert.AreEqual(endLine, syntax.Loc.End.Line);
            Assert.AreEqual(endColumn, syntax.Loc.End.Column);
        }
    }
}