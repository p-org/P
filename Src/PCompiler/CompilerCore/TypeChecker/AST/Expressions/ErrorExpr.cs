using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    /// <summary>
    /// Sentinel expression returned by a visitor when type-checking fails for
    /// an expression node. Its <see cref="Type"/> is <see cref="ErrorType.Instance"/>,
    /// which makes all downstream compatibility checks vacuously succeed (see
    /// <see cref="ErrorType"/>'s class doc for the cascade-suppression rationale).
    ///
    /// Deliberately does NOT implement <see cref="IExprTerm"/>: the IR
    /// transformer's post-typecheck passes are constrained to <c>IExprTerm</c>,
    /// so an <see cref="ErrorExpr"/> that accidentally leaks past type-checking
    /// will trip a clear cast failure rather than silently corrupting downstream
    /// code generation. <c>Compiler.Compile</c> guards this by skipping IR
    /// transformation when <c>handler.Diagnostics.HasErrors</c>.
    ///
    /// Produced by <c>ExprVisitor</c>/<c>StatementVisitor</c> whenever a node
    /// fails type-checking in collecting mode.
    /// </summary>
    public sealed class ErrorExpr : IPExpr
    {
        public ErrorExpr(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public PLanguageType Type => ErrorType.Instance;

        public ParserRuleContext SourceLocation { get; }
    }
}
