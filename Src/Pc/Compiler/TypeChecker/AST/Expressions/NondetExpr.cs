using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class NondetExpr : IPExpr
    {
        public ParserRuleContext SourceLocation { get; }

        public NondetExpr(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
