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
    /// code generation. The <see cref="Compiler"/> top-level guards this by
    /// skipping IR transformation when the diagnostic collector
    /// <c>HasErrors</c>.
    ///
    /// Phase 1 introduces this class; no visitor produces it yet.
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
