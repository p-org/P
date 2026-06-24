/-
PLean Phase-3 — empirical probe of `sent`/`received`/`machines`
SMT encodings.

The current `GlobalState` shape has `sent : Sig.Label → Bool` (matching
PVerifier's `[Label]boolean` encoding). lean-auto rejects function-typed
record fields with "Higher order input?" so `loom_smt` can't close
obligations over `s.sent` / `(addSent lbl s).sent`.

This file probes three alternative shapes against a representative
obligation a real `#pverify` query produces, to inform whether a
refactor of `GlobalState` is worth doing.

The probe goal mirrors the structural shape of a `prove default ;`
`UniqueActions` leaf:

  ∀ <state> (lbl₀ lbl₁ : Label),
    lbl₀ ≠ lbl₁ →
    sent_after_addSent <state> lbl_new lbl₀ →
    sent_after_addSent <state> lbl_new lbl₁ →
    actionCount lbl₀ ≠ actionCount lbl₁

with the pre-state having the per-label `count < state.actionCount`
guarantee (the M1 / Phase-3 default-invariant precondition). The
per-encoding `sent_after_addSent` definition is what changes.

If `loom_smt` closes this on encoding X, the obligation generator
can rely on SMT for `prove default ;` UniqueActions leaves under X.

Run with: `lake build Tests.Semantics.SmtEncodingProbe`
-/
import Loom.SMT
import Loom.MonadAlgebras.WP.Options
import Mathlib.Tactic

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

namespace PLean.SmtEncodingProbe

/-! ## Setup: a concrete `Label` type with `actionCount` -/

structure Label where
  target      : Nat
  action      : Nat   -- event tag (small int for SMT-friendliness)
  actionCount : Nat
  deriving DecidableEq, Inhabited

/-! ## Encoding 1 — `Label → Bool` (current PLean shape).

Expected: `loom_smt` rejects with "Higher order input?". -/

namespace Enc1

structure GS where
  sent        : Label → Bool
  actionCount : Nat
  deriving Inhabited

def GS.addSent (s : GS) (lbl : Label) : GS :=
  { s with sent := fun l => decide (l = lbl) || s.sent l }

def GS.bumpActionCount (s : GS) : GS := { s with actionCount := s.actionCount + 1 }

/-- The per-label IncreasingCount precondition: every sent label's
`actionCount` is strictly less than the global one. -/
def IncreasingCount (s : GS) : Prop :=
  ∀ a : Label, s.sent a = true → a.actionCount < s.actionCount

/-- The per-pair UniqueActions precondition. -/
def UniqueActions (s : GS) : Prop :=
  ∀ a b : Label, a ≠ b → s.sent a = true → s.sent b = true →
    a.actionCount ≠ b.actionCount

-- EXPECTED RESULT: `loom_smt` rejects with "Higher order input?"
-- because `sent : Label → Bool` is a function-typed *record field*.
-- The `#guard_msgs` below pins the rejection.
/--
error: _private.Auto.Translation.LamFOL2SMT.0.Auto.SMT.lamSort2SSortAux :: Unexpected error. Higher order input?
-/
#guard_msgs in
example (s : GS) (newLbl : Label)
    (hUA : UniqueActions s) (hIC : IncreasingCount s)
    (hNew : newLbl.actionCount = s.actionCount) :
    UniqueActions ((s.addSent newLbl).bumpActionCount) := by
  unfold UniqueActions IncreasingCount at *
  unfold GS.addSent GS.bumpActionCount
  loom_smt [hUA, hIC, hNew]

-- The reference manual proof (M1 recipe), to confirm the goal IS
-- provable via the rcases chain even though SMT can't handle it.
example (s : GS) (newLbl : Label)
    (hUA : UniqueActions s) (hIC : IncreasingCount s)
    (hNew : newLbl.actionCount = s.actionCount) :
    UniqueActions ((s.addSent newLbl).bumpActionCount) := by
  unfold UniqueActions IncreasingCount at *
  unfold GS.addSent GS.bumpActionCount
  intro a b hne ha hb
  simp at ha hb
  rcases ha with rfl | hAprev
  · rcases hb with rfl | hBprev
    · exact (hne rfl).elim
    · intro hEq; rw [hEq] at hNew
      exact absurd (hNew ▸ hIC b hBprev) (Nat.lt_irrefl _)
  · rcases hb with rfl | hBprev
    · intro hEq
      rw [← hEq] at hNew
      exact absurd (hNew ▸ hIC a hAprev) (Nat.lt_irrefl _)
    · exact hUA a b hne hAprev hBprev

end Enc1

/-! ## Encoding 2 — `List Label` (datatype, lean-auto translates as
`(declare-datatypes)`).

Expected: `loom_smt` closes IF the membership-after-cons reasoning
fits SMT-LIB's quantifier handling. The List datatype is well-supported
by cvc5. -/

namespace Enc2

structure GS where
  sent        : List Label
  actionCount : Nat
  deriving Inhabited

def GS.addSent (s : GS) (lbl : Label) : GS :=
  { s with sent := lbl :: s.sent }

def GS.bumpActionCount (s : GS) : GS := { s with actionCount := s.actionCount + 1 }

def IncreasingCount (s : GS) : Prop :=
  ∀ a : Label, a ∈ s.sent → a.actionCount < s.actionCount

def UniqueActions (s : GS) : Prop :=
  ∀ a b : Label, a ≠ b → a ∈ s.sent → b ∈ s.sent →
    a.actionCount ≠ b.actionCount

/-- THE PROBE: same goal, List encoding. -/
example (s : GS) (newLbl : Label)
    (hUA : UniqueActions s) (hIC : IncreasingCount s)
    (hNew : newLbl.actionCount = s.actionCount) :
    UniqueActions ((s.addSent newLbl).bumpActionCount) := by
  -- TRY SMT.
  unfold UniqueActions at *
  unfold IncreasingCount at *
  unfold GS.addSent GS.bumpActionCount
  simp only [List.mem_cons]
  -- Now the goal is over List.Mem expanded into ∨ of equalities.
  -- See whether loom_smt closes it.
  loom_smt [hUA, hIC, hNew]

end Enc2

/-! ## Encoding 3 — Top-level uninterpreted predicate
(`opaque sent : World → Label → Bool`).

The `World` type is opaque (uninterpreted sort). `sent` is an
uninterpreted top-level function. lean-auto declares
`(declare-fun sent (World Label) Bool)` and SMT can reason via
its UF theory + the user's axioms. -/

namespace Enc3

/-- `World` is just `Unit` underneath; the encoding doesn't care about
its actual structure. -/
def World : Type := Unit
instance : Inhabited World := ⟨()⟩

opaque sent  : World → Label → Bool
opaque actionCount : World → Nat
opaque addSent : World → Label → World
opaque bumpActionCount : World → World

axiom addSent_sent (w : World) (lbl l : Label) :
  sent (addSent w lbl) l = (decide (l = lbl) || sent w l)
axiom addSent_actionCount (w : World) (lbl : Label) :
  actionCount (addSent w lbl) = actionCount w
axiom bumpActionCount_actionCount (w : World) :
  actionCount (bumpActionCount w) = actionCount w + 1
axiom bumpActionCount_sent (w : World) (l : Label) :
  sent (bumpActionCount w) l = sent w l

def IncreasingCount (w : World) : Prop :=
  ∀ a : Label, sent w a = true → a.actionCount < actionCount w

def UniqueActions (w : World) : Prop :=
  ∀ a b : Label, a ≠ b → sent w a = true → sent w b = true →
    a.actionCount ≠ b.actionCount

example (w : World) (newLbl : Label)
    (hUA : UniqueActions w) (hIC : IncreasingCount w)
    (hNew : newLbl.actionCount = actionCount w) :
    UniqueActions (bumpActionCount (addSent w newLbl)) := by
  unfold UniqueActions IncreasingCount at *
  -- Hand-rewrite the post-state via the axioms first.
  loom_smt [hUA, hIC, hNew, addSent_sent, addSent_actionCount,
            bumpActionCount_actionCount, bumpActionCount_sent]

end Enc3

/-! ## Encoding 4a — `machines` as `List (MachineRef × MachineState)`.

Result: `loom_smt` rejects on the `List.filter` lambda inside
`updateMachine`. List works for `sent`/`received` (set-like, only
membership matters) but NOT for `machines` (map-like, need keyed
lookup, which involves `find?`/`filter` lambdas lean-auto can't
translate).

We don't include the failing example here; it's been confirmed
empirically. -/

/-! ## Encoding 4b — `machines` kept as `MachineRef → MachineState`
(function-typed field), but only *that one field* is function-valued.
`sent` / `received` are List. The rest of the state is plain. -/

namespace Enc4b

structure MachineState where
  held        : Bool
  epoch       : Int
  deriving Inhabited, DecidableEq

structure GS where
  sent        : List Label
  received    : List Label
  machines    : Nat → MachineState   -- function-typed field — bad for SMT
  actionCount : Nat

instance : Inhabited GS where
  default := { sent := [], received := [], machines := fun _ => default, actionCount := 0 }

def GS.addSent (s : GS) (lbl : Label) : GS :=
  { s with sent := lbl :: s.sent }

def GS.updateMachine (s : GS) (m : Nat) (ms : MachineState) : GS :=
  { s with machines := fun r => if r = m then ms else s.machines r }

def UniqueHolder (s : GS) : Prop :=
  ∀ n1 n2 : Nat, (s.machines n1).held = true → (s.machines n2).held = true → n1 = n2

-- THE PROBE: post-`updateMachine` UniqueHolder. Same shape as Enc4a;
-- the only change is `machines` shape. EXPECTED FAILURE: same
-- "Higher order input?" rejection that Enc1 had — `machines : Nat →
-- MachineState` as a record field.
/--
error: _private.Auto.Translation.LamFOL2SMT.0.Auto.SMT.lamSort2SSortAux :: Unexpected error. Higher order input?
-/
#guard_msgs in
example (s : GS) (n : Nat) (newEpoch : Int)
    (hUH : UniqueHolder s) :
    UniqueHolder (s.updateMachine n {held := false, epoch := newEpoch}) := by
  unfold UniqueHolder GS.updateMachine at *
  loom_smt [hUH]

end Enc4b

/-! ## Encoding 4c — `machines` as `List (Ref × MachineState)` but
with `lookup` defined recursively (no `find?`/`filter` lambdas), and
`updateMachine` defined recursively. The hope: lean-auto can translate
recursion-by-cases over `List` to a recursive SMT function. -/

namespace Enc4c

structure MachineState where
  held        : Bool
  epoch       : Int
  deriving Inhabited, DecidableEq

structure GS where
  sent        : List Label
  received    : List Label
  machines    : List (Nat × MachineState)
  actionCount : Nat
  deriving Inhabited

/-- Recursive lookup, no lambdas. -/
def GS.lookup : List (Nat × MachineState) → Nat → MachineState
  | [], _ => default
  | (r, ms) :: rest, m => if r = m then ms else GS.lookup rest m

def GS.updateMachine : List (Nat × MachineState) → Nat → MachineState → List (Nat × MachineState)
  | [], m, ms => [(m, ms)]
  | (r, msOld) :: rest, m, ms =>
      if r = m then (m, ms) :: rest
      else (r, msOld) :: GS.updateMachine rest m ms

/-- Equation lemma for empty case. -/
@[simp] theorem GS.lookup_nil (m : Nat) :
    GS.lookup [] m = default := rfl

/-- Equation lemma for cons case. -/
@[simp] theorem GS.lookup_cons (r : Nat) (ms : MachineState)
    (rest : List (Nat × MachineState)) (m : Nat) :
    GS.lookup ((r, ms) :: rest) m = if r = m then ms else GS.lookup rest m := rfl

@[simp] theorem GS.updateMachine_nil (m : Nat) (ms : MachineState) :
    GS.updateMachine [] m ms = [(m, ms)] := rfl

@[simp] theorem GS.updateMachine_cons (r : Nat) (msOld : MachineState)
    (rest : List (Nat × MachineState)) (m : Nat) (ms : MachineState) :
    GS.updateMachine ((r, msOld) :: rest) m ms =
      (if r = m then (m, ms) :: rest
       else (r, msOld) :: GS.updateMachine rest m ms) := rfl

def UniqueHolder (s : GS) : Prop :=
  ∀ n1 n2 : Nat, (GS.lookup s.machines n1).held = true →
    (GS.lookup s.machines n2).held = true → n1 = n2

/-
Result: lean-auto translates the problem (no "Higher order input?"
crash). z3 returns `unknown` — the recursive list traversal needs
either more time or solver-side induction support. cvc5 may do better;
the `loom_solver` option already pins us to cvc5 above. The takeaway
for the design: encoding `machines` as `List (Ref × State)` with
recursive lookup *gets through lean-auto's monomorphizer*, but the
solver may still time out on traversal-heavy goals. Manual proofs
remain a fallback for these cases.
-/
example (s : GS) (n : Nat) (newEpoch : Int)
    (hUH : UniqueHolder s) :
    UniqueHolder { s with
      machines := GS.updateMachine s.machines n {held := false, epoch := newEpoch} } := by
  unfold UniqueHolder at *
  -- The `loom_smt` line below times out / returns `unknown` on the
  -- bundled solver. Commented so the file builds; uncomment to
  -- see the result.
  -- loom_smt [hUH, GS.lookup_nil, GS.lookup_cons,
  --           GS.updateMachine_nil, GS.updateMachine_cons]
  sorry

end Enc4c

/-! ## Encoding 5 — `funextEq` + struct destruct.

Keep state as a `structure` with function-typed fields (matching
PLean's `GlobalState`) and preprocess goals with a `funextEq` rewrite
`f = g  ↔  ∀ x, f x = g x` so function values never appear as SMT
atoms — only their applied form does.

Combined with the structure's `mk.injEq` (which rewrites `s' = s`
into per-field equalities), this transforms goals about updated
state records into pointwise quantified equalities, which lean-auto
CAN translate (functions appear only in declared symbols, not as
sort-level types).

PROBE: take Encoding 1's failing goal and try the same approach. -/

namespace Enc5

-- Same structure as Enc1 (function-typed `sent` field).
structure GS where
  sent        : Label → Bool
  actionCount : Nat
  deriving Inhabited

def GS.addSent (s : GS) (lbl : Label) : GS :=
  { s with sent := fun l => decide (l = lbl) || s.sent l }

def GS.bumpActionCount (s : GS) : GS := { s with actionCount := s.actionCount + 1 }

def IncreasingCount (s : GS) : Prop :=
  ∀ a : Label, s.sent a = true → a.actionCount < s.actionCount

def UniqueActions (s : GS) : Prop :=
  ∀ a b : Label, a ≠ b → s.sent a = true → s.sent b = true →
    a.actionCount ≠ b.actionCount

/-- Function-extensional equality: `(f = g) ↔ ∀ x, f x = g x`. -/
theorem funextEq_label {α : Type} (f g : Label → α) :
    (f = g) = ∀ x, f x = g x := by
  apply propext
  exact ⟨fun h => fun x => h ▸ rfl, fun h => funext h⟩

/-- THE PROBE: try to **destruct the GS struct** before SMT (the
`sdestruct` + `mk.injEq` pattern). After destruct, `s` is replaced
by its components `(sent, actionCount)`, and the function-typed
field is just a free variable. -/
example (s : GS) (newLbl : Label)
    (hUA : UniqueActions s) (hIC : IncreasingCount s)
    (hNew : newLbl.actionCount = s.actionCount) :
    UniqueActions ((s.addSent newLbl).bumpActionCount) := by
  unfold UniqueActions IncreasingCount at *
  unfold GS.addSent GS.bumpActionCount
  -- Destruct `s` so the function-typed field becomes a free variable
  -- rather than a struct projection (the `sdestruct` pattern).
  obtain ⟨s_sent, s_count⟩ := s
  intro a b ha hb
  loom_smt [hUA, hIC, hNew, ha, hb]

end Enc5

end PLean.SmtEncodingProbe
