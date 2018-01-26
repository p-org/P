using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.ASTExt
{
    internal class CloneExpr : IPExpr
    {
        public CloneExpr(IPExpr subExpr)
        {
            SubExpr = subExpr;
            SourceLocation = subExpr.SourceLocation;
            Type = subExpr.Type;
        }

        public IPExpr SubExpr { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}
