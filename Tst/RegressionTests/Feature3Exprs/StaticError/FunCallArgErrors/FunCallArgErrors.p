// Phase 2 coverage: VisitFunCallExpr's Function branch — arity mismatch
// and per-argument type mismatch each fire independently, with
// cascade-suppression preventing extra "incompatible operand" diagnostics
// when an argument has already errored.
//
// Errors (collecting mode):
//   1. `undeclaredA`             — MissingDeclaration in stmt 1's arg
//   2. helper(undeclaredA)       — IncorrectArgumentCount (1 arg, expected 2)
//   3. `undeclaredB`             — MissingDeclaration in stmt 2's first arg
//   4. helper(undeclaredB,"str") — TypeMismatch on the 2nd arg (str -> int).
//                                   The 1st arg's int mismatch is suppressed
//                                   because arg[0] is ErrorType.
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports all four: count = 4.

fun helper(a: int, b: int): int { return a + b; }

machine Main {
    start state S {
        entry {
            var x: int;
            x = helper(undeclaredA);
            x = helper(undeclaredB, "str");
        }
    }
}
