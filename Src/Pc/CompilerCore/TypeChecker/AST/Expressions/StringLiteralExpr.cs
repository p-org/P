using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class StringLiteralExpr : IStaticTerm<string>
    {
        public StringLiteralExpr(ParserRuleContext sourceLocation, string value)
        {
            SourceLocation = sourceLocation;
            Value = value;
        }

        public string Value { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; } = PrimitiveType.String;
    }
}