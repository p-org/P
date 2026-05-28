// Phase 2 acceptance test for multi-error type checking.
//
// This file deliberately contains several independent type errors that
// should NOT cascade into one another:
//
//   - x = true         → bool assigned to int                (error 1)
//   - b = undeclaredVar → missing declaration                (error 2; must
//                         NOT also report "wrong assignment type" — that
//                         would be a cascade leak from ErrorType)
//   - x + "hello"      → mismatched binary operands           (error 3)
//   - send this, E, (1,2,3) → payload TYPE mismatch (E has no payload type,
//                        but the call passes a 3-tuple). Note: `(1,2,3)`
//                        parses as a single unnamed-tuple argument, so this
//                        fires TypeMismatch (tuple→null) via
//                        ValidatePayloadTypes' single-arg CheckArgument
//                        branch, NOT IncorrectArgumentCount. The count
//                        is still 1 either way — the comment is precise
//                        about the MECHANISM, not just the outcome.
//                        (error 4)
//
// Behavior contracts:
//   - Strict mode (default): exit 1 after the first error is reported.
//     Identical to the historical behavior — this file is no different
//     from any other single-error StaticError test in this mode.
//   - Collecting mode (P_COMPILER_COLLECT_ERRORS=1): exit 1 after
//     reporting all 4 independent errors. The Phase1DormancyTest
//     fixture asserts `collecting_count >= strict_count`, which holds
//     trivially with strict=1; the stronger acceptance check lives in
//     MultiErrorAcceptanceTest.cs.

event E;

machine Main {
    var x: int;
    var b: bool;

    start state S {
        entry {
            x = true;
            b = undeclaredVar;
            x = x + "hello";
            send this, E, (1, 2, 3);
        }
    }
}
