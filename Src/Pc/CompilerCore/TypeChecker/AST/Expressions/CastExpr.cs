using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class CastExpr : IPExpr
    {
        public CastExpr(ParserRuleContext sourceLocation, IPExpr subExpr, PLanguageType type)
        {
            Type = type;
            SourceLocation = sourceLocation;
            SubExpr = subExpr;
        }

        public IPExpr SubExpr { get; }
        public PLanguageType Type { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
