// Phase 3 per-FUNCTION isolation (distinct from MultiMachineErrors which
// is per-MACHINE). Three functions inside ONE machine, each with one
// independent error. Validates that the TolerantStep wrapper around
// pass 3 (FunctionBodyVisitor + FunctionValidator) catches per-function
// throws and continues to the next function.
//
// Note: in collecting mode, Phase 2's Report-and-continue path means these
// errors typically don't reach the TolerantStep catch — they're reported
// inside the visitor without throwing. The test still validates the
// user-visible "all 3 errors surface" outcome, which is the contract
// that matters.
//
// Errors (collecting mode):
//   1. bad1: `x = true`         — bool assigned to int
//   2. bad2: `y = undeclaredVar` — MissingDeclaration
//   3. bad3: `z = z + "s"`       — int + string BinOpTypeMismatch
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports all three: count = 3.

machine Main {
    fun bad1() { var x: int; x = true; }
    fun bad2() { var y: int; y = undeclaredVar; }
    fun bad3() { var z: int; z = z + "s"; }
    start state S {
        entry { bad1(); bad2(); bad3(); }
    }
}
