// Phase 2 cascade-suppression coverage: 2 independent errors nested in a
// single expression. Validates that the combiner rule (any child with
// ErrorType makes the parent's result ErrorType without an additional
// diagnostic) prevents cascade noise.
//
// Errors (collecting mode):
//   1. `undeclaredA` is not declared           — MissingDeclaration
//   2. `undeclaredB` is not declared           — MissingDeclaration
//
// What we DON'T want (would indicate broken cascade suppression):
//   - "Incompatible operand" on the outer `+` (lhs is ErrorType so the
//     bin-op combiner rule short-circuits silently).
//   - "Incompatible operand" on the inner `*` ("str" mismatch is masked
//     by undeclaredB.bar being ErrorType — the right behavior, since
//     undeclaredB.bar is the root cause).
//   - "TypeMismatch" on `x = ...` (assignment check is suppressed when
//     the RHS is ErrorType).
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports both missing-decls only: count = 2.

machine Main {
    var x: int;
    start state S {
        entry {
            x = undeclaredA.foo + undeclaredB.bar * "str";
        }
    }
}
