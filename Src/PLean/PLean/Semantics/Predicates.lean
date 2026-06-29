/-
PLean.Semantics.Predicates — state predicates that mirror P's surface
keywords (`inflight`, `sent`, `is`, `targets`).

Each predicate is a plain Lean `def` so it appears verbatim in
invariant bodies and synthesised Hoare triples, and so SMT-preparation
unfolds reach the underlying `s.sent`/`s.received` projections.
-/
import PLean.Semantics.GlobalState
import PLean.Verify.SimpAttrs

namespace PLean

variable {P : ProgramSig}

/-! ## Buffer-state predicates (act on `GlobalState P`) -/

/-- `inflight lbl s`: the label has been sent but not yet received. -/
@[inline] def inflight (lbl : P.Label) (s : GlobalState P) : Prop :=
  s.sent lbl = true ∧ s.received lbl = false

/-- `sent lbl s`: the label is in the sent set. -/
@[inline] def sent (lbl : P.Label) (s : GlobalState P) : Prop :=
  s.sent lbl = true

/-- `received lbl s`: the label has been delivered. -/
@[inline] def received (lbl : P.Label) (s : GlobalState P) : Prop :=
  s.received lbl = true

/-! ## Label predicates (act on `Label` directly, no `GlobalState`)

The user writes `lbl targets m`; the `lbl is <ev>` form is per-event
and is emitted by `Commands/GenModule.lean` as `is_<ev>` (a tag-only
check, not a payload-equality check — which is what P semantics
specifies). -/

/-- Test that a label is delivered to a particular machine. -/
@[inline] def Label.targets? (lbl : P.Label) (m : MachineRef) : Prop :=
  lbl.target = m

/-! ## Machine-state predicate -/

/-- `stateOf m s`: the discrete control state of machine `m` in state `s`. -/
@[inline] def stateOf (m : MachineRef) (s : GlobalState P) : P.S :=
  (s.machines m).currentState

/-! ## Temporal precedence operator `≺`

Two labels are ordered by their `actionCount`. Every primitive that
creates a label first records the global counter and then bumps it, so
each label has a unique counter; the `UniqueActions` default invariant
certifies the ordering is total over the sent set. -/

/-- `a ≺ b`: label `a` was sent before label `b`. The `≺` infix lives
in `Syntax/Notation.lean`. Tagged `@[pverifySimp]` so SMT prep
unfolds the `actionCount` comparison; otherwise lean-auto sees
`precedes` as an opaque uninterpreted predicate and can't relate
it to the `<` comparison `IncreasingCount` / `UniqueActions` use. -/
@[inline, pverifySimp] def precedes (a b : P.Label) : Prop :=
  a.actionCount < b.actionCount

end PLean
