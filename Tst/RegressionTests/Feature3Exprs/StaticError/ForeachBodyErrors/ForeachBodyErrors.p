// Phase 2 coverage: foreach body type-checking with two independent errors.
// Validates that VisitForeachStmt's happy path actually visits the body
// (not just the iterator/collection) AND that body-internal errors each
// surface independently in collecting mode without cascade leaks.
//
// `invariant` (the original target of this test) is PVerifier-only syntax,
// so we exercise only the body path here. The audit-flagged
// invariant-visiting fix lives in StatementVisitor.VisitForeachStmt and
// will get explicit regression coverage once a PVerifier-mode test
// harness lands.
//
// Errors (collecting mode):
//   1. `x = undeclaredVar`  — MissingDeclaration on the RHS
//   2. `b = "str"`           — bool lvalue, string rvalue (TypeMismatch)
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports both: count = 2.

machine Main {
    var items: seq[int];
    var x: int;
    var b: bool;
    var i: int;
    start state S {
        entry {
            foreach (i in items) {
                x = undeclaredVar;
                b = "str";
            }
        }
    }
}
