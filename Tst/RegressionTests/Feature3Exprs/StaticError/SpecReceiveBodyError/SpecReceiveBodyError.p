// Phase 2 coverage: receive on a spec machine is illegal AND we want to
// surface body / event-id errors in the same pass. Validates the recently-
// added recovery in VisitReceiveStmt's spec-machine branch.
//
// Errors:
//   1. `receive` is illegal on a spec machine (IllegalMonitorOperation)
//   2. `UndeclaredEvt` is not declared (MissingDeclaration on the event id)
//   3. `x = "str"` in the handler body — bool-typed var assigned a string
//
// Collecting mode must report exactly 3 diagnostics; strict mode reports
// only the first one before aborting.

event E: int;

spec Watcher observes E {
    start state Init {
        on E do (p: int) {
            receive {
                case UndeclaredEvt: (q: int) {
                    var x: bool;
                    x = "str";
                }
            }
        }
    }
}

machine Main {
    start state S { entry { } }
}
