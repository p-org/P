// Phase 3 acceptance test for per-machine error isolation.
//
// Three independent machines, each with one error. Validates that
// Analyzer.cs's TolerantStep wrappers around pass 2a (MachineChecker) and
// pass 3 (FunctionBodyVisitor) report errors from EACH machine without one
// bad machine clobbering the diagnostics of its siblings.
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
