// Phase 3 acceptance test for per-machine error isolation across function
// body type-checking (pass 3).
//
// Three independent machines, each with one error in its entry handler.
// Validates that Phase 2's Report-and-continue path (in ExprVisitor /
// StatementVisitor) PLUS Phase 3's TolerantStep wrapper around pass 3
// (FunctionBodyVisitor + FunctionValidator) lets errors from EACH machine
// surface without one bad machine clobbering its siblings' diagnostics.
//
// Note: pass 2a (MachineChecker.Validate) is NOT exercised by this file —
// each machine has a valid start state, payload type, etc. A separate test
// would be needed to validate per-machine isolation across pass 2a throws
// (e.g. one machine with MissingStartState + another with a body error).
//
// Errors (collecting mode):
//   1. MachineA: `x = true`            — bool assigned to int
//   2. MachineB: `y = undeclaredVar`   — missing declaration
//   3. MachineC: `z = z + "str"`       — int + string binop mismatch
//
// Strict mode aborts on the first error: count = 1.
// Collecting mode reports all three: count = 3.

machine MachineA {
    var x: int;
    start state S {
        entry { x = true; }
    }
}

machine MachineB {
    var y: int;
    start state S {
        entry { y = undeclaredVar; }
    }
}

machine MachineC {
    var z: int;
    start state S {
        entry { z = z + "str"; }
    }
}

machine Main {
    start state S { entry { } }
}
