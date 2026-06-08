/-
PLean.Semantics.Primitives — `send` / `raise` / `goto` / `announce`
/ `newMachine` as PM combinators.

Each primitive mirrors PVerifier's UCLID5 statement-emission cases at
`Uclid5CodeGenerator.cs:1967-1999`:

  send t, ev, p   ↝ sent ∪= { Label{t, .event ev_p, k} } ; k += 1
  goto S, p       ↝ sent ∪= { Label{this, .goto S_p, k} } ;
                    k += 1 ; machine.currentState := S ; machine.stage := true
  raise ev, p     ↝ send this ev p             (intra-machine)
  announce ev, p  ↝ broadcast send             (Phase-4 spec hook)
  newMachine kind args ↝ allocate fresh ref, register initial state

All primitives are pure `StateT`/`PM`-level updates — no nondeterminism
yet (that comes in via the runtime's choice of which in-flight label to
dispatch next, modeled in Phase 3's obligation generator).

WP specs for the underlying `get`/`set` operations need to be derived
per-program-instantiation via `#derive_lifted_wp`. For Phase-1
hand-written examples this happens in the example file (e.g.
`HandPingPong.lean`). Phase 2's `#gen_module` will emit the
`#derive_lifted_wp` calls automatically per pmodule.
-/
import PLean.Semantics.Monad

namespace PLean

variable {P : ProgramSig}
variable [DecidableEq P.E] [DecidableEq P.G]

/-- `send target ev`: enqueue an event label addressed to `target` and
bump the global action counter. Mirrors PVerifier's `SendStmt` branch
(`Uclid5CodeGenerator.cs:1981-1999`). -/
def send (target : MachineRef) (ev : P.E) : PM P Unit := do
  let s ← (get : StateT (GlobalState P) DivM (GlobalState P))
  let lbl : P.Label :=
    { target := target, action := .event ev, actionCount := s.actionCount }
  set ((s.addSent lbl).bumpActionCount)

/-- `raise ev`: like `send` but addressed to the running machine. -/
def raise (this : MachineRef) (ev : P.E) : PM P Unit :=
  send this ev

/-- `goto stateTag gotoPayload`: enqueue a goto label addressed to the
running machine, bump the counter, and update the machine's
`currentState`/`stage`. Preserves the machine's `kind` field (D20).
Mirrors PVerifier's `GotoStmt` branch. -/
def goto (this : MachineRef) (newState : P.S) (gotoArg : P.G) : PM P Unit := do
  let s ← (get : StateT (GlobalState P) DivM (GlobalState P))
  let lbl : P.Label :=
    { target := this, action := .goto gotoArg, actionCount := s.actionCount }
  let curr := s.machines this
  let nextMachine : P.MachineState :=
    { stage := true, currentState := newState, fields := curr.fields, kind := curr.kind }
  set (((s.addSent lbl).bumpActionCount).updateMachine this nextMachine)

/-- `announce ev`: broadcast variant of `send`. Phase-4 spec-machine
flattening hooks here. -/
def announce (this : MachineRef) (ev : P.E) : PM P Unit :=
  send this ev

/-- `newMachine kind args` — Phase 1 returns a placeholder ref. -/
def newMachine (this : MachineRef) (_kind : Nat) : PM P MachineRef :=
  pure this

/-- Mark a label as received. Called by the runtime when a handler fires. -/
def markReceived (lbl : P.Label) : PM P Unit := do
  let s ← (get : StateT (GlobalState P) DivM (GlobalState P))
  set (s.addReceived lbl)

end PLean
