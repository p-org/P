// Phase 2 coverage: VisitGotoStmt's two recovery paths each visit the
// rvalueList AFTER reporting the goto-level error, so internal argument
// errors surface even when the goto itself is malformed.
//
// Errors (collecting mode):
//   1. `MissingState`  — state not found (goto-level error)
//   2. `undeclaredA`   — MissingDeclaration in the rvalue of the first goto
//                         (surfaces because VisitGotoStmt visits the
//                          rvalueList AFTER reporting the missing-state)
//   3. `undeclaredB`   — MissingDeclaration in the rvalue of the second
//                         goto (state S has no entry params)
//   4. goto S, ...     — IncorrectArgumentCount (1 arg given, 0 expected
//                         because S's entry handler takes no params)
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports all four: count = 4.

machine Main {
    start state S {
        entry {
            goto MissingState, undeclaredA;
            goto S, undeclaredB;
        }
    }
}
