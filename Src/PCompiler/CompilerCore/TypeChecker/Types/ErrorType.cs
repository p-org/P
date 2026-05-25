using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    /// <summary>
    /// Sentinel type produced when type-checking fails for an expression.
    /// In collecting mode (see <see cref="IDiagnosticCollector"/>), an
    /// originating visitor reports the diagnostic and returns a value whose
    /// <c>Type</c> is <see cref="Instance"/>. Downstream checks then treat
    /// <see cref="ErrorType"/> as compatible with everything, which suppresses
    /// cascading errors: e.g. <c>undeclaredVar + 1</c> should produce one
    /// "undeclared" diagnostic, not also an "incompatible operand types" one.
    ///
    /// Cascade suppression has two pieces:
    ///   - This class overrides <see cref="IsAssignableFrom"/> to return true
    ///     for any other type. That covers the asymmetric case where the
    ///     sentinel is on the LHS of an assignability check.
    ///   - <see cref="PLanguageType.IsSameTypeAs"/> short-circuits when either
    ///     operand is an <see cref="ErrorType"/>. That covers symmetric
    ///     equality checks regardless of operand order, since not every
    ///     existing type's <c>IsAssignableFrom</c> override knows about the
    ///     sentinel.
    ///   - Phase 2 will additionally add a <c>CheckAssignable</c> helper in
    ///     <c>TypeCheckingUtils</c> that visitors use in place of inline
    ///     <c>IsAssignableFrom</c> calls, to cover the remaining asymmetric
    ///     sites where <c>ErrorType</c> appears on the RHS.
    ///
    /// Phase 1 introduces the sentinel; no visitor produces it yet. Phase 2
    /// converts the ~111 throw sites in ExprVisitor/StatementVisitor to record-
    /// and-continue, where this type does its job.
    ///
    /// Invariant: <see cref="ErrorType"/> instances must never reach the IR
    /// transformer or backend code generators. <see cref="Compiler"/> guards
    /// this by skipping post-typecheck stages when <c>HasErrors</c>.
    /// </summary>
    public sealed class ErrorType : PLanguageType
    {
        public static readonly ErrorType Instance = new ErrorType();

        private ErrorType() : base(TypeKind.Base)
        {
        }

        public override string OriginalRepresentation => "<error>";

        public override string CanonicalRepresentation => "<error>";

        public override Lazy<IReadOnlyList<Event>> AllowedPermissions { get; } =
            new Lazy<IReadOnlyList<Event>>(() => new List<Event>());

        /// <summary>
        /// Returns true for any other type. This is the central trick that
        /// makes cascade-suppression work without per-site special cases:
        /// every existing <c>IsAssignableFrom</c> / <c>IsSameTypeAs</c> check
        /// transparently passes when either operand is the error sentinel,
        /// so no additional diagnostic is emitted.
        /// </summary>
        public override bool IsAssignableFrom(PLanguageType otherType) => true;

        public override PLanguageType Canonicalize() => this;
    }
}
