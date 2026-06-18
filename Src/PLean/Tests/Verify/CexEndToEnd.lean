/-
Tests.Verify.CexEndToEnd — structural assertions on the counter-example
renderer over a model captured from a live cvc5 run.

The golden test (`CexParserGolden`) pins exact output on synthetic
models. This file guards against the live failure mode the rendering was
built to fix: a model reaching the report with lean-auto mangling
(`|_foo.123_|`, `valid_fact_N`) un-cleaned. Rather than pin solver-
specific model values (brittle), it asserts structural invariants of the
rendered text. The input is a real cvc5 `(get-model)` reply captured from
a DistributedLock `eGrant` obligation.
-/
import PLean.Verify.CexModel

open PLean.Verify

private def lockCtx : CexNameCtx := {
  stateCtors  := #[("Node_Act", "Node", "Act")],
  fieldOrder  := #[("Node", "epoch"), ("Node", "held")],
  eventFields := #[("eGrant", #["node", "epoch"]), ("eAccept", #["epoch", "source"])],
  refFields   := #["node", "source"]
}

/-- A cvc5 model for a DistributedLock `eGrant` obligation, as embedded
in `loom_smt`'s `the goal is false:` diagnostic. -/
private def capturedDiagnostic : String :=
  "cvc5: the goal is false:" ++
  "((define-fun |_this| () |_Node| (|_mk| 3)) " ++
  " (define-fun |_param| () |_tGrant| (|_tGrant.mk| 4 7719)) " ++
  " (define-fun |_machines.116_| ((|x!0| Int)) |_MachineState| " ++
  "   (ite (= |x!0| 8) (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 9 false) 10) " ++
  "    (ite (= |x!0| 42) (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| (- 1) false) 43) " ++
  "                      (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 7718 true) 5)))) " ++
  " (define-fun |_sent.1546_| ((|x!0| |_Label|)) Bool " ++
  "   (let ((|a!1| (|_Label.mk| 3 (|_EventOrGoto.event| (|_E.eGrant| (|_tGrant.mk| 4 7719))) 11100)) " ++
  "         (|a!2| (|_Label.mk| 24 (|_EventOrGoto.event| (|_E.eGrant| (|_tGrant.mk| 71 72))) 5246))) " ++
  "     (or (= (|k!32| |x!0|) |a!1|) (= (|k!32| |x!0|) |a!2|)))) " ++
  " (define-fun |_received.21_| ((|x!0| |_Label|)) Bool false) " ++
  " (define-fun |_actionCount.117_| () Int 14099) " ++
  " (define-fun |valid_fact_0| () Bool true))"

private def hasSub (s pat : String) : Bool :=
  (s.splitOn pat).length > 1

private def rendered : String :=
  (extractModelText capturedDiagnostic >>= (renderModelText · lockCtx)).getD ""

-- The model body parses out of the diagnostic and renders to a
-- non-empty structured result.
/-- info: true -/
#guard_msgs in
#eval (extractModelText capturedDiagnostic >>= (renderModelText · lockCtx)).isSome

-- Machine state renders as `Machine@State(field=val, …)`.
/-- info: true -/
#guard_msgs in #eval hasSub rendered "machine[8] = Node@Act(epoch=9, held=false)"

-- Negative ints collapse from `(- 1)` to `-1`.
/-- info: true -/
#guard_msgs in #eval hasSub rendered "epoch=-1"

-- The sent trace decoded and ordered, with event field names.
/-- info: true -/
#guard_msgs in #eval hasSub rendered "sent (ordered by actionCount)"

-- Ref-typed payload fields render as machine labels; a ref that isn't a
-- machine-table key has unknown kind and stays bare (`#4`).
/-- info: true -/
#guard_msgs in #eval hasSub rendered "eGrant(node=#4, epoch=7719)"

-- De-mangling stripped lean-auto's leading underscore + gensym.
/-- info: false -/
#guard_msgs in #eval hasSub rendered "_sent.1546_"

-- Solver boilerplate (`valid_fact_*`) and internal tables (`k!32`) are
-- not surfaced as witnesses.
/-- info: false -/
#guard_msgs in #eval hasSub rendered "valid_fact"

/-- info: false -/
#guard_msgs in #eval hasSub rendered "k!32"

-- Handler bindings appear under the witnesses heading.
/-- info: true -/
#guard_msgs in #eval hasSub rendered "witnesses"
