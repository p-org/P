using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class NullLiteralExpr : IPExpr
    {
        public ParserRuleContext SourceLocation { get; }

        public NullLiteralExpr(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public PLanguageType Type { get; } = PrimitiveType.Null;
    }
}
