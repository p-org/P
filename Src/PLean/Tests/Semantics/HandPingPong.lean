/-
PLean Phase-1 M1 — hand-written ping-pong, no surface macros.

A 2-machine ping-pong written directly as Lean defs over the real
`PM`, with four handler triples that discharge end-to-end. Validates
the full Phase-1 stack before any Phase-2 macro work.

Exercises:
- `ProgramSig` with concrete `Event`/`Goto`/`State`/`Fields` unions;
- `NonDetT (StateT _ DivM)` after `open PartialCorrectness DemonicChoice`;
- the buffer-update primitives (`send`/`markReceived`);
- the temporal precedence operator `≺` (= `precedes`);
- per-handler Hoare triples discharged with raw Loom primitives
  (`#derive_lifted_wp` for `get`/`set`, then `wpgen` + manual cleanup).

D3 (PLAN_P1): we discharge by `wpgen` then explicit reasoning, *not* by
`loom_solve` (which lives in CaseStudies). PLean ships its own
`loom_solve`-equivalent in Phase 3.
-/
import PLean.Semantics.Monad
import PLean.Semantics.Primitives
import PLean.Semantics.Predicates
import PLean.Semantics.Default
import Loom.Meta

namespace PLean.PingPongM1

/-! ## Program signature -/

inductive Ev where
  | ePing (replyTo : MachineRef)
  | ePong
  deriving Inhabited, DecidableEq, Repr

inductive GotoP where
  | unit
  deriving Inhabited, DecidableEq, Repr

inductive St where
  | ServerIdle
  | ClientBooting
  deriving Inhabited, DecidableEq, Repr

abbrev Fields : Type := Unit

/-- `abbrev` so `Sig.E = Ev` reduces — `DecidableEq Sig.E` then
synthesises from `Ev`'s `deriving` clause without manual instances. -/
abbrev Sig : ProgramSig :=
  { E := Ev, G := GotoP, S := St, F := Fields }

/-- The concrete monad for this program. `abbrev` so the
`#derive_lifted_wp` `as` clause (which expects an ident) can refer
to it. -/
abbrev M' (α : Type) := PM Sig α

abbrev Prp := PProp Sig
abbrev Lbl := Sig.Label

/-- Concrete output type for `get`. `#derive_lifted_wp` expects an
ident in its `as`-clause's output position. -/
abbrev GS := GlobalState Sig

def serverRef : MachineRef := 0
def clientRef : MachineRef := 1

/-! ## WP specs for `get`/`set` lifted into `M'`

Loom's `#derive_lifted_wp` (in `Loom/Meta.lean`) registers `loomSpec`
lemmas so `wpgen` can step through `liftM (get : StateT _ DivM _)` and
`liftM (set _)`. Without these the body of any handler that reads or
writes state is opaque to `wpgen`.

When Phase 2's `#gen_module` synthesises per-program type aliases, it
will also emit these declarations automatically. -/

open PartialCorrectness DemonicChoice

#derive_lifted_wp for
  (get : StateT GS DivM GS)
  as M' GS

#derive_lifted_wp (s : GS) for
  (set s : StateT GS DivM PUnit)
  as M' PUnit

/-! ## Default starting state -/

def initialState : GlobalState Sig :=
  GlobalState.initial' (P := Sig) fun r =>
    if r = serverRef then
      { stage := true, currentState := St.ServerIdle, fields := () }
    else
      { stage := true, currentState := St.ClientBooting, fields := () }

/-! ## Handlers -/

def Server.Idle.entry (_this : MachineRef) : M' Unit := pure ()

def Server.Idle.ePing_handler
    (this : MachineRef) (replyTo : MachineRef) (lbl : Lbl) : M' Unit := do
  let _ := this
  markReceived (P := Sig) lbl
  send (P := Sig) replyTo Ev.ePong

def Client.Booting.entry (this : MachineRef) : M' Unit := do
  send (P := Sig) serverRef (Ev.ePing this)

def Client.Booting.ePong_handler
    (_this : MachineRef) (lbl : Lbl) : M' Unit :=
  markReceived (P := Sig) lbl

/-! ## User invariant: every `ePong` is preceded by some `ePing` -/

def PongAfterPing : Prp := fun s =>
  ∀ q : Lbl, s.sent q = true → q.action = .event Ev.ePong →
    ∃ p : Lbl, s.sent p = true ∧
      (∃ r, p.action = .event (Ev.ePing r)) ∧ precedes p q

/-- The full invariant: defaults plus the user's temporal claim. -/
def Inv : Prp := fun s =>
  DefaultInvariants s ∧ PongAfterPing s

/-! ## The four handler triples

Each lemma's shape mirrors the per-handler obligation Phase 3 will
synthesise from the registry. -/

/-- Handler 1: `Server.Idle.entry` is `pure ()`; trivially preserves
the invariant. -/
theorem Server.Idle.entry_correct (this : MachineRef) :
    triple (l := Prp) Inv (Server.Idle.entry this) (fun _ => Inv) := by
  unfold Server.Idle.entry
  wpgen
  intro _ h
  exact h

/-- Handler 4: `Client.Booting.ePong_handler` only marks `lbl`
received. Preserves the invariant given the standard precondition that
`lbl` is in the sent set (the dispatcher's trigger condition). -/
theorem Client.Booting.ePong_handler_correct (this : MachineRef) (lbl : Lbl) :
    triple (l := Prp)
      (fun s => Inv s ∧ s.sent lbl = true)
      (Client.Booting.ePong_handler this lbl)
      (fun _ => Inv) := by
  unfold Client.Booting.ePong_handler markReceived Inv DefaultInvariants
    UniqueActions IncreasingCount ReceivedSubsetSent PongAfterPing
  -- `wpgen` chains `WPGen.bind`/`WPGen.pure` and the `#derive_lifted_wp`
  -- specs above; any residual `WPGen (liftM …)` not in the discrTree
  -- gets the universal fallback `WPGen.default`.
  wpgen <;> first | apply WPGen.default | skip
  intro s ⟨⟨⟨hUA, hIC, hRS⟩, hPP⟩, hSent⟩
  refine ⟨⟨?_, ?_, ?_⟩, ?_⟩
  · -- UniqueActions: `sent` unchanged.
    intro a b hne ha hb; exact hUA a b hne ha hb
  · -- IncreasingCount: `sent`/`actionCount` unchanged.
    intro a ha; exact hIC a ha
  · -- ReceivedSubsetSent: new `received` ⊆ old `received` ∪ {lbl} ⊆ `sent`.
    intro a ha
    simp [GlobalState.addReceived] at ha
    rcases ha with rfl | hPrev
    · exact hSent
    · exact hRS a hPrev
  · -- PongAfterPing: `sent` unchanged.
    intro q hq hev; exact hPP q hq hev

/-- Handler 2: `Server.Idle.ePing_handler`. The interesting one — sends
a fresh `ePong` and we must witness a preceding `ePing`. The witness is
the incoming `lbl`; its `actionCount` precedes the new pong's by
`IncreasingCount`. -/
theorem Server.Idle.ePing_handler_correct
    (this replyTo : MachineRef) (lbl : Lbl) :
    triple (l := Prp)
      (fun s =>
        Inv s ∧ inflight lbl s ∧ lbl.target = this ∧
        stateOf this s = St.ServerIdle ∧
        lbl.action = .event (Ev.ePing replyTo))
      (Server.Idle.ePing_handler this replyTo lbl)
      (fun _ => Inv) := by
  unfold Server.Idle.ePing_handler markReceived send Inv DefaultInvariants
    UniqueActions IncreasingCount ReceivedSubsetSent PongAfterPing
    inflight precedes
  -- `wpgen` chains `WPGen.bind`/`WPGen.pure` and the `#derive_lifted_wp`
  -- specs above; any residual `WPGen (liftM …)` not in the discrTree
  -- gets the universal fallback `WPGen.default`.
  wpgen <;> first | apply WPGen.default | skip
  intro s ⟨⟨⟨hUA, hIC, hRS⟩, hPP⟩, ⟨hSent, _hNotRecv⟩, _hTgt, _hSt, hAct⟩
  refine ⟨⟨?_, ?_, ?_⟩, ?_⟩
  · -- UniqueActions: pre-existing pairs from hUA; new label vs.
    -- pre-existing has count `s.actionCount` vs. some `< s.actionCount`
    -- (by hIC), so they're unequal.
    intro a b hne ha hb
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha hb
    rcases ha with rfl | hAprev
    · rcases hb with rfl | hBprev
      · exact (hne rfl).elim
      · intro hEq; exact absurd (hEq ▸ hIC b hBprev) (Nat.lt_irrefl _)
    · rcases hb with rfl | hBprev
      · intro hEq; exact absurd (hEq.symm ▸ hIC a hAprev) (Nat.lt_irrefl _)
      · exact hUA a b hne hAprev hBprev
  · -- IncreasingCount: every sent label's count < new actionCount = old + 1.
    intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha
    rcases ha with rfl | hPrev
    · exact Nat.lt_succ_self _
    · exact Nat.lt_succ_of_lt (hIC a hPrev)
  · -- ReceivedSubsetSent.
    intro a ha
    simp [GlobalState.addReceived, GlobalState.addSent,
      GlobalState.bumpActionCount] at ha ⊢
    rcases ha with rfl | hPrev
    · exact Or.inr hSent
    · exact Or.inr (hRS a hPrev)
  · -- PongAfterPing in the post-state.
    intro q hq hev
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at hq
    rcases hq with rfl | hQprev
    · -- q is the freshly-sent pong. Witness: `lbl` is the prior ePing.
      refine ⟨lbl, ?_, ⟨replyTo, hAct⟩, ?_⟩
      · simp [GlobalState.addSent, GlobalState.bumpActionCount,
          GlobalState.addReceived]
        exact Or.inr hSent
      · exact hIC lbl hSent
    · obtain ⟨p, hSP, hPing, hPrec⟩ := hPP q hQprev hev
      refine ⟨p, ?_, hPing, hPrec⟩
      simp [GlobalState.addSent, GlobalState.bumpActionCount,
        GlobalState.addReceived]
      exact Or.inr hSP

/-- Handler 3: `Client.Booting.entry`. Sends an `ePing`; preserves the
invariant trivially because no new `ePong` was created. -/
theorem Client.Booting.entry_correct (this : MachineRef) :
    triple (l := Prp)
      Inv
      (Client.Booting.entry this)
      (fun _ => Inv) := by
  unfold Client.Booting.entry send Inv DefaultInvariants
    UniqueActions IncreasingCount ReceivedSubsetSent PongAfterPing precedes
  -- `wpgen` chains `WPGen.bind`/`WPGen.pure` and the `#derive_lifted_wp`
  -- specs above; any residual `WPGen (liftM …)` not in the discrTree
  -- gets the universal fallback `WPGen.default`.
  wpgen <;> first | apply WPGen.default | skip
  intro s ⟨⟨hUA, hIC, hRS⟩, hPP⟩
  refine ⟨⟨?_, ?_, ?_⟩, ?_⟩
  · intro a b hne ha hb
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha hb
    rcases ha with rfl | hAprev
    · rcases hb with rfl | hBprev
      · exact (hne rfl).elim
      · intro hEq; exact absurd (hEq ▸ hIC b hBprev) (Nat.lt_irrefl _)
    · rcases hb with rfl | hBprev
      · intro hEq; exact absurd (hEq.symm ▸ hIC a hAprev) (Nat.lt_irrefl _)
      · exact hUA a b hne hAprev hBprev
  · intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha
    rcases ha with rfl | hPrev
    · exact Nat.lt_succ_self _
    · exact Nat.lt_succ_of_lt (hIC a hPrev)
  · intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount]
    exact Or.inr (hRS a ha)
  · intro q hq hev
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at hq
    rcases hq with rfl | hQprev
    · -- q is the new ePing label, but hev says it's an ePong. Contra.
      simp at hev
    · obtain ⟨p, hSP, hPing, hPrec⟩ := hPP q hQprev hev
      refine ⟨p, ?_, hPing, hPrec⟩
      simp [GlobalState.addSent, GlobalState.bumpActionCount]
      exact Or.inr hSP

end PLean.PingPongM1
