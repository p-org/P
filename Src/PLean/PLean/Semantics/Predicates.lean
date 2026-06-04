/-
PLean.Semantics.Predicates — state predicates that mirror P's surface
keywords.

P's surface uses the keywords `inflight`, `sent`, `is`, `targets` (see
`PLexer.g4:72`). PVerifier compiles them to UCLID5 bool expressions on
the global state (`Uclid5CodeGenerator.cs:600-602` for `InFlight`,
`:2049-2052` for `targets`/`sent`/`flying`/`is`/`as`). PLean defines
each as a Lean `def` so they appear verbatim in invariant bodies and
in synthesized Hoare triples.

Names mirror P's keywords, not C# AST node names like `FlyingExpr`.
-/
import PLean.Semantics.GlobalState

namespace PLean

variable {P : ProgramSig}

/-! ## Buffer-state predicates (act on `GlobalState P`) -/

/-- `inflight lbl s`: the label has been sent but not yet received.
Mirrors PVerifier's `InFlight` macro (`Uclid5CodeGenerator.cs:600-602`):
`(sent[lbl] ∧ ¬received[lbl])`. -/
@[inline] def inflight (lbl : P.Label) (s : GlobalState P) : Prop :=
  s.sent lbl = true ∧ s.received lbl = false

/-- `sent lbl s`: the label is in the sent set. -/
@[inline] def sent (lbl : P.Label) (s : GlobalState P) : Prop :=
  s.sent lbl = true

/-- `received lbl s`: the label has been delivered. -/
@[inline] def received (lbl : P.Label) (s : GlobalState P) : Prop :=
  s.received lbl = true

/-! ## Label predicates (act on `Label` directly, no `GlobalState`)

These match the P-surface meaning literally. The user writes
`lbl is ePing`, `lbl targets m`; we expose the underlying functions so
those notations (Phase 2) desugar to plain function applications. -/

/-- Test that a label's action is the event with payload `e`.

Equivalent to PVerifier's `LabelAdtIsE`/`EventOrGotoAdtIsE`
(`Uclid5CodeGenerator.cs:884-896`), modulo the user's choice of how the
event union encodes its tag. The full `=` is over the payload too,
which is what most invariants want. -/
@[inline] def Label.isEvent? (lbl : P.Label) (e : P.E) : Prop :=
  lbl.action = .event e

/-- Test that a label is delivered to a particular machine. Mirrors
PVerifier's `TargetsExpr` (`Uclid5CodeGenerator.cs:2051-2052`). -/
@[inline] def Label.targets? (lbl : P.Label) (m : MachineRef) : Prop :=
  lbl.target = m

/-! ## Machine-state predicate -/

/-- `stateOf m s`: the discrete control state of machine `m` in state
`s`. Mirrors PVerifier's `MachineStateAdtSelectState`. -/
@[inline] def stateOf (m : MachineRef) (s : GlobalState P) : P.S :=
  (s.machines m).currentState

/-! ## Temporal precedence operator `≺`

The Phase-1-baked encoding (decision D6 / PLAN.md "Open Design
Problems"): two labels are ordered by their `actionCount`. Each label
gets a unique `actionCount` because every primitive that creates a
label first records the global counter and then bumps it.

The `UniqueActions` invariant (in `Default.lean`) certifies that the
ordering is total over `sent` labels. -/

/-- `a ≺ b`: label `a` was sent before label `b`. Phase 2 adds the
notation `a ≺ b`; for now this is a plain `def`. -/
@[inline] def precedes (a b : P.Label) : Prop :=
  a.actionCount < b.actionCount

end PLean
