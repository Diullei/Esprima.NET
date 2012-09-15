using Esprima.NET.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esprima.NET.Test
{
    [TestClass]
    public class PrimaryExpressionTest
    {
        [TestMethod]
        public void This()
        {
            const string code = "this\n";

            var tree = new Esprima().Parse(code);

            // =================================== ToString
            Assert.AreEqual("this", tree.ToString());

            // =================================== Program
            // - range
            Assert.AreEqual(0, tree.Range.Start);
            Assert.AreEqual(5, tree.Range.End);
            // - loc
            Assert.AreEqual(1, tree.Loc.Start.Line);
            Assert.AreEqual(0, tree.Loc.Start.Column);
            Assert.AreEqual(2, tree.Loc.End.Line);
            Assert.AreEqual(0, tree.Loc.End.Column);

            Assert.AreEqual(1, tree.Body.Count);

            // =================================== ExpressionStatement
            Assert.IsTrue(tree.Body[0] is ExpressionStatement);
            var expr = (ExpressionStatement)tree.Body[0];
            // - range
            Assert.AreEqual(0, expr.Range.Start);
            Assert.AreEqual(4, expr.Range.End);
            // - loc
            Assert.AreEqual(1, expr.Loc.Start.Line);
            Assert.AreEqual(0, expr.Loc.Start.Column);
            Assert.AreEqual(1, expr.Loc.End.Line);
            Assert.AreEqual(4, expr.Loc.End.Column);

            // =================================== ThisExpression
            Assert.IsTrue(expr.Expression is ThisExpression);
            var @this = (ThisExpression)expr.Expression;
            // - range
            Assert.AreEqual(0, @this.Range.Start);
            Assert.AreEqual(4, @this.Range.End);
            // - loc
            Assert.AreEqual(1, @this.Loc.Start.Line);
            Assert.AreEqual(0, @this.Loc.Start.Column);
            Assert.AreEqual(1, @this.Loc.End.Line);
            Assert.AreEqual(4, @this.Loc.End.Column);

            // =================================== Tokens
            Assert.AreEqual(1, tree.Extra.Tokens.Count);
            // - range
            Assert.AreEqual(0, tree.Extra.Tokens[0].Range.Start);
            Assert.AreEqual(4, tree.Extra.Tokens[0].Range.End);
            // - loc
            Assert.AreEqual(1, tree.Extra.Tokens[0].Loc.Start.Line);
            Assert.AreEqual(0, tree.Extra.Tokens[0].Loc.Start.Column);
            Assert.AreEqual(1, tree.Extra.Tokens[0].Loc.End.Line);
            Assert.AreEqual(4, tree.Extra.Tokens[0].Loc.End.Column);
        }
    }
}