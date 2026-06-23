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
(`Uclid5CodeGenerator.cs:1981-1999`).

NOTE (Phase 3, R15 / R-P3.2): primitives are emitted as `@[reducible]`
so the obligation generator's `unfold` step (or a `simp` pass over
the primitives) reaches the underlying `get` / `set` calls. The
per-pmodule `#derive_lifted_wp` for `get` / `set` (`emitDerivedWP`)
registers `loomSpec` lemmas, so once the primitive reduces to its
body `wpgen` walks through state reads / writes natively. -/
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
`currentState`/`stage`. Preserves the machine's `kind` field (D20).
Mirrors PVerifier's `GotoStmt` branch. -/
@[reducible] def goto (this : MachineRef) (newState : P.S) (gotoArg : P.G) : PM P Unit := do
  let s ← get
  let lbl : P.Label :=
    { target := this, action := .goto gotoArg, actionCount := s.actionCount }
  let curr := s.machines this
  let nextMachine : P.MachineState :=
    { stage := true, currentState := newState, fields := curr.fields, kind := curr.kind }
  set (((s.addSent lbl).bumpActionCount).updateMachine this nextMachine)

/-- `announce ev`: broadcast variant of `send`. Phase-4 spec-machine
flattening hooks here. -/
@[reducible] def announce (this : MachineRef) (ev : P.E) : PM P Unit :=
  send this ev

/-- `newMachine kind args` — Phase 1 returns a placeholder ref. -/
@[reducible] def newMachine (this : MachineRef) (_kind : Nat) : PM P MachineRef :=
  pure this

/-- Mark a label as received. Called by the runtime when a handler fires. -/
@[reducible] def markReceived (lbl : P.Label) : PM P Unit := do
  let s ← get
  set (s.addReceived lbl)

/-- `choose bound` — return a nondeterministically chosen `Int` `x`
with `0 ≤ x ∧ x ≤ bound`. Mirrors P's `choose(n)`, which returns an
integer in `[0, n-1]` (we use `[0, bound]` for symmetry of the
constraint; the off-by-one isn't load-bearing — invariants depending
on the exact bound state it explicitly).

Implementation: `MonadNonDet.pickSuchThat` over the decidable predicate
`fun x => 0 ≤ x ∧ x ≤ bound`. `Findable` synthesises from `Encodable
Int + DecidablePred` automatically, so no user-supplied witness is
needed at the call site.

The WP is `⨅ x, ⌜0 ≤ x ∧ x ≤ bound⌝ ⇨ post x` — i.e., "the
post-condition must hold for every `x` *if* `0 ≤ x ≤ bound`". The
verifier gets the bound as a hypothesis when reasoning about the
chosen value. -/
@[reducible] def choose (bound : Int) : PM P Int :=
  MonadNonDet.pickSuchThat (m := PM P) Int (fun x => 0 ≤ x ∧ x ≤ bound)

end PLean

open PartialCorrectness DemonicChoice in
/-- WP spec for `PLean.choose`: post must hold for every `x` *given*
`0 ≤ x ∧ x ≤ bound`. Registered as a `loomSpec` so `wpgen` steps
through `choose` (otherwise it falls into `WPGen.default`). -/
@[loomSpec, loomWpSimp]
noncomputable def PLean.WPGen.choose
    {P : PLean.ProgramSig}
    [DecidableEq P.E] [DecidableEq P.G]
    (bound : Int) :
    WPGen (PLean.choose (P := P) bound : PLean.PM P Int) where
  get := fun (post : Int → PLean.PProp P) =>
    fun s => ∀ x : Int, 0 ≤ x → x ≤ bound → post x s
  prop := by
    intro post
    unfold PLean.choose
    rw [MonadNonDet.wp_pickSuchThat]
    -- Pointwise on the state. `⨅` over functions unfolds via
    -- `iInf_apply`; the residual `⨅` over `Prop` unfolds via
    -- `iInf_Prop_eq` to a `∀`. Then `⌜p⌝ ⇨ q` over `Prop`-valued
    -- functions reduces case-by-case on `p`.
    intro s hAll
    rw [iInf_apply, iInf_Prop_eq]
    intro x
    rw [himp_eq]
    by_cases hp : 0 ≤ x ∧ x ≤ bound
    · exact Or.inl (hAll x hp.1 hp.2)
    · exact Or.inr (by simp [LE.pure, hp])
