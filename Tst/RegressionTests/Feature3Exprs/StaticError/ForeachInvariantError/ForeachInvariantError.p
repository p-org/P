// Phase 2 coverage: loop invariant errors when the iterator IS declared
// (the success path of VisitForeachStmt; Copilot's earlier fix only
// addressed the missing-iter branch). The foreach body AND each invariant
// must both be visited so type errors in either get reported.
//
// Errors (collecting mode):
//   1. `x = undeclaredVar` inside the body  — MissingDeclaration
//   2. `i + "str"` in the invariant         — BinOpTypeMismatch
//
// The `==` outer in `i + "str" == 0` should NOT add a third diagnostic
// (lhs becomes ErrorType after the `+` mismatch, so the `==` combiner
// short-circuits silently).
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports both: count = 2.

machine Main {
    var items: seq[int];
    var x: int;
    var i: int;
    start state S {
        entry {
            foreach (i in items) invariant i + "str" == 0;
            {
                x = undeclaredVar;
            }
        }
    }
}
