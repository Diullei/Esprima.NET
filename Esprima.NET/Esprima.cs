using System;
using System.Collections.Generic;
using System.Linq;
using Esprima.NET.Syntax;

namespace Esprima.NET
{
    /// <summary>
    /// A port of esprima.js parser(https://github.com/ariya/esprima) to C#
    /// </summary>s
    public class Esprima
    {
        public class Token
        {
            public class TokenLoc
            {
                public class TokenPosition
                {
                    public int Line { get; set; }
                    public int Column { get; set; }
                }

                public TokenPosition Start { get; set; }
                public TokenPosition End { get; set; }
            }
            
            public class TokenRange
            {
                public int Start { get; set; }
                public int End { get; set; }
            }

            public TokenType Type { get; set; }
            public string Value { get; set; }
            internal int LineNumber { get; set; }
            internal int LineStart { get; set; }
            public TokenRange Range { get; set; }
            public TokenLoc Loc { get; set; }
            internal bool Octal { get; set; }
            internal string Literal { get; set; }

            public override string ToString()
            {
                return string.Format("{0}[{1}:{2};{3}] {4}", Type, LineNumber, Range.Start, Range.End, Value);
            }
        }

        public class Extra
        {
            public Extra()
            {
                this.Tokens = new List<Token>();
                this.Errors = new List<Exception>();
                this.Comments = new List<string>();
            }

            public List<Token> Tokens { get; set; }
            public List<Exception> Errors { get; set; }
            public List<string> Comments { get; set; }
        }

        public class Parameter
        {
            public string Name { get; set; }
        }

        public class State
        {
            public dynamic LastParenthesized { get; set; }
            public bool AllowIn { get; set; }
            public bool InIteration { get; set; }
            public Dictionary<string, object> LabelSet { get; set; }
            public bool InSwitch { get; set; }
            public bool InFunctionBody { get; set; }
        }

        private List<char> _source = new List<char>();
        private bool _strict;
        private bool _yieldAllowed;
        private bool _yieldFound;
        private int _index;
        private int _length;
        private int _lineNumber;
        private int _lineStart;
        private Token _buffer;
        private Extra _extra;
        private State _state = new State();

        private ICodeGeneration _codeGeneration;

        public enum TokenType
        {
            BooleanLiteral = 1,
            EOF = 2,
            Identifier = 3,
            Keyword = 4,
            NullLiteral = 5,
            NumericLiteral = 6,
            Punctuator = 7,
            StringLiteral = 8
        }

        private Dictionary<TokenType, string> _tokenName = new Dictionary<TokenType, string>
                                                          {
                                                              {TokenType.BooleanLiteral, "Boolean"},
                                                              {TokenType.EOF, "<end>"},
                                                              {TokenType.Identifier, "Identifier"},
                                                              {TokenType.Keyword, "Keyword"},
                                                              {TokenType.NullLiteral, "Null"},
                                                              {TokenType.NumericLiteral, "Numeric"},
                                                              {TokenType.Punctuator, "Punctuator"},
                                                              {TokenType.StringLiteral, "String"}
                                                          };

        private class Syntax
        {
            public static string AssignmentExpression { get { return "AssignmentExpression"; } }
            public static string ArrayExpression { get { return "ArrayExpression"; } }
            public static string ArrayPattern { get { return "ArrayPattern"; } }
            public static string BlockStatement { get { return "BlockStatement"; } }
            public static string BinaryExpression { get { return "BinaryExpression"; } }
            public static string BreakStatement { get { return "BreakStatement"; } }
            public static string CallExpression { get { return "CallExpression"; } }
            public static string CatchClause { get { return "CatchClause"; } }
            public static string ConditionalExpression { get { return "ConditionalExpression"; } }
            public static string ContinueStatement { get { return "ContinueStatement"; } }
            public static string DoWhileStatement { get { return "DoWhileStatement"; } }
            public static string DebuggerStatement { get { return "DebuggerStatement"; } }
            public static string EmptyStatement     { get { return "EmptyStatement"; } }
            public static string ExpressionStatement { get { return "ExpressionStatement"; } }
            public static string ForStatement { get { return "ForStatement"; } }
            public static string ForInStatement { get { return "ForInStatement"; } }
            public static string FunctionDeclaration { get { return "FunctionDeclaration"; } }
            public static string FunctionExpression { get { return "FunctionExpression"; } }
            public static string Identifier { get { return "Identifier"; } }
            public static string IfStatement { get { return "IfStatement"; } }
            public static string Literal { get { return "Literal"; } }
            public static string LabeledStatement { get { return "LabeledStatement"; } }
            public static string LogicalExpression { get { return "LogicalExpression"; } }
            public static string MemberExpression { get { return "MemberExpression"; } }
            public static string NewExpression { get { return "NewExpression"; } }
            public static string ObjectExpression { get { return "ObjectExpression"; } }
            public static string ObjectPattern { get { return "ObjectPattern"; } }
            public static string Program { get { return "Program"; } }
            public static string Property { get { return "Property"; } }
            public static string ReturnStatement { get { return "ReturnStatement"; } }
            public static string SequenceExpression { get { return "SequenceExpression"; } }
            public static string SwitchStatement { get { return "SwitchStatement"; } }
            public static string SwitchCase { get { return "SwitchCase"; } }
            public static string ThisExpression { get { return "ThisExpression"; } }
            public static string ThrowStatement { get { return "ThrowStatement"; } }
            public static string TryStatement { get { return "TryStatement"; } }
            public static string UnaryExpression { get { return "UnaryExpression"; } }
            public static string UpdateExpression { get { return "UpdateExpression"; } }
            public static string VariableDeclaration { get { return "VariableDeclaration"; } }
            public static string VariableDeclarator { get { return "VariableDeclarator"; } }
            public static string WhileStatement { get { return "WhileStatement"; } }
            public static string WithStatement { get { return "WithStatement"; } }
        }

        private enum PropertyKind
        {
            Data = 1,
            Get = 2,
            Set = 4
        }

        // Error messages should be identical to V8.
        private class Messages
        {
            public static string UnexpectedToken { get { return "Unexpected token {0}"; } }
            public static string UnexpectedNumber { get { return "Unexpected number"; } }
            public static string UnexpectedString { get { return "Unexpected string"; } }
            public static string UnexpectedIdentifier { get { return "Unexpected identifier"; } }
            public static string UnexpectedReserved { get { return "Unexpected reserved word"; } }
            public static string UnexpectedEOS { get { return "Unexpected end of input"; } }
            public static string NewlineAfterThrow { get { return "Illegal newline after throw"; } }
            public static string InvalidRegExp { get { return "Invalid regular expression"; } }
            public static string UnterminatedRegExp { get { return "Invalid regular expression: missing /"; } }
            public static string InvalidLHSInAssignment { get { return "Invalid left-hand side in assignment"; } }
            public static string InvalidLHSInForIn { get { return "Invalid left-hand side in for-in"; } }
            public static string MultipleDefaultsInSwitch { get { return "More than one default clause in switch statement"; } }
            public static string NoCatchOrFinally { get { return "Missing catch or finally after try"; } }
            public static string UnknownLabel { get { return "Undefined label \'{0}\'"; } }
            public static string Redeclaration { get { return "{0} \'{1}\' has already been declared"; } }
            public static string IllegalContinue { get { return "Illegal continue statement"; } }
            public static string IllegalBreak { get { return "Illegal break statement"; } }
            public static string IllegalReturn { get { return "Illegal return statement"; } }
            public static string StrictModeWith { get { return "Strict mode code may not include a with statement"; } }
            public static string StrictCatchVariable { get { return "Catch variable may not be eval or arguments in strict mode"; } }
            public static string StrictVarName { get { return "Variable name may not be eval or arguments in strict mode"; } }
            public static string StrictParamName { get { return "Parameter name eval or arguments is not allowed in strict mode"; } }
            public static string StrictParamDupe { get { return "Strict mode function may not have duplicate parameter names"; } }
            public static string StrictFunctionName { get { return "Function name may not be eval or arguments in strict mode"; } }
            public static string StrictOctalLiteral { get { return "Octal literals are not allowed in strict mode."; } }
            public static string StrictDelete { get { return "Delete of an unqualified identifier in strict mode."; } }
            public static string StrictDuplicateProperty { get { return "Duplicate data property in object literal not allowed in strict mode"; } }
            public static string AccessorDataProperty { get { return "Object literal may not have data and accessor property with the same name"; } }
            public static string AccessorGetSet { get { return "Object literal may not have multiple get/set accessors with the same name"; } }
            public static string StrictLHSAssignment { get { return "Assignment to eval or arguments is not allowed in strict mode"; } }
            public static string StrictLHSPostfix { get { return "Postfix increment/decrement may not have eval or arguments operand in strict mode"; } }
            public static string StrictLHSPrefix { get { return "Prefix increment/decrement may not have eval or arguments operand in strict mode"; } }
            public static string StrictReservedWord { get { return "Use of future reserved word in strict mode"; } }
        }

        // See also tools/generate-unicode-regex.py.
        private class Regex
        {
            public static System.Text.RegularExpressions.Regex NonAsciiIdentifierStart = new System.Text.RegularExpressions.Regex(@"[\xaa\xb5\xba\xc0-\xd6\xd8-\xf6\xf8-\u02c1\u02c6-\u02d1\u02e0-\u02e4\u02ec\u02ee\u0370-\u0374\u0376\u0377\u037a-\u037d\u0386\u0388-\u038a\u038c\u038e-\u03a1\u03a3-\u03f5\u03f7-\u0481\u048a-\u0527\u0531-\u0556\u0559\u0561-\u0587\u05d0-\u05ea\u05f0-\u05f2\u0620-\u064a\u066e\u066f\u0671-\u06d3\u06d5\u06e5\u06e6\u06ee\u06ef\u06fa-\u06fc\u06ff\u0710\u0712-\u072f\u074d-\u07a5\u07b1\u07ca-\u07ea\u07f4\u07f5\u07fa\u0800-\u0815\u081a\u0824\u0828\u0840-\u0858\u08a0\u08a2-\u08ac\u0904-\u0939\u093d\u0950\u0958-\u0961\u0971-\u0977\u0979-\u097f\u0985-\u098c\u098f\u0990\u0993-\u09a8\u09aa-\u09b0\u09b2\u09b6-\u09b9\u09bd\u09ce\u09dc\u09dd\u09df-\u09e1\u09f0\u09f1\u0a05-\u0a0a\u0a0f\u0a10\u0a13-\u0a28\u0a2a-\u0a30\u0a32\u0a33\u0a35\u0a36\u0a38\u0a39\u0a59-\u0a5c\u0a5e\u0a72-\u0a74\u0a85-\u0a8d\u0a8f-\u0a91\u0a93-\u0aa8\u0aaa-\u0ab0\u0ab2\u0ab3\u0ab5-\u0ab9\u0abd\u0ad0\u0ae0\u0ae1\u0b05-\u0b0c\u0b0f\u0b10\u0b13-\u0b28\u0b2a-\u0b30\u0b32\u0b33\u0b35-\u0b39\u0b3d\u0b5c\u0b5d\u0b5f-\u0b61\u0b71\u0b83\u0b85-\u0b8a\u0b8e-\u0b90\u0b92-\u0b95\u0b99\u0b9a\u0b9c\u0b9e\u0b9f\u0ba3\u0ba4\u0ba8-\u0baa\u0bae-\u0bb9\u0bd0\u0c05-\u0c0c\u0c0e-\u0c10\u0c12-\u0c28\u0c2a-\u0c33\u0c35-\u0c39\u0c3d\u0c58\u0c59\u0c60\u0c61\u0c85-\u0c8c\u0c8e-\u0c90\u0c92-\u0ca8\u0caa-\u0cb3\u0cb5-\u0cb9\u0cbd\u0cde\u0ce0\u0ce1\u0cf1\u0cf2\u0d05-\u0d0c\u0d0e-\u0d10\u0d12-\u0d3a\u0d3d\u0d4e\u0d60\u0d61\u0d7a-\u0d7f\u0d85-\u0d96\u0d9a-\u0db1\u0db3-\u0dbb\u0dbd\u0dc0-\u0dc6\u0e01-\u0e30\u0e32\u0e33\u0e40-\u0e46\u0e81\u0e82\u0e84\u0e87\u0e88\u0e8a\u0e8d\u0e94-\u0e97\u0e99-\u0e9f\u0ea1-\u0ea3\u0ea5\u0ea7\u0eaa\u0eab\u0ead-\u0eb0\u0eb2\u0eb3\u0ebd\u0ec0-\u0ec4\u0ec6\u0edc-\u0edf\u0f00\u0f40-\u0f47\u0f49-\u0f6c\u0f88-\u0f8c\u1000-\u102a\u103f\u1050-\u1055\u105a-\u105d\u1061\u1065\u1066\u106e-\u1070\u1075-\u1081\u108e\u10a0-\u10c5\u10c7\u10cd\u10d0-\u10fa\u10fc-\u1248\u124a-\u124d\u1250-\u1256\u1258\u125a-\u125d\u1260-\u1288\u128a-\u128d\u1290-\u12b0\u12b2-\u12b5\u12b8-\u12be\u12c0\u12c2-\u12c5\u12c8-\u12d6\u12d8-\u1310\u1312-\u1315\u1318-\u135a\u1380-\u138f\u13a0-\u13f4\u1401-\u166c\u166f-\u167f\u1681-\u169a\u16a0-\u16ea\u16ee-\u16f0\u1700-\u170c\u170e-\u1711\u1720-\u1731\u1740-\u1751\u1760-\u176c\u176e-\u1770\u1780-\u17b3\u17d7\u17dc\u1820-\u1877\u1880-\u18a8\u18aa\u18b0-\u18f5\u1900-\u191c\u1950-\u196d\u1970-\u1974\u1980-\u19ab\u19c1-\u19c7\u1a00-\u1a16\u1a20-\u1a54\u1aa7\u1b05-\u1b33\u1b45-\u1b4b\u1b83-\u1ba0\u1bae\u1baf\u1bba-\u1be5\u1c00-\u1c23\u1c4d-\u1c4f\u1c5a-\u1c7d\u1ce9-\u1cec\u1cee-\u1cf1\u1cf5\u1cf6\u1d00-\u1dbf\u1e00-\u1f15\u1f18-\u1f1d\u1f20-\u1f45\u1f48-\u1f4d\u1f50-\u1f57\u1f59\u1f5b\u1f5d\u1f5f-\u1f7d\u1f80-\u1fb4\u1fb6-\u1fbc\u1fbe\u1fc2-\u1fc4\u1fc6-\u1fcc\u1fd0-\u1fd3\u1fd6-\u1fdb\u1fe0-\u1fec\u1ff2-\u1ff4\u1ff6-\u1ffc\u2071\u207f\u2090-\u209c\u2102\u2107\u210a-\u2113\u2115\u2119-\u211d\u2124\u2126\u2128\u212a-\u212d\u212f-\u2139\u213c-\u213f\u2145-\u2149\u214e\u2160-\u2188\u2c00-\u2c2e\u2c30-\u2c5e\u2c60-\u2ce4\u2ceb-\u2cee\u2cf2\u2cf3\u2d00-\u2d25\u2d27\u2d2d\u2d30-\u2d67\u2d6f\u2d80-\u2d96\u2da0-\u2da6\u2da8-\u2dae\u2db0-\u2db6\u2db8-\u2dbe\u2dc0-\u2dc6\u2dc8-\u2dce\u2dd0-\u2dd6\u2dd8-\u2dde\u2e2f\u3005-\u3007\u3021-\u3029\u3031-\u3035\u3038-\u303c\u3041-\u3096\u309d-\u309f\u30a1-\u30fa\u30fc-\u30ff\u3105-\u312d\u3131-\u318e\u31a0-\u31ba\u31f0-\u31ff\u3400-\u4db5\u4e00-\u9fcc\ua000-\ua48c\ua4d0-\ua4fd\ua500-\ua60c\ua610-\ua61f\ua62a\ua62b\ua640-\ua66e\ua67f-\ua697\ua6a0-\ua6ef\ua717-\ua71f\ua722-\ua788\ua78b-\ua78e\ua790-\ua793\ua7a0-\ua7aa\ua7f8-\ua801\ua803-\ua805\ua807-\ua80a\ua80c-\ua822\ua840-\ua873\ua882-\ua8b3\ua8f2-\ua8f7\ua8fb\ua90a-\ua925\ua930-\ua946\ua960-\ua97c\ua984-\ua9b2\ua9cf\uaa00-\uaa28\uaa40-\uaa42\uaa44-\uaa4b\uaa60-\uaa76\uaa7a\uaa80-\uaaaf\uaab1\uaab5\uaab6\uaab9-\uaabd\uaac0\uaac2\uaadb-\uaadd\uaae0-\uaaea\uaaf2-\uaaf4\uab01-\uab06\uab09-\uab0e\uab11-\uab16\uab20-\uab26\uab28-\uab2e\uabc0-\uabe2\uac00-\ud7a3\ud7b0-\ud7c6\ud7cb-\ud7fb\uf900-\ufa6d\ufa70-\ufad9\ufb00-\ufb06\ufb13-\ufb17\ufb1d\ufb1f-\ufb28\ufb2a-\ufb36\ufb38-\ufb3c\ufb3e\ufb40\ufb41\ufb43\ufb44\ufb46-\ufbb1\ufbd3-\ufd3d\ufd50-\ufd8f\ufd92-\ufdc7\ufdf0-\ufdfb\ufe70-\ufe74\ufe76-\ufefc\uff21-\uff3a\uff41-\uff5a\uff66-\uffbe\uffc2-\uffc7\uffca-\uffcf\uffd2-\uffd7\uffda-\uffdc]");
            public static System.Text.RegularExpressions.Regex NonAsciiIdentifierPart = new System.Text.RegularExpressions.Regex(@"[\xaa\xb5\xba\xc0-\xd6\xd8-\xf6\xf8-\u02c1\u02c6-\u02d1\u02e0-\u02e4\u02ec\u02ee\u0300-\u0374\u0376\u0377\u037a-\u037d\u0386\u0388-\u038a\u038c\u038e-\u03a1\u03a3-\u03f5\u03f7-\u0481\u0483-\u0487\u048a-\u0527\u0531-\u0556\u0559\u0561-\u0587\u0591-\u05bd\u05bf\u05c1\u05c2\u05c4\u05c5\u05c7\u05d0-\u05ea\u05f0-\u05f2\u0610-\u061a\u0620-\u0669\u066e-\u06d3\u06d5-\u06dc\u06df-\u06e8\u06ea-\u06fc\u06ff\u0710-\u074a\u074d-\u07b1\u07c0-\u07f5\u07fa\u0800-\u082d\u0840-\u085b\u08a0\u08a2-\u08ac\u08e4-\u08fe\u0900-\u0963\u0966-\u096f\u0971-\u0977\u0979-\u097f\u0981-\u0983\u0985-\u098c\u098f\u0990\u0993-\u09a8\u09aa-\u09b0\u09b2\u09b6-\u09b9\u09bc-\u09c4\u09c7\u09c8\u09cb-\u09ce\u09d7\u09dc\u09dd\u09df-\u09e3\u09e6-\u09f1\u0a01-\u0a03\u0a05-\u0a0a\u0a0f\u0a10\u0a13-\u0a28\u0a2a-\u0a30\u0a32\u0a33\u0a35\u0a36\u0a38\u0a39\u0a3c\u0a3e-\u0a42\u0a47\u0a48\u0a4b-\u0a4d\u0a51\u0a59-\u0a5c\u0a5e\u0a66-\u0a75\u0a81-\u0a83\u0a85-\u0a8d\u0a8f-\u0a91\u0a93-\u0aa8\u0aaa-\u0ab0\u0ab2\u0ab3\u0ab5-\u0ab9\u0abc-\u0ac5\u0ac7-\u0ac9\u0acb-\u0acd\u0ad0\u0ae0-\u0ae3\u0ae6-\u0aef\u0b01-\u0b03\u0b05-\u0b0c\u0b0f\u0b10\u0b13-\u0b28\u0b2a-\u0b30\u0b32\u0b33\u0b35-\u0b39\u0b3c-\u0b44\u0b47\u0b48\u0b4b-\u0b4d\u0b56\u0b57\u0b5c\u0b5d\u0b5f-\u0b63\u0b66-\u0b6f\u0b71\u0b82\u0b83\u0b85-\u0b8a\u0b8e-\u0b90\u0b92-\u0b95\u0b99\u0b9a\u0b9c\u0b9e\u0b9f\u0ba3\u0ba4\u0ba8-\u0baa\u0bae-\u0bb9\u0bbe-\u0bc2\u0bc6-\u0bc8\u0bca-\u0bcd\u0bd0\u0bd7\u0be6-\u0bef\u0c01-\u0c03\u0c05-\u0c0c\u0c0e-\u0c10\u0c12-\u0c28\u0c2a-\u0c33\u0c35-\u0c39\u0c3d-\u0c44\u0c46-\u0c48\u0c4a-\u0c4d\u0c55\u0c56\u0c58\u0c59\u0c60-\u0c63\u0c66-\u0c6f\u0c82\u0c83\u0c85-\u0c8c\u0c8e-\u0c90\u0c92-\u0ca8\u0caa-\u0cb3\u0cb5-\u0cb9\u0cbc-\u0cc4\u0cc6-\u0cc8\u0cca-\u0ccd\u0cd5\u0cd6\u0cde\u0ce0-\u0ce3\u0ce6-\u0cef\u0cf1\u0cf2\u0d02\u0d03\u0d05-\u0d0c\u0d0e-\u0d10\u0d12-\u0d3a\u0d3d-\u0d44\u0d46-\u0d48\u0d4a-\u0d4e\u0d57\u0d60-\u0d63\u0d66-\u0d6f\u0d7a-\u0d7f\u0d82\u0d83\u0d85-\u0d96\u0d9a-\u0db1\u0db3-\u0dbb\u0dbd\u0dc0-\u0dc6\u0dca\u0dcf-\u0dd4\u0dd6\u0dd8-\u0ddf\u0df2\u0df3\u0e01-\u0e3a\u0e40-\u0e4e\u0e50-\u0e59\u0e81\u0e82\u0e84\u0e87\u0e88\u0e8a\u0e8d\u0e94-\u0e97\u0e99-\u0e9f\u0ea1-\u0ea3\u0ea5\u0ea7\u0eaa\u0eab\u0ead-\u0eb9\u0ebb-\u0ebd\u0ec0-\u0ec4\u0ec6\u0ec8-\u0ecd\u0ed0-\u0ed9\u0edc-\u0edf\u0f00\u0f18\u0f19\u0f20-\u0f29\u0f35\u0f37\u0f39\u0f3e-\u0f47\u0f49-\u0f6c\u0f71-\u0f84\u0f86-\u0f97\u0f99-\u0fbc\u0fc6\u1000-\u1049\u1050-\u109d\u10a0-\u10c5\u10c7\u10cd\u10d0-\u10fa\u10fc-\u1248\u124a-\u124d\u1250-\u1256\u1258\u125a-\u125d\u1260-\u1288\u128a-\u128d\u1290-\u12b0\u12b2-\u12b5\u12b8-\u12be\u12c0\u12c2-\u12c5\u12c8-\u12d6\u12d8-\u1310\u1312-\u1315\u1318-\u135a\u135d-\u135f\u1380-\u138f\u13a0-\u13f4\u1401-\u166c\u166f-\u167f\u1681-\u169a\u16a0-\u16ea\u16ee-\u16f0\u1700-\u170c\u170e-\u1714\u1720-\u1734\u1740-\u1753\u1760-\u176c\u176e-\u1770\u1772\u1773\u1780-\u17d3\u17d7\u17dc\u17dd\u17e0-\u17e9\u180b-\u180d\u1810-\u1819\u1820-\u1877\u1880-\u18aa\u18b0-\u18f5\u1900-\u191c\u1920-\u192b\u1930-\u193b\u1946-\u196d\u1970-\u1974\u1980-\u19ab\u19b0-\u19c9\u19d0-\u19d9\u1a00-\u1a1b\u1a20-\u1a5e\u1a60-\u1a7c\u1a7f-\u1a89\u1a90-\u1a99\u1aa7\u1b00-\u1b4b\u1b50-\u1b59\u1b6b-\u1b73\u1b80-\u1bf3\u1c00-\u1c37\u1c40-\u1c49\u1c4d-\u1c7d\u1cd0-\u1cd2\u1cd4-\u1cf6\u1d00-\u1de6\u1dfc-\u1f15\u1f18-\u1f1d\u1f20-\u1f45\u1f48-\u1f4d\u1f50-\u1f57\u1f59\u1f5b\u1f5d\u1f5f-\u1f7d\u1f80-\u1fb4\u1fb6-\u1fbc\u1fbe\u1fc2-\u1fc4\u1fc6-\u1fcc\u1fd0-\u1fd3\u1fd6-\u1fdb\u1fe0-\u1fec\u1ff2-\u1ff4\u1ff6-\u1ffc\u200c\u200d\u203f\u2040\u2054\u2071\u207f\u2090-\u209c\u20d0-\u20dc\u20e1\u20e5-\u20f0\u2102\u2107\u210a-\u2113\u2115\u2119-\u211d\u2124\u2126\u2128\u212a-\u212d\u212f-\u2139\u213c-\u213f\u2145-\u2149\u214e\u2160-\u2188\u2c00-\u2c2e\u2c30-\u2c5e\u2c60-\u2ce4\u2ceb-\u2cf3\u2d00-\u2d25\u2d27\u2d2d\u2d30-\u2d67\u2d6f\u2d7f-\u2d96\u2da0-\u2da6\u2da8-\u2dae\u2db0-\u2db6\u2db8-\u2dbe\u2dc0-\u2dc6\u2dc8-\u2dce\u2dd0-\u2dd6\u2dd8-\u2dde\u2de0-\u2dff\u2e2f\u3005-\u3007\u3021-\u302f\u3031-\u3035\u3038-\u303c\u3041-\u3096\u3099\u309a\u309d-\u309f\u30a1-\u30fa\u30fc-\u30ff\u3105-\u312d\u3131-\u318e\u31a0-\u31ba\u31f0-\u31ff\u3400-\u4db5\u4e00-\u9fcc\ua000-\ua48c\ua4d0-\ua4fd\ua500-\ua60c\ua610-\ua62b\ua640-\ua66f\ua674-\ua67d\ua67f-\ua697\ua69f-\ua6f1\ua717-\ua71f\ua722-\ua788\ua78b-\ua78e\ua790-\ua793\ua7a0-\ua7aa\ua7f8-\ua827\ua840-\ua873\ua880-\ua8c4\ua8d0-\ua8d9\ua8e0-\ua8f7\ua8fb\ua900-\ua92d\ua930-\ua953\ua960-\ua97c\ua980-\ua9c0\ua9cf-\ua9d9\uaa00-\uaa36\uaa40-\uaa4d\uaa50-\uaa59\uaa60-\uaa76\uaa7a\uaa7b\uaa80-\uaac2\uaadb-\uaadd\uaae0-\uaaef\uaaf2-\uaaf6\uab01-\uab06\uab09-\uab0e\uab11-\uab16\uab20-\uab26\uab28-\uab2e\uabc0-\uabea\uabec\uabed\uabf0-\uabf9\uac00-\ud7a3\ud7b0-\ud7c6\ud7cb-\ud7fb\uf900-\ufa6d\ufa70-\ufad9\ufb00-\ufb06\ufb13-\ufb17\ufb1d-\ufb28\ufb2a-\ufb36\ufb38-\ufb3c\ufb3e\ufb40\ufb41\ufb43\ufb44\ufb46-\ufbb1\ufbd3-\ufd3d\ufd50-\ufd8f\ufd92-\ufdc7\ufdf0-\ufdfb\ufe00-\ufe0f\ufe20-\ufe26\ufe33\ufe34\ufe4d-\ufe4f\ufe70-\ufe74\ufe76-\ufefc\uff10-\uff19\uff21-\uff3a\uff3f\uff41-\uff5a\uff66-\uffbe\uffc2-\uffc7\uffca-\uffcf\uffd2-\uffd7\uffda-\uffdc]");
        }

        private Regex _regex = new Regex();

        // Ensure the condition is true, otherwise throw an error.
        // This is only to have a better contract semantic, i.e. another safety net
        // to catch a logic error. The condition shall be fulfilled in normal case.
        // Do NOT use this to enforce a certain condition on any user input.
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("ASSERT: " + message);
            }
        }

        private List<char> SliceSource(int from, int to)
        {
            return _source.GetRange(from, to - from);
        }

        private bool IsDecimalDigit(char ch)
        {
            return "0123456789".IndexOf(ch) >= 0;
        }

        private bool IsHexDigit(char ch)
        {
            return "0123456789abcdefABCDEF".IndexOf(ch) >= 0;
        }

        private bool IsOctalDigit(char ch)
        {
            return "01234567".IndexOf(ch) >= 0;
        }

        // 7.2 White Space

        private bool IsWhiteSpace(char ch)
        {
            return (ch == ' ') || (ch == '\u0009') || (ch == '\u000B') ||
                   (ch == '\u000C') || (ch == '\u00A0') ||
                   (char.GetNumericValue(ch) >= 0x1680 && new char[]
                                        {
                                            '\u1680', '\u180E', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004',
                                            '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', '\u202F',
                                            '\u205F', '\u3000', '\uFEFF'
                                        }.Contains(ch));
        }

        // 7.3 Line Terminators

        private bool IsLineTerminator(char ch)
        {
            return (ch == '\n' || ch == '\r' || ch == '\u2028' || ch == '\u2029');
        }

        // 7.6 Identifier Names and Identifiers

        private bool IsIdentifierStart(char ch)
        {
            return (ch == '$') || (ch == '_') || (ch == '\\') ||
                   (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') ||
                   ((char.GetNumericValue(ch) >= 0x80) && Regex.NonAsciiIdentifierStart.IsMatch(ch.ToString()));
        }

        private bool IsIdentifierPart(char ch)
        {
            return (ch == '$') || (ch == '_') || (ch == '\\') ||
                   (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') ||
                   ((ch >= '0') && (ch <= '9')) ||
                   ((char.GetNumericValue(ch) >= 0x80) && Regex.NonAsciiIdentifierPart.IsMatch(ch.ToString()));
        }

        // 7.6.1.2 Future Reserved Words

        private bool IsFutureReservedWord(string id)
        {
            switch (id)
            {

                // Future reserved words. 
                case "class":
                case "enum":
                case "export":
                case "extends":
                case "import":
                case "super":
                    return true;
            }

            return false;
        }

        private bool IsStrictModeReservedWord(string id)
        {
            switch (id)
            {

                // Strict Mode reserved words. 
                case "implements":
                case "interface":
                case "package":
                case "private":
                case "protected":
                case "public":
                case "static":
                case "yield":
                case "let":
                    return true;
            }

            return false;
        }

        private bool IsRestrictedWord(string id)
        {
            return id == "eval" || id == "arguments";
        }

        private bool IsKeyword(string id)
        {
            var keyword = false;
            switch (id.Length)
            {
                case 2:
                    keyword = (id == "if") || (id == "in") || (id == "do");
                    break;
                case 3:
                    keyword = (id == "var") || (id == "for") || (id == "new") || (id == "try");
                    break;
                case 4:
                    keyword = (id == "this") || (id == "else") || (id == "case") || (id == "void") || (id == "with");
                    break;
                case 5:
                    keyword = (id == "while") || (id == "break") || (id == "catch") || (id == "throw");
                    break;
                case 6:
                    keyword = (id == "return") || (id == "typeof") || (id == "delete") || (id == "switch");
                    break;
                case 7:
                    keyword = (id == "default") || (id == "finally");
                    break;
                case 8:
                    keyword = (id == "function") || (id == "continue") || (id == "debugger");
                    break;
                case 10:
                    keyword = (id == "instanceof");
                    break;
            }

            if (keyword)
            {
                return true;
            }

            switch (id)
            {
                // Future reserved words. 
                // 'const' is specialized as Keyword in V8. 
                case "const":
                    return true;

                // For compatiblity to SpiderMonkey and ES.next
                case "yield":
                case "let":
                    return true;
            }

            if (_strict && IsStrictModeReservedWord(id))
            {
                return true;
            }

            return IsFutureReservedWord(id);
        }

        // Return the next character and move forward.

        private char NextChar()
        {
            return _source[_index++];
        }

        // 7.4 Comments

        private void SkipComment()
        {
            char ch;
            var blockComment = false;
            var lineComment = false;

            while (_index < _length)
            {
                ch = _source[_index];

                if (lineComment)
                {
                    ch = NextChar();
                    if (IsLineTerminator(ch))
                    {
                        lineComment = false;
                        if(_index > _source.Count)
                            if (ch == '\r' && _source[_index] == '\n')
                            {
                                ++_index;
                            }
                        ++_lineNumber;
                        _lineStart = _index;
                    }
                }
                else if (blockComment)
                {
                    if (IsLineTerminator(ch))
                    {
                        if (ch == '\r' && _source[_index + 1] == '\n')
                        {
                            ++_index;
                        }
                        ++_lineNumber;
                        ++_index;
                        _lineStart = _index;
                        if (_index >= _length)
                        {
                            ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                        }
                    }
                    else
                    {
                        ch = NextChar();
                        if (_index >= _length)
                        {
                            ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                        }
                        if (ch == '*')
                        {
                            ch = _source[_index];
                            if (ch == '/')
                            {
                                ++_index;
                                blockComment = false;
                            }
                        }
                    }
                }
                else if (ch == '/')
                {
                    ch = _source[_index + 1];
                    if (ch == '/')
                    {
                        _index += 2;
                        lineComment = true;
                    }
                    else if (ch == '*')
                    {
                        _index += 2;
                        blockComment = true;
                        if (_index >= _length)
                        {
                            ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else if (IsWhiteSpace(ch))
                {
                    ++_index;
                }
                else if (IsLineTerminator(ch))
                {
                    ++_index;
                    if (_index < _source.Count)
                        if (ch == '\r' && _source[_index] == '\n')
                        {
                            ++_index;
                        }
                    ++_lineNumber;
                    _lineStart = _index;
                }
                else
                {
                    break;
                }
            }
        }

        private char ScanHexEscape(char prefix)
        {
            char ch;
            var code = 0;

            var len = (prefix == 'u') ? 4 : 2;
            for (var i = 0; i < len; ++i)
            {
                if (_index < _length && IsHexDigit(_source[_index]))
                {
                    ch = NextChar();
                    code = code * 16 + "0123456789abcdef".IndexOf(ch.ToString().ToLower());
                }
                else
                {
                    return '\0';
                }
            }
            return Convert.ToChar(code);
        }

        private Token ScanIdentifier()
        {
            int start;
            int restore;
            string id;

            var ch = _source[_index];
            if (!IsIdentifierStart(ch))
            {
                return null;
            }

            start = _index;
            if (ch == '\\')
            {
                ++_index;
                if (_source[_index] != 'u')
                {
                    return null;
                }
                ++_index;
                restore = _index;
                ch = ScanHexEscape('u');
                //if (ch) {
                if (ch != '\0')
                {
                    if (ch == '\\' || !IsIdentifierStart(ch))
                    {
                        return null;
                    }
                    id = ch.ToString();
                }
                else
                {
                    _index = restore;
                    id = 'u'.ToString();
                }
            }
            else
            {
                id = NextChar().ToString();
            }

            while (_index < _length)
            {
                ch = _source[_index];
                if (!IsIdentifierPart(ch))
                {
                    break;
                }
                if (ch == '\\')
                {
                    ++_index;
                    if (_source[_index] != 'u')
                    {
                        return null;
                    }
                    ++_index;
                    restore = _index;
                    ch = ScanHexEscape('u');
                    //if (ch) {
                    if (ch != '\0')
                    {
                        if (ch == '\\' || !IsIdentifierPart(ch))
                        {
                            return null;
                        }
                        id += ch;
                    }
                    else
                    {
                        _index = restore;
                        id += 'u';
                    }
                }
                else
                {
                    id += NextChar();
                }
            }

            // There is no keyword or literal with only one character.
            // Thus, it must be an identifier.
            if (id.Length == 1)
            {
                return new Token
                {
                    Type = TokenType.Identifier,
                    Value = id,
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            if (IsKeyword(id))
            {
                return new Token
                {
                    Type = TokenType.Keyword,
                    Value = id,
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            // 7.8.1 Null Literals

            if (id == "null")
            {
                return new Token
                {
                    Type = TokenType.NullLiteral,
                    Value = id,
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            // 7.8.2 Boolean Literals

            if (id == "true" || id == "false")
            {
                return new Token
                {
                    Type = TokenType.BooleanLiteral,
                    Value = id,
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            return new Token
            {
                Type = TokenType.Identifier,
                Value = id,
                LineNumber = _lineNumber,
                LineStart = _lineStart,
                Range = new Token.TokenRange() { Start = start, End = _index }
            };
        }

        // 7.7 Punctuators

        private Token ScanPunctuator()
        {
            var start = _index;
            var ch1 = _source[_index];

            // Check for most common single-character punctuators.

            if (ch1 == ';' || ch1 == '{' || ch1 == '}')
            {
                ++_index;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = ch1.ToString(),
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            if (ch1 == ',' || ch1 == '(' || ch1 == ')')
            {
                ++_index;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = ch1.ToString(),
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            // Dot (.) can also start a floating-point number, hence the need
            // to check the next character.

            var ch2 = (_index + 1) < _source.Count ? _source[_index + 1] : '\0';
            if (ch1 == '.' && !IsDecimalDigit(ch2))
            {
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = NextChar().ToString(),
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            // Peek more characters.

            var ch3 = (_index + 2) < _source.Count ? _source[_index + 2] : '\0';
            var ch4 = (_index + 3) < _source.Count ? _source[_index + 3] : '\0';

            // 4-character punctuator: >>>=

            if (ch1 == '>' && ch2 == '>' && ch3 == '>')
            {
                if (ch4 == '=')
                {
                    _index += 4;
                    return new Token
                    {
                        Type = TokenType.Punctuator,
                        Value = ">>>=",
                        LineNumber = _lineNumber,
                        LineStart = _lineStart,
                        Range = new Token.TokenRange() { Start = start, End = _index }
                    };
                }
            }

            // 3-character punctuators: === !== >>> <<= >>=

            if (ch1 == '=' && ch2 == '=' && ch3 == '=')
            {
                _index += 3;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = "===",
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            if (ch1 == '!' && ch2 == '=' && ch3 == '=')
            {
                _index += 3;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = "!==",
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            if (ch1 == '>' && ch2 == '>' && ch3 == '>')
            {
                _index += 3;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = ">>>",
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            if (ch1 == '<' && ch2 == '<' && ch3 == '=')
            {
                _index += 3;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = "<<=",
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            if (ch1 == '>' && ch2 == '>' && ch3 == '=')
            {
                _index += 3;
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = ">>=",
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            // 2-character punctuators: <= >= == != ++ -- << >> && ||
            // += -= *= %= &= |= ^= /=

            if (ch2 == '=')
            {
                if ("<>=!+-*%&|^/".IndexOf(ch1) >= 0)
                {
                    _index += 2;
                    return new Token
                    {
                        Type = TokenType.Punctuator,
                        Value = new string(new char[] { ch1, ch2 }),
                        LineNumber = _lineNumber,
                        LineStart = _lineStart,
                        Range = new Token.TokenRange() { Start = start, End = _index }
                    };
                }
            }

            if (ch1 == ch2 && ("+-<>&|".IndexOf(ch1) >= 0))
            {
                if ("+-<>&|".IndexOf(ch2) >= 0)
                {
                    _index += 2;
                    return new Token
                    {
                        Type = TokenType.Punctuator,
                        Value = new string(new char[] { ch1, ch2 }),
                        LineNumber = _lineNumber,
                        LineStart = _lineStart,
                        Range = new Token.TokenRange() { Start = start, End = _index }
                    };
                }
            }

            // The remaining 1-character punctuators.

            if ("[]<>+-*%&|^!~?:=/".IndexOf(ch1) >= 0)
            {
                return new Token
                {
                    Type = TokenType.Punctuator,
                    Value = NextChar().ToString(),
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = start, End = _index }
                };
            }

            return null;
        }

        // 7.8.3 Numeric Literals

        private Token ScanNumericLiteral()
        {
            //var number, start, ch;

            var ch = _source[_index];
            Assert(IsDecimalDigit(ch) || (ch == '.'),
                   "Numeric literal must start with a decimal digit or a decimal point");

            var start = _index;
            var number = '\0'.ToString();
            if (ch != '.')
            {
                number = NextChar().ToString();
                ch = (_index < _source.Count ? _source[_index] : '\0');// _source[_index];

                // Hex number starts with '0x'.
                // Octal number starts with '0'.
                if (number == '0'.ToString())
                {
                    if (ch == 'x' || ch == 'X')
                    {
                        number += NextChar();
                        while (_index < _length)
                        {
                            ch = _source[_index];
                            if (!IsHexDigit(ch))
                            {
                                break;
                            }
                            number += NextChar();
                        }

                        if (number.Length <= 2)
                        {
                            // only 0x
                            ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                        }

                        if (_index < _length)
                        {
                            ch = _source[_index];
                            if (IsIdentifierStart(ch))
                            {
                                ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                            }
                        }
                        return new Token
                        {
                            Type = TokenType.NumericLiteral,
                            Value = number,
                            LineNumber = _lineNumber,
                            LineStart = _lineStart,
                            Range = new Token.TokenRange() { Start = start, End = _index }
                        };
                    }
                    else if (IsOctalDigit(ch))
                    {
                        number += NextChar();
                        while (_index < _length)
                        {
                            ch = _source[_index];
                            if (!IsOctalDigit(ch))
                            {
                                break;
                            }
                            number += NextChar();
                        }

                        if (_index < _length)
                        {
                            ch = _source[_index];
                            if (IsIdentifierStart(ch) || IsDecimalDigit(ch))
                            {
                                ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                            }
                        }
                        return new Token
                        {
                            Type = TokenType.NumericLiteral,
                            Value = number,
                            Octal = true,
                            LineNumber = _lineNumber,
                            LineStart = _lineStart,
                            Range = new Token.TokenRange() { Start = start, End = _index }
                        };
                    }

                    // decimal number starts with '0' such as '09' is illegal.
                    if (IsDecimalDigit(ch))
                    {
                        ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                    }
                }

                while (_index < _length)
                {
                    ch = _source[_index];
                    if (!IsDecimalDigit(ch))
                    {
                        break;
                    }
                    number += NextChar();
                }
            }

            if (ch == '.')
            {
                number += NextChar();
                while (_index < _length)
                {
                    ch = _source[_index];
                    if (!IsDecimalDigit(ch))
                    {
                        break;
                    }
                    number += NextChar();
                }
            }

            if (ch == 'e' || ch == 'E')
            {
                number += NextChar();

                ch = _source[_index];
                if (ch == '+' || ch == '-')
                {
                    number += NextChar();
                }

                ch = _source[_index];
                if (IsDecimalDigit(ch))
                {
                    number += NextChar();
                    while (_index < _length)
                    {
                        ch = _source[_index];
                        if (!IsDecimalDigit(ch))
                        {
                            break;
                        }
                        number += NextChar();
                    }
                }
                else
                {
                    //ch = "character " + ch;
                    if (_index >= _length)
                    {
                        //ch = "<end>";
                    }
                    ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                }
            }

            if (_index < _length)
            {
                ch = _source[_index];
                if (IsIdentifierStart(ch))
                {
                    ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
                }
            }

            return new Token
            {
                Type = TokenType.NumericLiteral,
                Value = number,
                LineNumber = _lineNumber,
                LineStart = _lineStart,
                Range = new Token.TokenRange() { Start = start, End = _index }
            };
        }

        // 7.8.4 String Literals

        private Token ScanStringLiteral()
        {
            //var str = '', quote, start, ch, code, unescaped, restore, octal = false;
            var str = "";
            int restore;
            char unescaped;
            int code;
            var octal = false;

            var quote = _source[_index];
            Assert((quote == '\'' || quote == '"'), "String literal must starts with a quote");

            var start = _index;
            ++_index;

            while (_index < _length)
            {
                var ch = NextChar();

                if (ch == quote)
                {
                    quote = '\0';
                    break;
                }
                else if (ch == '\\')
                {
                    ch = NextChar();
                    if (!IsLineTerminator(ch))
                    {
                        switch (ch)
                        {
                            case 'n':
                                str += '\n';
                                break;
                            case 'r':
                                str += '\r';
                                break;
                            case 't':
                                str += '\t';
                                break;
                            case 'u':
                            case 'x':
                                restore = _index;
                                unescaped = ScanHexEscape(ch);
                                if (unescaped != '\0')
                                {
                                    str += unescaped;
                                }
                                else
                                {
                                    _index = restore;
                                    str += ch;
                                }
                                break;
                            case 'b':
                                str += '\b';
                                break;
                            case 'f':
                                str += '\f';
                                break;
                            case 'v':
                                str += '\v';
                                break;

                            default:
                                if (IsOctalDigit(ch))
                                {
                                    code = "01234567".IndexOf(ch);

                                    // \0 is not octal escape sequence
                                    if (code != 0)
                                    {
                                        octal = true;
                                    }

                                    if (_index < _length && IsOctalDigit(_source[_index]))
                                    {
                                        octal = true;
                                        code = code * 8 + "01234567".IndexOf(NextChar());

                                        // 3 digits are only allowed when string starts
                                        // with 0, 1, 2, 3
                                        if ("0123".IndexOf(ch) >= 0 &&
                                            _index < _length &&
                                            IsOctalDigit(_source[_index]))
                                        {
                                            code = code * 8 + "01234567".IndexOf(NextChar());
                                        }
                                    }
                                    str += Convert.ToChar(code).ToString();
                                }
                                else
                                {
                                    str += ch;
                                }
                                break;
                        }
                    }
                    else
                    {
                        ++_lineNumber;
                        if (ch == '\r' && _source[_index] == '\n')
                        {
                            ++_index;
                        }
                    }
                }
                else if (IsLineTerminator(ch))
                {
                    break;
                }
                else
                {
                    str += ch;
                }
            }

            if (quote != '\0')
            {
                ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
            }

            return new Token
            {
                Type = TokenType.StringLiteral,
                Value = str,
                Octal = octal,
                LineNumber = _lineNumber,
                LineStart = _lineStart,
                Range = new Token.TokenRange() { Start = start, End = _index }
            };
        }

        private Token ScanRegExp()
        {
            var str = ""; //, ch, start, pattern, flags, value, classMarker = false, restore, terminated = false;
            var classMarker = false;
            var terminated = false;
            string pattern;
            int restore;
            var value = "";

            _buffer = null;
            SkipComment();

            var start = _index;
            var ch = _source[_index];
            Assert(ch == '/', "Regular expression literal must start with a slash");
            str = NextChar().ToString();

            while (_index < _length)
            {
                ch = NextChar();
                str += ch;
                if (classMarker)
                {
                    if (ch == ']')
                    {
                        classMarker = false;
                    }
                }
                else
                {
                    if (ch == '\\')
                    {
                        ch = NextChar();
                        // ECMA-262 7.8.5
                        if (IsLineTerminator(ch))
                        {
                            ThrowError(null, Messages.UnterminatedRegExp);
                        }
                        str += ch;
                    }
                    else if (ch == '/')
                    {
                        terminated = true;
                        break;
                    }
                    else if (ch == '[')
                    {
                        classMarker = true;
                    }
                    else if (IsLineTerminator(ch))
                    {
                        ThrowError(null, Messages.UnterminatedRegExp);
                    }
                }
            }

            if (!terminated)
            {
                ThrowError(null, Messages.UnterminatedRegExp);
            }

            // Exclude leading and trailing slash.
            pattern = str.Substring(1, str.Length - 2);

            var flags = '\0';
            while (_index < _length)
            {
                ch = _source[_index];
                if (!IsIdentifierPart(ch))
                {
                    break;
                }

                ++_index;
                if (ch == '\\' && _index < _length)
                {
                    ch = _source[_index];
                    if (ch == 'u')
                    {
                        ++_index;
                        restore = _index;
                        ch = ScanHexEscape('u');
                        if (ch != '\0')
                        {
                            flags += ch;
                            str += "\\u";
                            for (; restore < _index; ++restore)
                            {
                                str += _source[restore];
                            }
                        }
                        else
                        {
                            _index = restore;
                            flags += 'u';
                            str += "\\u";
                        }
                    }
                    else
                    {
                        str += '\\';
                    }
                }
                else
                {
                    flags += ch;
                    str += ch;
                }
            }

            try
            {
                value = new System.Text.RegularExpressions.Regex(pattern).Match(flags.ToString()).Value;
            }
            catch (Exception e)
            {
                ThrowError(null, Messages.InvalidRegExp);
            }

            return new Token
            {
                Literal = str,
                Value = value,
                Range = new Token.TokenRange() { Start = start, End = _index }
            };
        }

        private bool IsIdentifierName(Token token)
        {
            return token.Type == TokenType.Identifier ||
                   token.Type == TokenType.Keyword ||
                   token.Type == TokenType.BooleanLiteral ||
                   token.Type == TokenType.NullLiteral;
        }

        private Token Advance()
        {
            return CollectToken();
        }

        private Token _Advance()
        {
            //var ch, token;

            SkipComment();

            if (_index >= _length)
            {
                return new Token
                {
                    Type = TokenType.EOF,
                    LineNumber = _lineNumber,
                    LineStart = _lineStart,
                    Range = new Token.TokenRange() { Start = _index, End = _index }
                };
            }

            var token = ScanPunctuator();
            if (token != null)
            {
                return token;
            }

            var ch = _source[_index];

            if (ch == '\'' || ch == '"')
            {
                return ScanStringLiteral();
            }

            if (ch == '.' || IsDecimalDigit(ch))
            {
                return ScanNumericLiteral();
            }

            token = ScanIdentifier();
            if (token != null)
            {
                return token;
            }

            ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));

            return null;
        }

        private Token Lex()
        {
            if (_buffer != null)
            {
                _index = _buffer.Range.End;
                _lineNumber = _buffer.LineNumber;
                _lineStart = _buffer.LineStart;
                var token = _buffer;
                _buffer = null;
                return token;
            }

            _buffer = null;
            return Advance();
        }

        private Token Lookahead()
        {
            if (_buffer != null)
            {
                return _buffer;
            }

            var pos = _index;
            var line = _lineNumber;
            var start = _lineStart;
            _buffer = Advance();
            _index = pos;
            _lineNumber = line;
            _lineStart = start;

            return _buffer;
        }

        // Return true if there is a line terminator before the next token.

        private bool PeekLineTerminator()
        {
            //var pos, line, start, found;

            var pos = _index;
            var line = _lineNumber;
            var start = _lineStart;
            SkipComment();
            var found = _lineNumber != line;
            _index = pos;
            _lineNumber = line;
            _lineStart = start;

            return found;
        }

        // Throw an exception

        public class Error : Exception
        {
            public Error(string message)
                : base(message)
            {
            }

            public int Index { get; set; }
            public int LineNumber { get; set; }
            public int Column { get; set; }
        }

        private void ThrowError(Token token, string msg)
        {
            Error error = null;

            //if (typeof token.lineNumber === 'number') {
            if (token != null)
            {
                error = new Error("Line " + token.LineNumber + ": " + msg)
                {
                    Index = token.Range.Start,
                    LineNumber = token.LineNumber,
                    Column = token.Range.Start - _lineStart + 1
                };
            }
            else
            {
                error = new Error("Line " + _lineNumber + ": " + msg)
                {
                    Index = _index,
                    LineNumber = _lineNumber,
                    Column = _index - _lineStart + 1
                };
            }

            throw error;
        }

        private void ThrowErrorTolerant(Token token, string msg)
        {
            try
            {
                ThrowError(token, msg);
            }
            catch (Exception e)
            {
                if (_extra.Errors.Count > 0)
                {
                    _extra.Errors.Add(e);
                }
                else
                {
                    throw e;
                }
            }
        }

        // Throw an exception because of the token.

        private void ThrowUnexpected(Token token)
        {
            if (token.Type == TokenType.EOF)
            {
                ThrowError(token, Messages.UnexpectedEOS);
            }

            if (token.Type == TokenType.NumericLiteral)
            {
                ThrowError(token, Messages.UnexpectedNumber);
            }

            if (token.Type == TokenType.StringLiteral)
            {
                ThrowError(token, Messages.UnexpectedString);
            }

            if (token.Type == TokenType.Identifier)
            {
                ThrowError(token, Messages.UnexpectedIdentifier);
            }

            if (token.Type == TokenType.Keyword)
            {
                if (IsFutureReservedWord(token.Value))
                {
                    ThrowError(token, Messages.UnexpectedReserved);
                }
                else if (_strict && IsStrictModeReservedWord(token.Value))
                {
                    ThrowError(token, Messages.StrictReservedWord);
                }
                ThrowError(token, string.Format(Messages.UnexpectedToken, token.Value));
            }

            // BooleanLiteral, NullLiteral, or Punctuator.
            ThrowError(token, string.Format(Messages.UnexpectedToken, token.Value));
        }

        // Expect the next token to match the specified punctuator.
        // If not, an exception will be thrown.

        private void Expect(string value)
        {
            var token = Lex();
            if (token.Type != TokenType.Punctuator || token.Value != value)
            {
                ThrowUnexpected(token);
            }
        }

        private void Expect(char value)
        {
            Expect(value.ToString());
        }

        // Expect the next token to match the specified keyword.
        // If not, an exception will be thrown.

        private void ExpectKeyword(string keyword)
        {
            var token = Lex();
            if (token.Type != TokenType.Keyword || token.Value != keyword)
            {
                ThrowUnexpected(token);
            }
        }


        // Return true if the next token matches the specified punctuator.

        private bool Match(string value)
        {
            var token = Lookahead();
            return token.Type == TokenType.Punctuator && token.Value == value;
        }

        private bool Match(char value)
        {
            return Match(value.ToString());
        }

        // Return true if the next token matches the specified keyword

        private bool MatchKeyword(string keyword)
        {
            var token = Lookahead();
            return token.Type == TokenType.Keyword && token.Value == keyword;
        }

        // Return true if the next token matches the specified contextual keyword

        private bool MatchContextualKeyword(string keyword)
        {
            var token = Lookahead();
            return token.Type == TokenType.Identifier && token.Value == keyword;
        }

        // Return true if the next token is an assignment operator

        private bool MatchAssign()
        {
            var token = Lookahead();
            var op = token.Value;

            if (token.Type != TokenType.Punctuator)
            {
                return false;
            }
            return op == "=" ||
                   op == "*=" ||
                   op == "/=" ||
                   op == "%=" ||
                   op == "+=" ||
                   op == "-=" ||
                   op == "<<=" ||
                   op == ">>=" ||
                   op == ">>>=" ||
                   op == "&=" ||
                   op == "^=" ||
                   op == "|=";
        }

        private void ConsumeSemicolon()
        {
            //var token, line;

            // Catch the very common case first.
            //if (_source[_index] == ';')
            if ((_index < _source.Count ? _source[_index] : '\0') == ';')
            {
                Lex();
                return;
            }

            var line = _lineNumber;
            SkipComment();
            if (_lineNumber != line)
            {
                return;
            }

            if (Match(";"))
            {
                Lex();
                return;
            }

            var token = Lookahead();
            if (token.Type != TokenType.EOF && !Match("}"))
            {
                ThrowUnexpected(token);
            }
            return;
        }

        // Return true if provided expression is LeftHandSideExpression

        private bool IsLeftHandSide(dynamic expr)
        {
            return expr.Type == Syntax.Identifier || expr.Type == Syntax.MemberExpression;
        }

        // 11.1.4 Array Initialiser

        private dynamic ParseArrayInitialiser()
        {
            var elements = new List<object>();

            Expect("[");

            while (!Match("]"))
            {
                if (Match(","))
                {
                    Lex();
                    elements.Add(null);
                }
                else
                {
                    elements.Add(ParseAssignmentExpression());

                    if (!Match("]"))
                    {
                        Expect(",");
                    }
                }
            }

            Expect("]");

            return new
            {
                Type = Syntax.ArrayExpression,
                Elements = elements
            };
        }

        // 11.1.5 Object Initialiser

        private dynamic ParsePropertyFunction(List<Parameter> param, Token first)
        {
            //var previousStrict, body;

            var previousStrict = _strict;
            var body = ParseFunctionSourceElements();
            if (first != null && _strict && IsRestrictedWord(param[0].Name))
            {
                ThrowError(first, Messages.StrictParamName);
            }
            _strict = previousStrict;

            return new
            {
                Type = Syntax.FunctionExpression,
                //Id = null,
                Params = param,
                //Defaults = null,
                Body = body,
                //Rest = null,
                Generator = false,
                IsExpression = false
            };
        }

        private dynamic ParseObjectPropertyKey()
        {
            var start = _index - _lineNumber;// +2;//xxx
            var token = Lex();

            // Note: This function is called only from parseObjectProperty(), where
            // EOF and Punctuator tokens are already filtered out.

            if (token.Type == TokenType.StringLiteral || token.Type == TokenType.NumericLiteral)
            {
                if (_strict && token.Octal)
                {
                    ThrowError(token, Messages.StrictOctalLiteral);
                }
                return CreateLiteral(token);
            }

            return new Identifier(_codeGeneration)
            {
                Name = token.Value,
                Range = new Range { Start = token.Range.Start, End = token.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = token.Loc.Start.Line, Column = token.Loc.Start.Column },
                        End = new Loc.Position { Line = token.Loc.End.Line, Column = token.Loc.End.Column }
                    }
            };
        }

        private dynamic ParseObjectProperty()
        {
            //var token, key, id, param;

            var token = Lookahead();

            if (token.Type == TokenType.Identifier)
            {

                var id = ParseObjectPropertyKey();

                // Property Assignment: Getter and Setter.

                if (token.Value == "get" && !Match(":"))
                {
                    var key = ParseObjectPropertyKey();
                    Expect("(");
                    Expect(")");
                    return new
                    {
                        Type = Syntax.Property,
                        Key = key,
                        Value = ParsePropertyFunction(new List<Parameter>(), null),
                        Kind = "get"
                    };
                }
                else if (token.Value == "set" && !Match(":"))
                {
                    var key = ParseObjectPropertyKey();
                    Expect("(");
                    token = Lookahead();
                    if (token.Type != TokenType.Identifier)
                    {
                        ThrowUnexpected(Lex());
                    }
                    var param = new List<Parameter>() { ParseVariableIdentifier() };
                    Expect(")");
                    return new
                    {
                        Type = Syntax.Property,
                        Key = key,
                        Value = ParsePropertyFunction(param, token),
                        Kind = "set"
                    };
                }
                else
                {
                    Expect(":");
                    return new
                    {
                        Type = Syntax.Property,
                        Key = id,
                        Value = ParseAssignmentExpression(),
                        Kind = "init"
                    };
                }
            }
            else if (token.Type == TokenType.EOF || token.Type == TokenType.Punctuator)
            {
                ThrowUnexpected(token);
            }
            else
            {
                var key = ParseObjectPropertyKey();
                Expect(":");
                return new
                {
                    Type = Syntax.Property,
                    Key = key,
                    Value = ParseAssignmentExpression(),
                    Kind = "init"
                };
            }

            return null;
        }

        private dynamic ParseObjectInitialiser()
        {
            //var properties = [], property, name, kind, map = {}, toString = String;
            var name = "";
            var map = new Dictionary<string, PropertyKind>();
            var properties = new List<object>();

            Expect("{");

            while (!Match("}"))
            {
                var property = ParseObjectProperty();

                if (property.Key.Type == Syntax.Identifier)
                {
                    name = property.Key.Name;
                }
                else
                {
                    name = property.Key.Value.ToString();
                }
                var kind = (property.Kind == "init")
                               ? PropertyKind.Data
                               : (property.Kind == "get") ? PropertyKind.Get : PropertyKind.Set;
                //if (Object.prototype.hasOwnProperty.call(map, name)) {
                if (map.ContainsKey(name))
                {
                    if (map[name] == PropertyKind.Data)
                    {
                        if (_strict && kind == PropertyKind.Data)
                        {
                            ThrowErrorTolerant(null, Messages.StrictDuplicateProperty);
                        }
                        else if (kind != PropertyKind.Data)
                        {
                            ThrowError(null, Messages.AccessorDataProperty);
                        }
                    }
                    else
                    {
                        if (kind == PropertyKind.Data)
                        {
                            ThrowError(null, Messages.AccessorDataProperty);
                        }
                        else if (map.ContainsKey(name) & (kind == PropertyKind.Get || kind == PropertyKind.Set))
                        {
                            ThrowError(null, Messages.AccessorGetSet);
                        }
                    }
                    map[name] |= kind;
                }
                else
                {
                    map[name] = kind;
                }

                properties.Add(property);

                if (!Match("}"))
                {
                    Expect(",");
                }
            }

            Expect("}");

            return new
            {
                Type = Syntax.ObjectExpression,
                Properties = properties
            };
        }

        // 11.1 Primary Expressions

        private dynamic ParsePrimaryExpression()
        {
            var start = _index;// -_lineNumber +2;//xxx

            var token = Lookahead();
            var type = token.Type;

            if (type == TokenType.Identifier)
            {
                return new
                {
                    Type = Syntax.Identifier,
                    Name = Lex().Value
                };
            }

            if (type == TokenType.StringLiteral || type == TokenType.NumericLiteral)
            {
                if (_strict && token.Octal)
                {
                    ThrowErrorTolerant(token, Messages.StrictOctalLiteral);
                }
                return CreateLiteral(Lex());
            }

            if (type == TokenType.Keyword)
            {
                if (MatchKeyword("this"))
                {
                    Lex();

                    return new ThisExpression(_codeGeneration)
                               {
                                   Range = new Range { Start = token.Range.Start, End = token.Range.End },
                                   Loc =
                                       new Loc
                                       {
                                           Start = new Loc.Position { Line = token.Loc.Start.Line, Column = token.Loc.Start.Column },
                                           End = new Loc.Position { Line = token.Loc.End.Line, Column = token.Loc.End.Column }
                                       }
                               };
                }

                if (MatchKeyword("function"))
                {
                    return ParseFunctionExpression();
                }
            }

            if (type == TokenType.BooleanLiteral)
            {
                Lex();
                token.Value = (token.Value == "true").ToString();
                return CreateLiteral(token);
            }

            if (type == TokenType.NullLiteral)
            {
                Lex();
                token.Value = null;
                return CreateLiteral(token);
            }

            if (Match("["))
            {
                return ParseArrayInitialiser();
            }

            if (Match("{"))
            {
                return ParseObjectInitialiser();
            }

            if (Match("("))
            {
                Lex();
                dynamic expr = null;
                _state.LastParenthesized = expr = ParseExpression();
                Expect(")");
                return expr;
            }

            if (Match("/") || Match("/="))
            {
                return CreateLiteral(ScanRegExp());
            }

            ThrowUnexpected(Lex());

            return null;
        }

        // 11.2 Left-Hand-Side Expressions

        private List<object> ParseArguments()
        {
            var args = new List<object>();

            Expect("(");

            if (!Match(")"))
            {
                while (_index < _length)
                {
                    args.Add(ParseAssignmentExpression());
                    if (Match(")"))
                    {
                        break;
                    }
                    Expect(",");
                }
            }

            Expect(")");

            return args;
        }

        private dynamic ParseNonComputedProperty()
        {
            var token = Lex();

            if (!IsIdentifierName(token))
            {
                ThrowUnexpected(token);
            }

            return new Identifier(_codeGeneration)
            {
                Name = token.Value,
                Range = new Range { Start = token.Range.Start, End = token.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = token.Loc.Start.Line, Column = token.Loc.Start.Column },
                        End = new Loc.Position { Line = token.Loc.End.Line, Column = token.Loc.End.Column }
                    }
            };
        }

        private dynamic ParseNonComputedMember(dynamic obj)
        {
            return new
            {
                Type = Syntax.MemberExpression,
                Computed = false,
                Object = obj,
                Property = ParseNonComputedProperty()
            };
        }

        private dynamic ParseComputedMember(dynamic obj)
        {
            //var property, expr;

            Expect("[");
            var property = ParseExpression();
            var expr = new
            {
                Type = Syntax.MemberExpression,
                Computed = true,
                Object = obj,
                Property = property
            };
            Expect("]");
            return expr;
        }

        private dynamic ParseCallMember(dynamic obj)
        {
            return new
            {
                Type = Syntax.CallExpression,
                Callee = obj,
                Arguments = ParseArguments()
            };
        }

        private dynamic ParseNewExpression()
        {

            ExpectKeyword("new");

            var expr = new
            {
                Type = Syntax.NewExpression,
                Callee = ParseLeftHandSideExpression(),
                Arguments = new List<object>()
            };

            if (Match("("))
            {
                expr.Arguments.AddRange(ParseArguments());
            }

            return expr;
        }

        private dynamic ParseLeftHandSideExpressionAllowCall()
        {
            //var useNew, expr;

            var useNew = MatchKeyword("new");
            var expr = useNew ? ParseNewExpression() : ParsePrimaryExpression();

            while (_index < _length)
            {
                if (Match("."))
                {
                    Lex();
                    expr = ParseNonComputedMember(expr);
                }
                else if (Match("["))
                {
                    expr = ParseComputedMember(expr);
                }
                else if (Match("("))
                {
                    expr = ParseCallMember(expr);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private dynamic ParseLeftHandSideExpression()
        {
            //var useNew, expr;

            var useNew = MatchKeyword("new");
            var expr = useNew ? ParseNewExpression() : ParsePrimaryExpression();

            while (_index < _length)
            {
                if (Match("."))
                {
                    Lex();
                    expr = ParseNonComputedMember(expr);
                }
                else if (Match("["))
                {
                    expr = ParseComputedMember(expr);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        // 11.3 Postfix Expressions

        private dynamic ParsePostfixExpression()
        {
            var expr = ParseLeftHandSideExpressionAllowCall();

            if ((Match("++") || Match("--")) && !PeekLineTerminator())
            {
                // 11.3.1, 11.3.2
                if (_strict && expr.Type == Syntax.Identifier && IsRestrictedWord(expr.Name))
                {
                    ThrowError(null, Messages.StrictLHSPostfix);
                }

                if (!IsLeftHandSide(expr))
                {
                    ThrowError(null, Messages.InvalidLHSInAssignment);
                }

                expr = new
                {
                    Type = Syntax.UpdateExpression,
                    Operator = Lex().Value,
                    Argument = expr,
                    Prefix = false
                };
            }

            return expr;
        }

        // 11.4 Unary Operators

        private dynamic ParseUnaryExpression()
        {
            //var token, expr;

            if (Match("++") || Match("--"))
            {
                var token = Lex();
                var expr = ParseUnaryExpression();
                // 11.4.4, 11.4.5
                if (_strict && expr.Type == Syntax.Identifier && IsRestrictedWord(expr.Name))
                {
                    ThrowError(null, Messages.StrictLHSPrefix);
                }

                if (!IsLeftHandSide(expr))
                {
                    ThrowError(null, Messages.InvalidLHSInAssignment);
                }

                expr = new
                {
                    Type = Syntax.UpdateExpression,
                    Operator = token.Value,
                    Argument = expr,
                    Prefix = true
                };
                return expr;
            }

            if (Match('+') || Match('-') || Match('~') || Match('!'))
            {
                var expr = new
                {
                    Type = Syntax.UnaryExpression,
                    Operator = Lex().Value,
                    Argument = ParseUnaryExpression()
                };
                return expr;
            }

            if (MatchKeyword("delete") || MatchKeyword("void") || MatchKeyword("typeof"))
            {
                var expr = new
                {
                    Type = Syntax.UnaryExpression,
                    Operator = Lex().Value,
                    Argument = ParseUnaryExpression()
                };
                if (_strict && expr.Operator == "delete" && expr.Argument.Type == Syntax.Identifier)
                {
                    ThrowErrorTolerant(null, Messages.StrictDelete);
                }
                return expr;
            }

            return ParsePostfixExpression();
        }

        // 11.5 Multiplicative Operators

        private dynamic ParseMultiplicativeExpression()
        {
            var expr = ParseUnaryExpression();

            while (Match('*') || Match('/') || Match('%'))
            {
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = Lex().Value,
                    Left = expr,
                    Right = ParseUnaryExpression()
                };
            }

            return expr;
        }

        // 11.6 Additive Operators

        private dynamic ParseAdditiveExpression()
        {
            var expr = ParseMultiplicativeExpression();

            while (Match('+') || Match('-'))
            {
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = Lex().Value,
                    Left = expr,
                    Right = ParseMultiplicativeExpression()
                };
            }

            return expr;
        }

        // 11.7 Bitwise Shift Operators

        private dynamic ParseShiftExpression()
        {
            var expr = ParseAdditiveExpression();

            while (Match("<<") || Match(">>") || Match(">>>"))
            {
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = Lex().Value,
                    Left = expr,
                    Right = ParseAdditiveExpression()
                };
            }

            return expr;
        }

        // 11.8 Relational Operators

        private dynamic ParseRelationalExpression()
        {
            //var expr, previousAllowIn;

            var previousAllowIn = _state.AllowIn;
            _state.AllowIn = true;

            var expr = ParseShiftExpression();
            // PULAR ESPAÇOS!
            while (Match('<') || Match('>') || Match("<=") || Match(">=") || (previousAllowIn && MatchKeyword("in")) ||
                   MatchKeyword("instanceof"))
            {
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = Lex().Value,
                    Left = expr,
                    Right = ParseShiftExpression()
                };
            }

            _state.AllowIn = previousAllowIn;
            return expr;
        }

        // 11.9 Equality Operators

        private dynamic ParseEqualityExpression()
        {
            var expr = ParseRelationalExpression();

            while (Match("==") || Match("!=") || Match("===") || Match("!=="))
            {
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = Lex().Value,
                    Left = expr,
                    Right = ParseRelationalExpression()
                };
            }

            return expr;
        }

        // 11.10 Binary Bitwise Operators

        private dynamic ParseBitwiseANDExpression()
        {
            var expr = ParseEqualityExpression();

            while (Match('&'))
            {
                Lex();
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = "&",
                    Left = expr,
                    Right = ParseEqualityExpression()
                };
            }

            return expr;
        }

        private dynamic ParseBitwiseXORExpression()
        {
            var expr = ParseBitwiseANDExpression();

            while (Match('^'))
            {
                Lex();
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = "^",
                    Left = expr,
                    Right = ParseBitwiseANDExpression()
                };
            }

            return expr;
        }

        private dynamic ParseBitwiseORExpression()
        {
            var expr = ParseBitwiseXORExpression();

            while (Match('|'))
            {
                Lex();
                expr = new
                {
                    Type = Syntax.BinaryExpression,
                    Operator = "|",
                    Left = expr,
                    Right = ParseBitwiseXORExpression()
                };
            }

            return expr;
        }

        // 11.11 Binary Logical Operators

        private dynamic ParseLogicalANDExpression()
        {
            var expr = ParseBitwiseORExpression();

            while (Match("&&"))
            {
                Lex();
                expr = new
                {
                    Type = Syntax.LogicalExpression,
                    Operator = "&&",
                    Left = expr,
                    Right = ParseBitwiseORExpression()
                };
            }

            return expr;
        }

        private dynamic ParseLogicalORExpression()
        {
            var expr = ParseLogicalANDExpression();

            while (Match("||"))
            {
                Lex();
                expr = new
                {
                    Type = Syntax.LogicalExpression,
                    Operator = "||",
                    Left = expr,
                    Right = ParseLogicalANDExpression()
                };
            }

            return expr;
        }

        // 11.12 Conditional Operator

        private dynamic ParseConditionalExpression()
        {
            //var expr, previousAllowIn, consequent;

            var expr = ParseLogicalORExpression();

            if (Match('?'))
            {
                Lex();
                var previousAllowIn = _state.AllowIn;
                _state.AllowIn = true;
                var consequent = ParseAssignmentExpression();
                _state.AllowIn = previousAllowIn;
                Expect(':');

                expr = new
                {
                    Type = Syntax.ConditionalExpression,
                    Test = expr,
                    Consequent = consequent,
                    Alternate = ParseAssignmentExpression()
                };
            }

            return expr;
        }

        public dynamic ReinterpretAsAssignmentBindingPattern(dynamic expr)
        {
            var i = 0;
            var len = 0; //, property, element;
            dynamic property = null;
            dynamic element = null;

            if (expr.@sealed)
            {
                ThrowError(null, Messages.InvalidLHSInAssignment);
            }

            if (expr.Type == Syntax.ObjectExpression)
            {
                expr.Type = Syntax.ObjectPattern;
                for (i = 0, len = expr.properties.length; i < len; i += 1)
                {
                    property = expr.properties[i];
                    if (property.kind != "init")
                    {
                        ThrowError(null, Messages.InvalidLHSInAssignment);
                    }
                    ReinterpretAsAssignmentBindingPattern(property.value);
                }
            }
            else if (expr.Type == Syntax.ArrayExpression)
            {
                expr.Type = Syntax.ArrayPattern;
                for (i = 0, len = expr.elements.length; i < len; i += 1)
                {
                    element = expr.elements[i];
                    if (element)
                    {
                        ReinterpretAsAssignmentBindingPattern(element);
                    }
                }
            }
            else if (expr.Type == Syntax.Identifier)
            {
                if (expr.name == "super")
                {
                    ThrowError(null, Messages.InvalidLHSInAssignment);
                }
            }
            else
            {
                if (expr.Type != Syntax.MemberExpression && expr.Type != Syntax.CallExpression && expr.Type != Syntax.NewExpression)
                {
                    ThrowError(null, Messages.InvalidLHSInAssignment);
                }
            }

            return null;
        }

        // 11.13 Assignment Operators
        private dynamic ParseAssignmentExpression()
        {

            var expr = ParseConditionalExpression();

            if (MatchAssign())
            {
                // LeftHandSideExpression
                if (!IsLeftHandSide(expr))
                {
                    ThrowError(null, Messages.InvalidLHSInAssignment);
                }

                // 11.13.1
                if (_strict && expr.Type == Syntax.Identifier && IsRestrictedWord(expr.Name))
                {
                    ThrowError(null, Messages.StrictLHSAssignment);
                }

                // ES.next draf 11.13 Runtime Semantics step 1
                if (expr.Type == Syntax.ObjectExpression || expr.Type == Syntax.ArrayExpression)
                {
                    ReinterpretAsAssignmentBindingPattern(expr);
                }

                expr = new
                {
                    Type = Syntax.AssignmentExpression,
                    Operator = Lex().Value,
                    Left = expr,
                    Right = ParseAssignmentExpression()
                };
            }

            return expr;
        }

        // 11.14 Comma Operator

        private dynamic ParseExpression()
        {
            var expr = ParseAssignmentExpression();

            if (Match(','))
            {
                expr = new
                {
                    Type = Syntax.SequenceExpression,
                    Expressions = new List<object> { expr }
                };

                while (_index < _length)
                {
                    if (!Match(','))
                    {
                        break;
                    }
                    Lex();
                    expr.Expressions.Add(ParseAssignmentExpression());
                }

            }
            return expr;
        }

        // 12.1 Block

        private List<object> ParseStatementList()
        {
            var list = new List<object>();

            while (_index < _length)
            {
                if (Match('}'))
                {
                    break;
                }
                var statement = ParseSourceElement();
                if (statement == null)
                {
                    break;
                }
                list.Add(statement);
            }

            return list;
        }

        private dynamic ParseBlock()
        {

            Expect('{');

            var block = ParseStatementList();

            Expect('}');

            return new
            {
                Type = Syntax.BlockStatement,
                Body = block
            };
        }

        // 12.2 Variable Statement

        private dynamic ParseVariableIdentifier()
        {
            var token = Lex();

            if (token.Type != TokenType.Identifier)
            {
                ThrowUnexpected(token);
            }

            return new Identifier(_codeGeneration)
            {
                Name = token.Value,
                Range = new Range { Start = token.Range.Start, End = token.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = token.Loc.Start.Line, Column = token.Loc.Start.Column },
                        End = new Loc.Position { Line = token.Loc.End.Line, Column = token.Loc.End.Column }
                    }
            };
        }

        private dynamic ParseVariableDeclaration(string kind)
        {

            var id = ParseVariableIdentifier();
            dynamic init = null;

            var firstToken = _extra.Tokens[_extra.Tokens.Count - 1];

            // 12.2.1
            if (_strict && IsRestrictedWord(id.Name))
            {
                ThrowErrorTolerant(null, Messages.StrictVarName);
            }

            if (kind == "const")
            {
                Expect('=');
                init = ParseAssignmentExpression();
            }
            else if (Match('='))
            {
                Lex();
                init = ParseAssignmentExpression();
            }

            Token lastToken = null;

            if (init != null)
                lastToken = new Token
                                {
                                    Range = new Token.TokenRange
                                                {
                                                    Start = init.Range.Start,
                                                    End = init.Range.End
                                                },
                                    Loc = new Token.TokenLoc
                                              {
                                                  Start = new Token.TokenLoc.TokenPosition
                                                              {
                                                                  Line = init.Loc.Start.Line,
                                                                  Column = init.Loc.Start.Column
                                                              },
                                                  End = new Token.TokenLoc.TokenPosition
                                                            {
                                                                Line = init.Loc.End.Line,
                                                                Column = init.Loc.End.Column
                                                            }
                                              }
                                };
            else
                lastToken = firstToken;

            return new VariableDeclarator(_codeGeneration)
            {
                Id = id,
                Init = init,
                Range = new Range { Start = firstToken.Range.Start, End = lastToken.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = firstToken.Loc.Start.Line, Column = firstToken.Loc.Start.Column },
                        End = new Loc.Position { Line = lastToken.Loc.End.Line, Column = lastToken.Loc.End.Column }
                    }
            };
        }

        private List<object> ParseVariableDeclarationList(string kind)
        {
            var list = new List<object>();

            while (_index < _length)
            {
                list.Add(ParseVariableDeclaration(kind));
                if (!Match(','))
                {
                    break;
                }
                Lex();
            }

            return list;
        }

        private dynamic ParseVariableStatement()
        {

            ExpectKeyword("var");

            var firstToken = _extra.Tokens[_extra.Tokens.Count - 1];

            var declarations = ParseVariableDeclarationList(null);

            var lastToken = _extra.Tokens[_extra.Tokens.Count - 1];

            ConsumeSemicolon();

            return new VariableDeclaration(_codeGeneration)
            {
                //Type = Syntax.VariableDeclaration,
                Declarations = declarations,
                Kind = "var",
                Range = new Range { Start = firstToken.Range.Start, End = lastToken.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = firstToken.Loc.Start.Line, Column = firstToken.Loc.Start.Column },
                        End = new Loc.Position { Line = lastToken.Loc.End.Line, Column = lastToken.Loc.End.Column }
                    }
            };
        }

        // kind may be `const` or `let`
        // Both are experimental and not in the specification yet.
        // see http://wiki.ecmascript.org/doku.php?id=harmony:const
        // and http://wiki.ecmascript.org/doku.php?id=harmony:let
        private dynamic ParseConstLetDeclaration(string kind)
        {
            ExpectKeyword(kind);

            var declarations = ParseVariableDeclarationList(kind);

            ConsumeSemicolon();

            return new
            {
                Type = Syntax.VariableDeclaration,
                Declarations = declarations,
                Kind = kind
            };
        }

        // 12.3 Empty Statement

        private dynamic ParseEmptyStatement()
        {
            Expect(';');

            return new
            {
                Type = Syntax.EmptyStatement
            };
        }

        // 12.4 Expression Statement

        private dynamic ParseExpressionStatement()
        {
            var firstToken = _extra.Tokens[_extra.Tokens.Count - 1];

            var expr = ParseExpression();

            var lastToken = _extra.Tokens[_extra.Tokens.Count - 1];

            ConsumeSemicolon();

            return new ExpressionStatement(_codeGeneration)
            {
                Expression = expr,
                Range = new Range { Start = firstToken.Range.Start, End = lastToken.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = firstToken.Loc.Start.Line, Column = firstToken.Loc.Start.Column },
                        End = new Loc.Position { Line = lastToken.Loc.End.Line, Column = lastToken.Loc.End.Column }
                    }
            };
        }

        // 12.5 If statement

        private dynamic ParseIfStatement()
        {
            //var test, consequent, alternate;
            dynamic alternate = null;

            ExpectKeyword("if");

            Expect('(');

            var test = ParseExpression();

            Expect(')');

            var consequent = ParseStatement();

            if (MatchKeyword("else"))
            {
                Lex();
                alternate = ParseStatement();
            }
            else
            {
                alternate = null;
            }

            return new
            {
                Type = Syntax.IfStatement,
                Test = test,
                Consequent = consequent,
                Alternate = alternate
            };
        }

        // 12.6 Iteration Statements

        private dynamic ParseDoWhileStatement()
        {
            //var body, test, oldInIteration;

            ExpectKeyword("do");

            var oldInIteration = _state.InIteration;
            _state.InIteration = true;

            var body = ParseStatement();

            _state.InIteration = oldInIteration;

            ExpectKeyword("while");

            Expect('(');

            var test = ParseExpression();

            Expect(')');

            if (Match(';'))
            {
                Lex();
            }

            return new
            {
                Type = Syntax.DoWhileStatement,
                Body = body,
                Test = test
            };
        }

        private dynamic ParseWhileStatement()
        {
            //var test, body, oldInIteration;

            ExpectKeyword("while");

            Expect('(');

            var test = ParseExpression();

            Expect(')');

            var oldInIteration = _state.InIteration;
            _state.InIteration = true;

            var body = ParseStatement();

            _state.InIteration = oldInIteration;

            return new
            {
                Type = Syntax.WhileStatement,
                Test = test,
                Body = body
            };
        }

        private dynamic ParseForVariableDeclaration()
        {
            var token = Lex();

            return new
            {
                Type = Syntax.VariableDeclaration,
                Declarations = ParseVariableDeclarationList(null),
                Kind = token.Value
            };
        }

        private dynamic ParseForStatement()
        {
            //var init, test, update, left, right, body, oldInIteration;

            //init = test = update = null;
            dynamic init = null;
            dynamic left = null;
            dynamic right = null;
            dynamic test = null;
            dynamic update = null;
            bool each = false;

            ExpectKeyword("for");

            // http://wiki.ecmascript.org/doku.php?id=proposals:iterators_and_generators&s=each
            if (MatchContextualKeyword("each"))
            {
                Lex();
                each = true;
            }
            else
            {
                each = false;
            }

            Expect('(');

            if (Match(';'))
            {
                Lex();
            }
            else
            {
                if (MatchKeyword("var") || MatchKeyword("let"))
                {
                    _state.AllowIn = false;
                    init = ParseForVariableDeclaration();
                    _state.AllowIn = true;

                    if (init.Declarations.Count == 1 && MatchKeyword("in"))
                    {
                        Lex();
                        left = init;
                        right = ParseExpression();
                        init = null;
                    }
                }
                else
                {
                    _state.AllowIn = false;
                    init = ParseExpression();
                    _state.AllowIn = true;

                    if (MatchKeyword("in"))
                    {
                        // LeftHandSideExpression
                        if (!IsLeftHandSide(init))
                        {
                            ThrowError(null, Messages.InvalidLHSInForIn);
                        }

                        Lex();
                        left = init;
                        right = ParseExpression();
                        init = null;
                    }
                }

                if (left == null)
                {
                    Expect(';');
                }
            }

            if (left == null)
            {

                if (!Match(';'))
                {
                    test = ParseExpression();
                }
                Expect(';');

                if (!Match(')'))
                {
                    update = ParseExpression();
                }
            }

            Expect(')');

            var oldInIteration = _state.InIteration;
            _state.InIteration = true;

            var body = ParseStatement();

            _state.InIteration = oldInIteration;

            if (left == null)
            {
                return new
                {
                    Type = Syntax.ForStatement,
                    Init = init,
                    Test = test,
                    Update = update,
                    Body = body
                };
            }

            return new
            {
                Type = Syntax.ForInStatement,
                Left = left,
                Right = right,
                Body = body,
                Each = each
            };
        }

        // 12.7 The continue statement

        private dynamic ParseContinueStatement()
        {
            //var token, label = null;
            dynamic label = null;

            ExpectKeyword("continue");

            // Optimize the most common form: 'continue;'.
            if (_source[_index] == ';')
            {
                Lex();

                if (!_state.InIteration)
                {
                    ThrowError(null, Messages.IllegalContinue);
                }

                return new
                {
                    Type = Syntax.ContinueStatement,
                    //Label = null
                };
            }

            if (PeekLineTerminator())
            {
                if (!_state.InIteration)
                {
                    ThrowError(null, Messages.IllegalContinue);
                }

                return new
                {
                    Type = Syntax.ContinueStatement,
                    //Label = null
                };
            }

            var token = Lookahead();
            if (token.Type == TokenType.Identifier)
            {
                label = ParseVariableIdentifier();

                //if (!Object.prototype.hasOwnProperty.call(state.labelSet, label.name)) {
                if (!_state.LabelSet.ContainsKey(label.Name))
                {
                    ThrowError(null, string.Format(Messages.UnknownLabel, label.Name));
                }
            }

            ConsumeSemicolon();

            if (label == null && !_state.InIteration)
            {
                ThrowError(null, Messages.IllegalContinue);
            }

            return new
            {
                Type = Syntax.ContinueStatement,
                Label = label
            };
        }


        // 12.8 The break statement

        private dynamic ParseBreakStatement()
        {
            //var token, label = null;
            dynamic label = null;

            ExpectKeyword("break");

            // Optimize the most common form: 'break;'.
            if (_source[_index] == ';')
            {
                Lex();

                if (!(_state.InIteration || _state.InSwitch))
                {
                    ThrowError(null, Messages.IllegalBreak);
                }

                return new
                {
                    Type = Syntax.BreakStatement,
                    //Label = null
                };
            }

            if (PeekLineTerminator())
            {
                if (!(_state.InIteration || _state.InSwitch))
                {
                    ThrowError(null, Messages.IllegalBreak);
                }

                return new
                {
                    Type = Syntax.BreakStatement,
                    //Label = null
                };
            }

            var token = Lookahead();
            if (token.Type == TokenType.Identifier)
            {
                label = ParseVariableIdentifier();

                //if (!Object.prototype.hasOwnProperty.call(state.labelSet, label.name)) {
                if (!_state.LabelSet.ContainsKey(label.Name))
                {
                    ThrowError(null, string.Format(Messages.UnknownLabel, label.Name));
                }
            }

            ConsumeSemicolon();

            if (label == null && !(_state.InIteration || _state.InSwitch))
            {
                ThrowError(null, Messages.IllegalBreak);
            }

            return new
            {
                Type = Syntax.BreakStatement,
                Label = label
            };
        }

        // 12.9 The return statement

        private dynamic ParseReturnStatement()
        {
            //var token, argument = null;
            dynamic argument = null;

            ExpectKeyword("return");

            if (!_state.InFunctionBody)
            {
                ThrowErrorTolerant(null, Messages.IllegalReturn);
            }

            // 'return' followed by a space and an identifier is very common.
            if (_source[_index] == ' ')
            {
                if (IsIdentifierStart(_source[_index + 1]))
                {
                    argument = ParseExpression();
                    ConsumeSemicolon();
                    return new
                    {
                        Type = Syntax.ReturnStatement,
                        Argument = argument
                    };
                }
            }

            if (PeekLineTerminator())
            {
                return new
                {
                    Type = Syntax.ReturnStatement,
                    //Argument = null
                };
            }

            if (!Match(';'))
            {
                var token = Lookahead();

                if (!Match('}') && token.Type != TokenType.EOF)
                {
                    argument = ParseExpression();
                }

                if (argument == null)
                {
                    argument = token;
                }
            }

            ConsumeSemicolon();

            return new
            {
                Type = Syntax.ReturnStatement,
                Argument = argument
            };
        }

        // 12.10 The with statement

        private dynamic ParseWithStatement()
        {
            //var object, body;
            dynamic obj = null;

            //if (_strict != null) {
            //    ThrowErrorTolerant({}, Messages.StrictModeWith);
            //}

            ExpectKeyword("with");

            Expect('(');

            obj = ParseExpression();

            Expect(')');

            var body = ParseStatement();

            return new
            {
                Type = Syntax.WithStatement,
                Object = obj,
                Body = body
            };
        }

        // 12.10 The swith statement

        private dynamic ParseSwitchCase()
        {
            //var test,
            //    consequent = [],
            //    statement;
            dynamic test = null;
            dynamic statement = null;
            var consequent = new List<object>();

            if (MatchKeyword("default"))
            {
                Lex();
                test = null;
            }
            else
            {
                ExpectKeyword("case");
                test = ParseExpression();
            }
            Expect(':');

            while (_index < _length)
            {
                if (Match('}') || MatchKeyword("default") || MatchKeyword("case"))
                {
                    break;
                }
                statement = ParseStatement();
                if (statement != null)
                {
                    break;
                }
                consequent.Add(statement);
            }

            return new
            {
                Type = Syntax.SwitchCase,
                Test = test,
                Consequent = consequent
            };
        }

        private dynamic ParseSwitchStatement()
        {
            //var discriminant, cases, clause, oldInSwitch, defaultFound;

            ExpectKeyword("switch");

            Expect('(');

            var discriminant = ParseExpression();

            Expect(')');

            Expect('{');

            if (Match('}'))
            {
                Lex();
                return new
                {
                    Type = Syntax.SwitchStatement,
                    Discriminant = discriminant
                };
            }

            var cases = new List<object>();

            var oldInSwitch = _state.InSwitch;
            _state.InSwitch = true;
            var defaultFound = false;

            while (_index < _length)
            {
                if (Match('}'))
                {
                    break;
                }
                var clause = ParseSwitchCase();
                if (clause.Test == null)
                {
                    if (defaultFound)
                    {
                        ThrowError(null, Messages.MultipleDefaultsInSwitch);
                    }
                    defaultFound = true;
                }
                cases.Add(clause);
            }

            _state.InSwitch = oldInSwitch;

            Expect('}');

            return new
            {
                Type = Syntax.SwitchStatement,
                Discriminant = discriminant,
                Cases = cases
            };
        }

        // 12.13 The throw statement

        private dynamic ParseThrowStatement()
        {
            //var argument;

            ExpectKeyword("throw");

            if (PeekLineTerminator())
            {
                ThrowError(null, Messages.NewlineAfterThrow);
            }

            var argument = ParseExpression();

            ConsumeSemicolon();

            return new
            {
                Type = Syntax.ThrowStatement,
                Argument = argument
            };
        }

        // 12.14 The try statement

        private dynamic ParseCatchClause()
        {
            dynamic param = null;

            ExpectKeyword("catch");

            Expect('(');
            if (!Match(')'))
            {
                param = ParseExpression();
                // 12.14.1
                if (_strict && param.Type == Syntax.Identifier && IsRestrictedWord(param.Name))
                {
                    ThrowErrorTolerant(null, Messages.StrictCatchVariable);
                }
            }
            Expect(')');

            return new
            {
                Type = Syntax.CatchClause,
                Param = param,
                Body = ParseBlock()
            };
        }

        private dynamic ParseTryStatement()
        {
            //var block, handlers = [], finalizer = null;
            var handlers = new List<object>();
            dynamic finalizer = null;

            ExpectKeyword("try");

            var block = ParseBlock();

            if (MatchKeyword("catch"))
            {
                handlers.Add(ParseCatchClause());
            }

            if (MatchKeyword("finally"))
            {
                Lex();
                finalizer = ParseBlock();
            }

            if (handlers.Count == 0 && finalizer == null)
            {
                ThrowError(null, Messages.NoCatchOrFinally);
            }

            return new
            {
                Type = Syntax.TryStatement,
                Block = block,
                GuardedHandlers = new List<object>(),
                Handlers = handlers,
                Finalizer = finalizer
            };
        }

        // 12.15 The debugger statement

        private dynamic ParseDebuggerStatement()
        {
            ExpectKeyword("debugger");

            ConsumeSemicolon();

            return new
            {
                Type = Syntax.DebuggerStatement
            };
        }

        // 12 Statements

        private dynamic ParseStatement()
        {
            var token = Lookahead();
            dynamic expr = null;
            dynamic labeledBody = null;

            if (token.Type == TokenType.EOF)
            {
                ThrowUnexpected(token);
            }

            if (token.Type == TokenType.Punctuator)
            {
                switch (token.Value)
                {
                    case ";":
                        return ParseEmptyStatement();
                    case "{":
                        return ParseBlock();
                    case "(":
                        return ParseExpressionStatement();
                    default:
                        break;
                }
            }

            if (token.Type == TokenType.Keyword)
            {
                switch (token.Value)
                {
                    case "break":
                        return ParseBreakStatement();
                    case "continue":
                        return ParseContinueStatement();
                    case "debugger":
                        return ParseDebuggerStatement();
                    case "do":
                        return ParseDoWhileStatement();
                    case "for":
                        return ParseForStatement();
                    case "function":
                        return ParseFunctionDeclaration();
                    case "if":
                        return ParseIfStatement();
                    case "return":
                        return ParseReturnStatement();
                    case "switch":
                        return ParseSwitchStatement();
                    case "throw":
                        return ParseThrowStatement();
                    case "try":
                        return ParseTryStatement();
                    case "var":
                        return ParseVariableStatement();
                    case "while":
                        return ParseWhileStatement();
                    case "with":
                        return ParseWithStatement();
                    default:
                        break;
                }
            }

            var start = _index;
            var line = _lineNumber;
            var col = _index - _lineStart;

            expr = ParseExpression();

            var endRange = _index;

            // 12.12 Labelled Statements
            if ((expr is ISyntax && expr is Identifier) && Match(':'))
            {
                Lex();

                //if (Object.prototype.hasOwnProperty.call(state.labelSet, expr.name)) {
                if (_state.LabelSet.ContainsKey(expr.Name))
                {
                    ThrowError(null, string.Format(Messages.Redeclaration, "Label", expr.Name));
                }

                _state.LabelSet[expr.Name] = true;
                labeledBody = ParseStatement();
                _state.LabelSet.Remove(expr.Name);

                return new
                {
                    Type = Syntax.LabeledStatement,
                    Label = expr,
                    Body = labeledBody
                };
            }

            var lineEnd = _lineNumber;
            var colEnd = _index - _lineStart;

            ConsumeSemicolon();

            return new ExpressionStatement(_codeGeneration)
            {
                Expression = expr,
                Range = new Range { Start = start, End = endRange },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = line, Column = col },
                        End = new Loc.Position { Line = lineEnd, Column = colEnd }
                    }
            };
        }

        // 13 Function Definition

        private dynamic ParseFunctionSourceElements()
        {
            //var sourceElement, sourceElements = [], token, directive, firstRestricted,
            //    oldLabelSet, oldInIteration, oldInSwitch, oldInFunctionBody;
            Token token = null;
            Token firstRestricted = null;
            var sourceElements = new List<object>();

            Expect('{');

            while (_index < _length)
            {
                token = Lookahead();
                if (token.Type != TokenType.StringLiteral)
                {
                    break;
                }

                var sourceElement = ParseSourceElement();
                sourceElements.Add(sourceElement);
                if (sourceElement.Expression.Type != Syntax.Literal)
                {
                    // this is not directive
                    break;
                }
                var directive = SliceSource(token.Range.Start + 1, token.Range.End - 1);
                if (new String(directive.ToArray()) == "use strict")
                {
                    _strict = true;
                    if (firstRestricted != null)
                    {
                        ThrowError(firstRestricted, Messages.StrictOctalLiteral);
                    }
                }
                else
                {
                    if (firstRestricted == null && token.Octal)
                    {
                        firstRestricted = token;
                    }
                }
            }

            var oldLabelSet = _state.LabelSet;
            var oldInIteration = _state.InIteration;
            var oldInSwitch = _state.InSwitch;
            var oldInFunctionBody = _state.InFunctionBody;

            _state.LabelSet = new Dictionary<string, object>();
            _state.InIteration = false;
            _state.InSwitch = false;
            _state.InFunctionBody = true;

            while (_index < _length)
            {
                if (Match('}'))
                {
                    break;
                }
                var sourceElement = ParseSourceElement();
                if (sourceElement == null)
                {
                    break;
                }
                sourceElements.Add(sourceElement);
            }

            Expect('}');

            _state.LabelSet = oldLabelSet;
            _state.InIteration = oldInIteration;
            _state.InSwitch = oldInSwitch;
            _state.InFunctionBody = oldInFunctionBody;

            return new
            {
                Type = Syntax.BlockStatement,
                Body = sourceElements
            };
        }

        private dynamic ParseFunctionDeclaration()
        {
            //var id, param, params = [], body, token, firstRestricted, message, previousStrict, paramSet;
            Token firstRestricted = null;
            bool previousStrict;
            string message = null;
            var paramSet = new Dictionary<string, object>();
            Token token = null;
            var @params = new List<object>();

            ExpectKeyword("function");
            token = Lookahead();
            var id = ParseVariableIdentifier();
            if (_strict)
            {
                if (IsRestrictedWord(token.Value))
                {
                    ThrowError(token, Messages.StrictFunctionName);
                }
            }
            else
            {
                if (IsRestrictedWord(token.Value))
                {
                    firstRestricted = token;
                    message = Messages.StrictFunctionName;
                }
                else if (IsStrictModeReservedWord(token.Value))
                {
                    firstRestricted = token;
                    message = Messages.StrictReservedWord;
                }
            }

            Expect('(');

            if (!Match(')'))
            {
                while (_index < _length)
                {
                    token = Lookahead();
                    var param = ParseVariableIdentifier();
                    if (_strict)
                    {
                        if (IsRestrictedWord(token.Value))
                        {
                            ThrowError(token, Messages.StrictParamName);
                        }
                        //if (Object.prototype.hasOwnProperty.call(paramSet, token.value)) {
                        if (paramSet.ContainsKey(token.Value))
                        {
                            ThrowError(token, Messages.StrictParamDupe);
                        }
                    }
                    else if (firstRestricted == null)
                    {
                        if (IsRestrictedWord(token.Value))
                        {
                            firstRestricted = token;
                            message = Messages.StrictParamName;
                        }
                        else if (IsStrictModeReservedWord(token.Value))
                        {
                            firstRestricted = token;
                            message = Messages.StrictReservedWord;
                            //} else if (Object.prototype.hasOwnProperty.call(paramSet, token.value)) {
                        }
                        else if (paramSet.ContainsKey(token.Value))
                        {
                            firstRestricted = token;
                            message = Messages.StrictParamDupe;
                        }
                    }
                    @params.Add(param);
                    paramSet[param.Name] = true;
                    if (Match(')'))
                    {
                        break;
                    }
                    Expect(',');
                }
            }

            Expect(')');

            previousStrict = _strict;
            var body = ParseFunctionSourceElements();
            if (_strict && firstRestricted != null)
            {
                ThrowError(firstRestricted, message);
            }
            _strict = previousStrict;

            return new
            {
                Type = Syntax.FunctionDeclaration,
                Id = id,
                Params = @params,
                Defaults = new List<object>(),
                Body = body,
                //Rest = null,
                Generator = false,
                Expression = false
            };
        }

        private dynamic ParseFunctionExpression()
        {
            //var token, id = null, firstRestricted, message, param, params = [], body, previousStrict, paramSet;
            Token token = null;
            dynamic id = null;
            Token firstRestricted = null;
            string message = null;
            var paramSet = new Dictionary<string, object>();
            dynamic param = null;
            var @params = new List<object>();


            ExpectKeyword("function");

            if (!Match('('))
            {
                token = Lookahead();
                id = ParseVariableIdentifier();
                if (_strict)
                {
                    if (IsRestrictedWord(token.Value))
                    {
                        ThrowError(token, Messages.StrictFunctionName);
                    }
                }
                else
                {
                    if (IsRestrictedWord(token.Value))
                    {
                        firstRestricted = token;
                        message = Messages.StrictFunctionName;
                    }
                    else if (IsStrictModeReservedWord(token.Value))
                    {
                        firstRestricted = token;
                        message = Messages.StrictReservedWord;
                    }
                }
            }

            Expect('(');

            if (!Match(')'))
            {
                while (_index < _length)
                {
                    token = Lookahead();
                    param = ParseVariableIdentifier();
                    if (_strict)
                    {
                        if (IsRestrictedWord(token.Value))
                        {
                            ThrowError(token, Messages.StrictParamName);
                        }
                        //if (Object.prototype.hasOwnProperty.call(paramSet, token.value)) {
                        if (paramSet.ContainsKey(token.Value))
                        {
                            ThrowError(token, Messages.StrictParamDupe);
                        }
                    }
                    else if (firstRestricted == null)
                    {
                        if (IsRestrictedWord(token.Value))
                        {
                            firstRestricted = token;
                            message = Messages.StrictParamName;
                        }
                        else if (IsStrictModeReservedWord(token.Value))
                        {
                            firstRestricted = token;
                            message = Messages.StrictReservedWord;
                            //} else if (Object.prototype.hasOwnProperty.call(paramSet, token.value)) {
                        }
                        else if (paramSet.ContainsKey(token.Value))
                        {
                            firstRestricted = token;
                            message = Messages.StrictParamDupe;
                        }
                    }
                    @params.Add(param);
                    paramSet[param.Name] = true;
                    if (Match(')'))
                    {
                        break;
                    }
                    Expect(',');
                }
            }

            Expect(')');

            var previousStrict = _strict;
            var body = ParseFunctionSourceElements();
            if (_strict && firstRestricted != null)
            {
                ThrowError(firstRestricted, message);
            }
            _strict = previousStrict;

            return new
            {
                Type = Syntax.FunctionExpression,
                Id = id,
                Params = @params,
                //Defaults  = new {},
                Body = body,
                //Rest = null,
                Generator = false,
                Expression = false
            };
        }

        // 14 Program

        private dynamic ParseSourceElement()
        {
            var token = Lookahead();

            if (token.Type == TokenType.Keyword)
            {
                switch (token.Value)
                {
                    case "const":
                    case "let":
                        return ParseConstLetDeclaration(token.Value);
                    case "function":
                        return ParseFunctionDeclaration();
                    default:
                        return ParseStatement();
                }
            }

            if (token.Type != TokenType.EOF)
            {
                return ParseStatement();
            }

            return null;
        }

        private dynamic ParseSourceElements()
        {
            //var sourceElement, sourceElements = [], token, directive, firstRestricted;
            Token token = null;
            dynamic sourceElement = null;
            var sourceElements = new List<object>();
            Token firstRestricted = null;

            while (_index < _length)
            {
                token = Lookahead();
                if (token.Type != TokenType.StringLiteral)
                {
                    break;
                }

                sourceElement = ParseSourceElement();
                sourceElements.Add(sourceElement);
                if (sourceElement.Expression.Type != Syntax.Literal)
                {
                    // this is not directive
                    break;
                }
                var directive = SliceSource(token.Range.Start + 1, token.Range.End - 1);
                if (new string(directive.ToArray()) == "use strict")
                {
                    _strict = true;
                    if (firstRestricted != null)
                    {
                        ThrowError(firstRestricted, Messages.StrictOctalLiteral);
                    }
                }
                else
                {
                    if (firstRestricted == null && token.Octal)
                    {
                        firstRestricted = token;
                    }
                }
            }

            while (_index < _length)
            {
                sourceElement = ParseSourceElement();
                if (sourceElement == null)
                {
                    break;
                }
                sourceElements.Add(sourceElement);
            }
            return sourceElements;
        }

        private Program ParseProgram()
        {
            _strict = false;
            _yieldAllowed = false;
            _yieldFound = false;
            var program = new Program(_codeGeneration)
                              {
                                  Extra = _extra,
                                  Body = ParseSourceElements(),
                                  Range = new Range {Start = 0, End = _index},
                                  Loc =
                                      new Loc
                                          {
                                              Start = new Loc.Position {Line = 1, Column = 0},
                                              End = new Loc.Position { Line = _lineNumber, Column = _length - _lineStart }
                                          }
                              };
            return program;
        }

        // The following functions are needed only when the option to preserve
        // the comments is active.

        private void AddComment(object type, object value, int start, int end, object loc)
        {
            //Assert(start >= 0, "Comment must have valid position");

            //// Because the way the actual token is scanned, often the comments
            //// (if any) are skipped twice during the lexical analysis.
            //// Thus, we need to skip adding a comment if the comment array already
            //// handled it.
            //if (_extra.Comments.Count > 0)
            //{
            //    if (_extra.Comments[_extra.Comments.Count - 1].Range.End > start)
            //    {
            //        return;
            //    }
            //}

            //_extra.Comments.Add(
            //{
            //    type = type,
            //    value = value,
            //    range = [
            //    start,
            //    end],
            //    loc = loc
            //});
        }

        private dynamic ScanComment()
        {
            ////var comment, ch, loc, start, blockComment, lineComment;
            //Loc loc = null;
            //int start;

            //var comment = "";
            //var blockComment = false;
            //var lineComment = false;

            //while (_index < _length) {
            //    var ch = _source[_index];

            //    if (lineComment) {
            //        ch = NextChar();
            //        if (IsLineTerminator(ch)) {
            //            loc.End = new Pos{
            //                Line= _lineNumber,
            //                Column= _index - _lineStart - 1
            //            };
            //            lineComment = false;
            //            AddComment("Line", comment, start, _index - 1, loc);
            //            if (ch === '\r' && _source[_index] == '\n') {
            //                ++_index;
            //            }
            //            ++_lineNumber;
            //            _lineStart = _index;
            //            comment = '';
            //        } else if (index >= length) {
            //            lineComment = false;
            //            comment += ch;
            //            loc.End = new Pos{
            //                Line= _lineNumber,
            //                Column= _length - _lineStart
            //            };
            //            AddComment("Line", comment, start, _length, loc);
            //        } else {
            //            comment += ch;
            //        }
            //    } else if (blockComment) {
            //        if (IsLineTerminator(ch)) {
            //            if (ch == '\r' && _source[_index + 1] == '\n') {
            //                ++_index;
            //                comment += "\r\n";
            //            } else {
            //                comment += ch;
            //            }
            //            ++_lineNumber;
            //            ++_index;
            //            _lineStart = _index;
            //            if (_index >= _length) {
            //                ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
            //            }
            //        } else {
            //            ch = NextChar();
            //            if (_index >= _length) {
            //                ThrowError(null, string.Format(Messages.UnexpectedToken, "ILLEGAL"));
            //            }
            //            comment += ch;
            //            if (ch == '*') {
            //                ch = _source[_index];
            //                if (ch == '/') {
            //                    comment = comment.Substring(0, comment.Length - 1);
            //                    blockComment = false;
            //                    ++_index;
            //                    loc.End = new Pos{
            //                        Line= _lineNumber,
            //                        Column= _index - _lineStart
            //                    };
            //                    AddComment("Block", comment, start, _index, loc);
            //                    comment = "";
            //                }
            //            }
            //        }
            //    } else if (ch == '/') {
            //        ch = _source[_index + 1];
            //        if (ch == '/') {
            //            loc = new Loc{
            //                start: {
            //                    line: lineNumber,
            //                    column: index - lineStart
            //                }
            //            };
            //            start = index;
            //            index += 2;
            //            lineComment = true;
            //            if (index >= length) {
            //                loc.end = {
            //                    line: lineNumber,
            //                    column: index - lineStart
            //                };
            //                lineComment = false;
            //                addComment('Line', comment, start, index, loc);
            //            }
            //        } else if (ch === '*') {
            //            start = index;
            //            index += 2;
            //            blockComment = true;
            //            loc = {
            //                start: {
            //                    line: lineNumber,
            //                    column: index - lineStart - 2
            //                }
            //            };
            //            if (index >= length) {
            //                throwError({}, Messages.UnexpectedToken, 'ILLEGAL');
            //            }
            //        } else {
            //            break;
            //        }
            //    } else if (isWhiteSpace(ch)) {
            //        ++index;
            //    } else if (isLineTerminator(ch)) {
            //        ++index;
            //        if (ch === '\r' && source[index] === '\n') {
            //            ++index;
            //        }
            //        ++lineNumber;
            //        lineStart = index;
            //    } else {
            //        break;
            //    }
            //}
            return null;
        }

        private void FilterCommentLocation()
        {
            //var i, entry, comment, comments = [];

            //for (i = 0; i < extra.comments.length; ++i) {
            //    entry = extra.comments[i];
            //    comment = {
            //        type: entry.type,
            //        value: entry.value
            //    };
            //    if (extra.range) {
            //        comment.range = entry.range;
            //    }
            //    if (extra.loc) {
            //        comment.loc = entry.loc;
            //    }
            //    comments.push(comment);
            //}

            //extra.comments = comments;
        }

        private Token CollectToken()
        {
            SkipComment();
            var loc = new Token.TokenLoc
                          {
                              Start = new Token.TokenLoc.TokenPosition
                                          {
                                              Line = _lineNumber,
                                              Column = _index - _lineStart
                                          }
                          };

            //extra.advance
            var token = _Advance();
            loc.End = new Token.TokenLoc.TokenPosition
                          {
                              Line = _lineNumber,
                              Column = _index - _lineStart
                          };

            if (token.Type != TokenType.EOF)
            {
                _extra.Tokens.Add(new Token
                                      {
                                          Range = new Token.TokenRange
                                                      {
                                                          Start = token.Range.Start,
                                                          End = token.Range.End,
                                                      },
                                          Value = string.Join("", SliceSource(token.Range.Start, token.Range.End)),
                                          Loc = loc
                                      });
            }

            token.Loc = loc;

            return token;
        }

        private object CollectRegex()
        {
            //var pos, loc, regex, token;

            //skipComment();

            //pos = index;
            //loc = {
            //    start: {
            //        line: lineNumber,
            //        column: index - lineStart
            //    }
            //};

            //regex = extra.scanRegExp();
            //loc.end = {
            //    line: lineNumber,
            //    column: index - lineStart
            //};

            //// Pop the previous token, which is likely '/' or '/='
            //if (extra.tokens.length > 0) {
            //    token = extra.tokens[extra.tokens.length - 1];
            //    if (token.range[0] === pos && token.type === 'Punctuator') {
            //        if (token.value === '/' || token.value === '/=') {
            //            extra.tokens.pop();
            //        }
            //    }
            //}

            //extra.tokens.push({
            //    type: 'RegularExpression',
            //    value: regex.literal,
            //    range: [pos, index],
            //    loc: loc
            //});

            //return regex;

            return null;
        }

        private void FilterTokenLocation()
        {
            //var i, entry, token, tokens = [];

            //for (i = 0; i < extra.tokens.length; ++i) {
            //    entry = extra.tokens[i];
            //    token = {
            //        type: entry.type,
            //        value: entry.value
            //    };
            //    if (extra.range) {
            //        token.range = entry.range;
            //    }
            //    if (extra.loc) {
            //        token.loc = entry.loc;
            //    }
            //    tokens.push(token);
            //}

            //extra.tokens = tokens;
        }

        private dynamic CreateLiteral(Token token)
        {
            return new Literal(_codeGeneration)
            {
                IsString = token.Type == TokenType.StringLiteral,
                Value = token.Value,
                Range = new Range { Start = token.Range.Start, End = token.Range.End },
                Loc =
                    new Loc
                    {
                        Start = new Loc.Position { Line = token.Loc.Start.Line, Column = token.Loc.Start.Column },
                        End = new Loc.Position { Line = token.Loc.End.Line, Column = token.Loc.End.Column }
                    }
            };
        }

        private object CreateRawLiteral(Token token)
        {
            //return new Token {
            //    Type= Syntax.Literal,
            //    Value: token.value,
            //    Raw: sliceSource(token.range[0], token.range[1])
            //};
            return null;
        }

        private object WrapTrackingFunction(object range, object loc)
        {

            //return function (parseFunction) {

            //    function isBinary(node) {
            //        return node.type === Syntax.LogicalExpression ||
            //            node.type === Syntax.BinaryExpression;
            //    }

            //    function visit(node) {
            //        if (isBinary(node.left)) {
            //            visit(node.left);
            //        }
            //        if (isBinary(node.right)) {
            //            visit(node.right);
            //        }

            //        if (range && typeof node.range === 'undefined') {
            //            node.range = [node.left.range[0], node.right.range[1]];
            //        }
            //        if (loc && typeof node.loc === 'undefined') {
            //            node.loc = {
            //                start: node.left.loc.start,
            //                end: node.right.loc.end
            //            };
            //        }
            //    }

            //    return function () {
            //        var node, rangeInfo, locInfo;

            //        skipComment();
            //        rangeInfo = [index, 0];
            //        locInfo = {
            //            start: {
            //                line: lineNumber,
            //                column: index - lineStart
            //            }
            //        };

            //        node = parseFunction.apply(null, arguments);
            //        if (typeof node !== 'undefined') {

            //            if (range && typeof node.range === 'undefined') {
            //                rangeInfo[1] = index;
            //                node.range = rangeInfo;
            //            }

            //            if (loc && typeof node.loc === 'undefined') {
            //                locInfo.end = {
            //                    line: lineNumber,
            //                    column: index - lineStart
            //                };
            //                node.loc = locInfo;
            //            }

            //            if (isBinary(node)) {
            //                visit(node);
            //            }

            //            if (node.type === Syntax.MemberExpression) {
            //                if (typeof node.object.range !== 'undefined') {
            //                    node.range[0] = node.object.range[0];
            //                }
            //                if (typeof node.object.loc !== 'undefined') {
            //                    node.loc.start = node.object.loc.start;
            //                }
            //            }

            //            if (node.type === Syntax.CallExpression) {
            //                if (typeof node.callee.range !== 'undefined') {
            //                    node.range[0] = node.callee.range[0];
            //                }
            //                if (typeof node.callee.loc !== 'undefined') {
            //                    node.loc.start = node.callee.loc.start;
            //                }
            //            }
            //            return node;
            //        }
            //    };

            //};
            return null;
        }

        private char[] StringToArray(string str)
        {
            var length = str.Length;
            var result = new List<char>();

            for (var i = 0; i < length; ++i)
            {
                result.Add(str[i]);
            }
            return result.ToArray();
        }

        public Program Parse(string code)
        {
            return Parse(new JsCodeGeneration(), code);
        }

        public Program Parse(ICodeGeneration codeGeneration, string code)
        {
            _codeGeneration = codeGeneration;

            _lineNumber = (code.Length > 0) ? 1 : 0;
            _lineStart = 0;
            _length = code.Length;
            _buffer = null;
            _state = new State
            {
                AllowIn = true,
                LabelSet = new Dictionary<string, object>(),
                LastParenthesized = null,
                InFunctionBody = false,
                InIteration = false,
                InSwitch = false
            };

            _extra = new Extra();

            if (_length > 0)
            {
                _source = StringToArray(code).ToList();
            }

            return ParseProgram();
        }
    }
}
