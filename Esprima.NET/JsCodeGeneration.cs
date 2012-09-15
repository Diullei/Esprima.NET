using System;
using System.Text;
using Esprima.NET.Syntax;

namespace Esprima.NET
{
    public class JsCodeGeneration : ICodeGeneration
    {
        public string Generate(ISyntax syntax)
        {
            var sb = new StringBuilder();
            var typeName = syntax.GetType().Name;
            switch (typeName)
            {
                    #region "Program"

                case "Program":
                    var program = (syntax as Program);
                    program.Body.ForEach(b => sb.Append(b.ToString()));
                    break;

                    #endregion

                    #region "VariableDeclaration":

                case "VariableDeclaration":
                    var variableDeclaration = (syntax as VariableDeclaration);

                    sb.Append("var ");

                    var index = 0;
                    foreach (var d in variableDeclaration.Declarations)
                    {
                        if (index > 0) sb.Append(", ");
                        sb.Append(d.ToString());
                        index++;
                    }

                    sb.Append("; ");
                    break;

                    #endregion

                    #region "VariableDeclarator"

                case "VariableDeclarator":
                    var variableDeclarator = (syntax as VariableDeclarator);
                    sb.Append(variableDeclarator.Id.ToString());
                    if (variableDeclarator.Init != null)
                    {
                        sb.Append(" = ");
                        sb.Append(variableDeclarator.Init.ToString());
                    }
                    break;

                    #endregion

                    #region "Identifier"

                case "Identifier":
                    var identifier = (syntax as Identifier);
                    sb.Append(identifier.Name);

                    break;

                    #endregion

                    #region "Literal"

                case "Literal":
                    var literal = (syntax as Literal);
                    if (literal.IsString) sb.Append("\"");
                    sb.Append(literal.Value);
                    if (literal.IsString) sb.Append("\"");

                    break;

                    #endregion

                #region "ExpressionStatement"

                case "ExpressionStatement":
                    var expression = (syntax as ExpressionStatement);
                    sb.Append(expression.Expression.ToString());
                    break;

                    #endregion

                #region "ThisExpression"

                case "ThisExpression":
                    var thisExpression = (syntax as ThisExpression);
                    sb.Append(thisExpression.ToString());

                    break;

                #endregion
            }

            return sb.ToString();
        }
    }
}