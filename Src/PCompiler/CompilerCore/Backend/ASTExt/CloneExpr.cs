using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.ASTExt
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