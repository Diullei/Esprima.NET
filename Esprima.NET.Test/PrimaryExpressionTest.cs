using Esprima.NET.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esprima.NET.Test
{
    [TestClass]
    public class PrimaryExpressionTest : BaseTeste
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
            VerifyRange(tree, 0, 5);
            // - loc
            VerifyLocation(tree, 1, 0, 2, 0);

            Assert.AreEqual(1, tree.Body.Count);

            // =================================== ExpressionStatement
            Assert.IsTrue(tree.Body[0] is ExpressionStatement);
            var expr = (ExpressionStatement)tree.Body[0];
            // - range
            VerifyRange(tree, 0, 5);
            // - loc
            VerifyLocation(tree, 1, 0, 2, 0);

            // =================================== ThisExpression
            Assert.IsTrue(expr.Expression is ThisExpression);
            var @this = (ThisExpression)expr.Expression;
            // - range
            VerifyRange(@this, 0, 4);
            // - loc
            VerifyLocation(@this, 1, 0, 1, 4);

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