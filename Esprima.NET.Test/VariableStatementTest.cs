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

    [TestClass]
    public class VariableStatementTest : BaseTeste
    {
        [TestMethod]
        public void VarX()
        {
            const string code = "var x";

            var tree = new Esprima().Parse(code);

            // =================================== ToString
            Assert.AreEqual("var x; ", tree.ToString());

            // =================================== Program
            // - range
            VerifyRange(tree, 0, 5);
            // - loc
            VerifyLocation(tree, 1, 0, 1, 5);

            Assert.AreEqual(1, tree.Body.Count);

            // =================================== VariableDeclaration
            Assert.IsTrue(tree.Body[0] is VariableDeclaration);
            var declaration = (VariableDeclaration)tree.Body[0];
            // - range
            VerifyRange(declaration, 0, 5);
            // - loc
            VerifyLocation(declaration, 1, 0, 1, 5);

            Assert.AreEqual("var", declaration.Kind);

            // =================================== VariableDeclarator
            Assert.IsTrue(tree.Body[0].Declarations[0] is VariableDeclarator);
            var var = (VariableDeclarator)tree.Body[0].Declarations[0];

            Assert.IsNull(var.Init);
            // - range
            VerifyRange(var, 4, 5);
            // - loc
            VerifyLocation(var, 1, 4, 1, 5);

            // =================================== Identifier
            Assert.IsTrue(tree.Body[0].Declarations[0].Id is Identifier);
            var id = (Identifier)tree.Body[0].Declarations[0].Id;
            Assert.AreEqual("x", id.ToString());

            // - range
            VerifyRange(id, 4, 5);
            // - loc
            VerifyLocation(id, 1, 4, 1, 5);
        }

        [TestMethod]
        public void VarXY()
        {
            const string code = "var x, y;";

            var tree = new Esprima().Parse(code);

            // =================================== ToString
            Assert.AreEqual("var x, y; ", tree.ToString());

            // =================================== Program
            // - range
            VerifyRange(tree, 0, 9);
            // - loc
            VerifyLocation(tree, 1, 0, 1, 9);

            Assert.AreEqual(1, tree.Body.Count);

            // =================================== VariableDeclaration
            Assert.IsTrue(tree.Body[0] is VariableDeclaration);
            var declaration = (VariableDeclaration)tree.Body[0];
            // - range
            VerifyRange(declaration, 0, 9);
            // - loc
            VerifyLocation(declaration, 1, 0, 1, 9); 

            Assert.AreEqual("var", declaration.Kind);

            // =================================== VariableDeclarator
            Assert.IsTrue(tree.Body[0].Declarations[0] is VariableDeclarator);
            var var1 = (VariableDeclarator)tree.Body[0].Declarations[0];
            Assert.IsNull(var1.Init);
            // - range
            VerifyRange(var1, 4, 5);
            // - loc
            VerifyLocation(var1, 1, 4, 1, 5);

            // =================================== Identifier
            Assert.IsTrue(tree.Body[0].Declarations[0].Id is Identifier);
            var id1 = (Identifier)tree.Body[0].Declarations[0].Id;
            Assert.AreEqual("x", id1.ToString());

            // - range
            VerifyRange(id1, 4, 5);
            // - loc
            VerifyLocation(id1, 1, 4, 1, 5);

            // =================================== VariableDeclarator
            Assert.IsTrue(tree.Body[0].Declarations[1] is VariableDeclarator);
            var var2 = (VariableDeclarator)tree.Body[0].Declarations[1];
            Assert.IsNull(var2.Init);
            // - range
            VerifyRange(var2, 7, 8);
            // - loc
            VerifyLocation(var2, 1, 7, 1, 8);

            // =================================== Identifier
            Assert.IsTrue(tree.Body[0].Declarations[1].Id is Identifier);
            var id2 = (Identifier)tree.Body[0].Declarations[1].Id;
            Assert.AreEqual("y", id2.ToString());

            // - range
            VerifyRange(id2, 7, 8);
            // - loc
            VerifyLocation(id2, 1, 7, 1, 8);
        }

        [TestMethod]
        public void VarX42()
        {
            const string code = "var x = 42";

            var tree = new Esprima().Parse(code);

            // =================================== ToString
            Assert.AreEqual("var x = 42; ", tree.ToString());

            // =================================== Program
            // - range
            VerifyRange(tree, 0, 10);
            // - loc
            VerifyLocation(tree, 1, 0, 1, 10);

            Assert.AreEqual(1, tree.Body.Count);

            // =================================== VariableDeclaration
            Assert.IsTrue(tree.Body[0] is VariableDeclaration);
            var declaration = (VariableDeclaration)tree.Body[0];
            // - range
            VerifyRange(declaration, 0, 10);
            // - loc
            VerifyLocation(declaration, 1, 0, 1, 10);

            Assert.AreEqual("var", declaration.Kind);

            // =================================== VariableDeclarator
            Assert.IsTrue(tree.Body[0].Declarations[0] is VariableDeclarator);
            var var1 = (VariableDeclarator)tree.Body[0].Declarations[0];
            Assert.IsNotNull(var1.Init);
            // - range
            VerifyRange(var1, 4, 10);
            // - loc
            VerifyLocation(var1, 1, 4, 1, 10);

            // =================================== Identifier
            Assert.IsTrue(tree.Body[0].Declarations[0].Id is Identifier);
            var id1 = (Identifier)tree.Body[0].Declarations[0].Id;
            Assert.AreEqual("x", id1.ToString());

            // - range
            VerifyRange(id1, 4, 5);
            // - loc
            VerifyLocation(id1, 1, 4, 1, 5);

            // =================================== Literal
            Assert.IsTrue(tree.Body[0].Declarations[0].Init is Literal);
            var init1 = (Literal)tree.Body[0].Declarations[0].Init;
            Assert.AreEqual("42", init1.ToString());

            // - range
            VerifyRange(init1, 8, 10);
            // - loc
            VerifyLocation(init1, 1, 8, 1, 10);
        }

        [TestMethod]
        public void VarEval42Arguments42()
        {
            const string code = "var eval = 42, arguments = 42";

            var tree = new Esprima().Parse(code);

            // =================================== ToString
            Assert.AreEqual("var eval = 42, arguments = 42; ", tree.ToString());

            // =================================== Program
            // - range
            VerifyRange(tree, 0, 29);
            // - loc
            VerifyLocation(tree, 1, 0, 1, 29);

            Assert.AreEqual(1, tree.Body.Count);

            // =================================== VariableDeclaration
            Assert.IsTrue(tree.Body[0] is VariableDeclaration);
            var declaration = (VariableDeclaration)tree.Body[0];
            // - range
            VerifyRange(declaration, 0, 29);
            // - loc
            VerifyLocation(declaration, 1, 0, 1, 29);

            Assert.AreEqual("var", declaration.Kind);

            // =================================== VariableDeclarator
            Assert.IsTrue(tree.Body[0].Declarations[0] is VariableDeclarator);
            var var1 = (VariableDeclarator)tree.Body[0].Declarations[0];
            Assert.IsNotNull(var1.Init);
            // - range
            VerifyRange(var1, 4, 13);
            // - loc
            VerifyLocation(var1, 1, 4, 1, 13);

            // =================================== Identifier
            Assert.IsTrue(tree.Body[0].Declarations[0].Id is Identifier);
            var id1 = (Identifier)tree.Body[0].Declarations[0].Id;
            Assert.AreEqual("eval", id1.ToString());

            // - range
            VerifyRange(id1, 4, 8);
            // - loc
            VerifyLocation(id1, 1, 4, 1, 8);

            // =================================== Literal
            Assert.IsTrue(tree.Body[0].Declarations[0].Init is Literal);
            var init1 = (Literal)tree.Body[0].Declarations[0].Init;
            Assert.AreEqual("42", init1.ToString());

            // - range
            VerifyRange(init1, 11, 13);
            // - loc
            VerifyLocation(init1, 1, 11, 1, 13);

            // =================================== VariableDeclarator
            Assert.IsTrue(tree.Body[0].Declarations[1] is VariableDeclarator);
            var var2 = (VariableDeclarator)tree.Body[0].Declarations[1];
            Assert.IsNotNull(var1.Init);
            // - range
            VerifyRange(var2, 15, 29);
            // - loc
            VerifyLocation(var2, 1, 15, 1, 29);

            // =================================== Identifier
            Assert.IsTrue(tree.Body[0].Declarations[1].Id is Identifier);
            var id2 = (Identifier)tree.Body[0].Declarations[1].Id;
            Assert.AreEqual("arguments", id2.ToString());

            // - range
            VerifyRange(id2, 15, 24);
            // - loc
            VerifyLocation(id2, 1, 15, 1, 24);

            // =================================== Literal
            Assert.IsTrue(tree.Body[0].Declarations[1].Init is Literal);
            var init2 = (Literal)tree.Body[0].Declarations[1].Init;
            Assert.AreEqual("42", init2.ToString());

            // - range
            VerifyRange(init2, 27, 29);
            // - loc
            VerifyLocation(init2, 1, 27, 1, 29);

        }
    }
}