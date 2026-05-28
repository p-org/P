// Phase 2 coverage: VisitCtorExpr's recovery path visits constructor
// arguments BEFORE the interface lookup, so internal errors in the
// arguments surface even when the interface itself is unknown.
//
// Errors (collecting mode):
//   1. `undeclaredA` — MissingDeclaration (1st arg)
//   2. `undeclaredB` — MissingDeclaration (2nd arg)
//   3. `UnknownIface` — MissingDeclaration interface
//
// The downstream `m = new UnknownIface(...)` assignment-type check is
// suppressed because the RHS is ErrorExpr — no spurious 4th diagnostic.
//
// Strict mode aborts on the first error: count = 1 (whichever arg/iface
// the visitor reaches first — args are visited before interface lookup,
// so this is `undeclaredA`).
// Collecting mode reports all three: count = 3.

machine Main {
    start state S {
        entry {
            var m: machine;
            m = new UnknownIface(undeclaredA, undeclaredB);
        }
    }
}
