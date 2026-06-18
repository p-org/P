/-
Tests.Verify.CexParserGolden — pin the counter-example parser/renderer
output on fixed model strings.

`renderModelText` consumes the model S-expression `loom_smt` embeds in
its SAT diagnostic, against a `CexNameCtx` carrying registry-derived
names (state→machine, `Fields` order, event payload fields). These tests
feed it a synthetic model + ctx and pin the rendered result, so parser
drift fails loudly here rather than degrading the live `#pverify` report.
-/
import PLean.Verify.CexModel

open PLean.Verify

/-! ## De-mangling

lean-auto field gensyms collapse to the readable base; member
projections survive. -/

/-- info: "sent" -/
#guard_msgs in #eval demangle "_sent.1546_"

/-- info: "machines" -/
#guard_msgs in #eval demangle "_machines.116_"

/-- info: "Label.actionCount" -/
#guard_msgs in #eval demangle "_Label.actionCount"

/-- info: "b" -/
#guard_msgs in #eval demangle "_b.99_"

/-! ## Registry name context (mirrors what a DistributedLock-shaped
pmodule produces). -/

private def lockCtx : CexNameCtx := {
  stateCtors  := #[("Node_Act", "Node", "Act")],
  fieldOrder  := #[("Node", "epoch"), ("Node", "held")],
  eventFields := #[("eGrant", #["node", "epoch"]), ("eAccept", #["epoch", "source"])],
  refFields   := #["node", "source"]
}

/-! ## Datatype-constructor model

`machines` maps a ref to a `MachineState.mk stage currentState fields
kind` value; `sent` is an `or`-of-equalities over `Label.mk …` values
with the labels in `let` bindings. The renderer recovers
`Node@Act(field=val)` and `eGrant(field=val)`, ordering the trace by the
label's `actionCount`. -/

private def lockModel : String :=
  "((define-fun |_machines.10_| ((|x| Int)) |_MachineState| " ++
  "   (ite (= |x| 8) (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 9 false) 1) " ++
  "    (ite (= |x| 42) (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| (- 1) true) 1) " ++
  "                    (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 0 false) 0)))) " ++
  " (define-fun |_sent.20_| ((|l| |_Label|)) Bool " ++
  "   (let ((|a!1| (|_Label.mk| 8 (|_EventOrGoto.event| (|_E.eGrant| (|_tGrant.mk| 42 72))) 5246)) " ++
  "         (|a!2| (|_Label.mk| 3 (|_EventOrGoto.event| (|_E.eGrant| (|_tGrant.mk| 4 99))) 11100))) " ++
  "     (or (= (|k!1| |l|) |a!1|) (= (|k!1| |l|) |a!2|)))) " ++
  " (define-fun |_received.21_| ((|l| |_Label|)) Bool " ++
  "   (= (|k!1| |l|) (|_Label.mk| 8 (|_EventOrGoto.event| (|_E.eGrant| (|_tGrant.mk| 42 72))) 5246))) " ++
  " (define-fun |_actionCount.30_| () Int 12) " ++
  " (define-fun |_this| () |_Node| (|_mk| 8)) " ++
  " (define-fun |valid_fact_0| () Bool true))"

/--
info: machines:
  machine[8] = Node@Act(epoch=9, held=false)
  machine[42] = Node@Act(epoch=-1, held=true)
  machine[else] = Node@Act(epoch=0, held=false)
sent (ordered by actionCount):
  @5246 → Node#8  eGrant(node=Node#42, epoch=72) [delivered]
  @11100 → #3  eGrant(node=#4, epoch=99)
actionCount = 12
witnesses (handler & skolem bindings):
  this = Node#8 = Node@Act(epoch=9, held=false)
-/
#guard_msgs in #eval IO.println ((renderModelText lockModel lockCtx).getD "FALLBACK")

/-! ## Empty sent set renders as `[]`. -/

private def noSentModel : String :=
  "((define-fun |_machines.10_| ((|x| Int)) |_MachineState| " ++
  "   (ite (= |x| 0) (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 5 true) 1) " ++
  "                  (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 0 false) 0))) " ++
  " (define-fun |_sent.20_| ((|l| |_Label|)) Bool false) " ++
  " (define-fun |_actionCount.30_| () Int 0))"

/--
info: machines:
  machine[0] = Node@Act(epoch=5, held=true)
  machine[else] = Node@Act(epoch=0, held=false)
sent (ordered by actionCount): []
actionCount = 0
-/
#guard_msgs in #eval IO.println ((renderModelText noSentModel lockCtx).getD "FALLBACK")

/-! ## Constant `machines` function (no `ite` cases): the single state
applies to every ref, surfaced as `machine[else]`, and witnessed
machine-refs resolve their state inline. -/

private def constMachinesModel : String :=
  "((define-fun |_machines.1_| ((|x| Int)) |_MachineState| " ++
  "   (|_MachineState.mk| false |_S.Node_Act| (|_Fields.mk| 7 false) 1)) " ++
  " (define-fun |_sent.2_| ((|l| |_Label|)) Bool false) " ++
  " (define-fun |_actionCount.3_| () Int 0) " ++
  " (define-fun |_n1| () |_Node| (|_Node.mk| 1)) " ++
  " (define-fun |_n2| () |_Node| (|_Node.mk| 4)))"

private def lockKindCtx : CexNameCtx :=
  { lockCtx with machineKinds := #["Node"] }

/--
info: machines:
  machine[else] = Node@Act(epoch=7, held=false)
sent (ordered by actionCount): []
actionCount = 0
witnesses (handler & skolem bindings):
  n1 = Node#1 = Node@Act(epoch=7, held=false)
  n2 = Node#4 = Node@Act(epoch=7, held=false)
-/
#guard_msgs in #eval IO.println ((renderModelText constMachinesModel lockKindCtx).getD "FALLBACK")

/-! ## Without registry names, machine/event fall back to raw values. -/

/--
info: machines:
  machine[0] = (MachineState.mk false S.Node_Act (Fields.mk 5 true) 1)
sent (ordered by actionCount): []
actionCount = 0
-/
#guard_msgs in #eval IO.println ((renderModelText noSentModel {}).getD "FALLBACK")

/-! ## Non-model text falls through to `none`. -/

/-- info: "FALLBACK" -/
#guard_msgs in #eval (renderModelText "not an s-expression at all").getD "FALLBACK"
