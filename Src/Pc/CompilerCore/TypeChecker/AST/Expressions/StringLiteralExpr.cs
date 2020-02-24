using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;
using System.Diagnostics.Contracts;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class StringLiteralExpr : IStaticTerm<string>
    {
        public StringLiteralExpr(ParserRuleContext sourceLocation, string value)
        {
            Contract.Requires(sourceLocation != null);
            Contract.Requires(value != null);
            SourceLocation = sourceLocation;
            Value = value;
        }

        public string Value { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; } = PrimitiveType.String;
    }
}