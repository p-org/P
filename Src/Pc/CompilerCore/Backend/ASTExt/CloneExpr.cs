using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.ASTExt
{
    internal class CloneExpr : IPExpr
    {
        public CloneExpr(IExprTerm term)
        {
            Term = term;
            SourceLocation = term.SourceLocation;
            Type = term.Type;
        }

        public IExprTerm Term { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}
