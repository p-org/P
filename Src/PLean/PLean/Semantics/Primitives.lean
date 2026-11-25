/-
PLean.Semantics.Primitives — `send` / `raise` / `goto` / `announce`
/ `newMachine` as PM combinators.

  send t, ev, p   ↝ sent ∪= { Label{t, .event ev_p, k} } ; k += 1
  goto S, p       ↝ sent ∪= { Label{this, .goto S_p, k} } ;
                    k += 1 ; machine.currentState := S ; machine.stage := true
  raise ev, p     ↝ send this ev p             (intra-machine)
  announce ev, p  ↝ broadcast send             (spec hook)
  newMachine kind args ↝ allocate fresh ref, register initial state

All primitives are pure `StateT`/`PM`-level updates. Nondeterminism
enters at the obligation level via the runtime's choice of which
in-flight label to dispatch next.

`get`/`set` steps are handled by `Std.WP`'s `StateT` specs.
-/
import PLean.Semantics.Monad

namespace PLean

variable {P : ProgramSig}
variable [DecidableEq P.E] [DecidableEq P.G]

/-- `send target ev`: enqueue an event label addressed to `target` and
bump the global action counter.

Primitives are `@[reducible]` so the obligation generator's `unfold`
step (or `simp` over the primitives) reaches the underlying `get` /
`set` calls. -/
@[reducible] def send (target : MachineRef) (ev : P.E) : PM P Unit := do
  let s ← get
  let lbl : P.Label :=
    { target := target, action := .event ev, actionCount := s.actionCount }
  set ((s.addSent lbl).bumpActionCount)

/-- `raise ev`: like `send` but addressed to the running machine. -/
@[reducible] def raise (this : MachineRef) (ev : P.E) : PM P Unit :=
  send this ev

/-- `goto stateTag gotoPayload`: enqueue a goto label addressed to the
running machine, bump the counter, and update the machine's
`currentState`/`stage`. Preserves the machine's `kind` field. -/
@[reducible] def goto (this : MachineRef) (newState : P.S) (gotoArg : P.G) : PM P Unit := do
  let s ← get
  let lbl : P.Label :=
    { target := this, action := .goto gotoArg, actionCount := s.actionCount }
  let curr := s.machines this
  let nextMachine : P.MachineState :=
    { stage := true, currentState := newState, fields := curr.fields, kind := curr.kind }
  set (((s.addSent lbl).bumpActionCount).updateMachine this nextMachine)

/-- `announce ev`: broadcast variant of `send`; spec-machine
flattening hooks here. -/
@[reducible] def announce (this : MachineRef) (ev : P.E) : PM P Unit :=
  send this ev

/-- `newMachine kind args` — placeholder; allocation isn't yet
modelled, so this returns the caller's ref. -/
@[reducible] def newMachine (this : MachineRef) (_kind : Nat) : PM P MachineRef :=
  pure this

/-- Mark a label as received. Called by the runtime when a handler fires. -/
@[reducible] def markReceived (lbl : P.Label) : PM P Unit := do
  let s ← get
  set (s.addReceived lbl)

/- `choose bound` (P's `choose(n)`) is not provided by the interim
deterministic `PM`; its nondeterministic semantics is part of the
pending transition-model redesign (see `Semantics/Monad.lean`). -/

end PLean
