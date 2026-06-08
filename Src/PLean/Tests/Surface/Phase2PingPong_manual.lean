/-
PLean Phase-2 M2 — surface-syntax ping-pong, verifies.

Re-expresses the M1 hand-written ping-pong (`Tests/Semantics/HandPingPong.lean`)
in PLean surface syntax. Two handler triples discharge with the same
`wpgen` + manual-tail proofs M1 used.

Differences from M1:
- The handler bodies come from `#gen_module` macro expansion, not hand-written
  Lean defs. They use the per-pmodule `Sig` / `PM'` / `GS` synthesised from
  the registry (decision D8) and the wrapper-struct `this` (decision D10/D11).
- The `≺` notation lives in `PLean.Surface.Notation` (decision D16); we open
  the namespace to use it inside the user invariant.
- `markReceived` is *not* part of the surface-emitted handler bodies — the
  dispatcher is responsible for it in production, and the M2 proofs are
  shaped to not depend on it. This is the intentional surface↔M1 difference.

What this test proves: surface-syntax pmodules verify against the same
backend as Phase-1 hand-written examples. Phase-3's obligation generator
will synthesise the `theorem ..._correct` lemmas mechanically from the
registry; for now they're hand-written and mirror the M1 shape modulo
markReceived elision and wrapper-struct `this`.
-/
import PLean
import Loom.Meta

open PLean PartialCorrectness DemonicChoice

pmodule Phase2PingPongManual

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting {
      on ePong { pure () }
    }
  }

end Phase2PingPongManual

#gen_module Phase2PingPongManual
#pwf        Phase2PingPongManual

/-! ## Verifying the two handler triples

The proofs mirror `HandPingPong.lean` line-for-line modulo (a) names from
the synthesised `Sig` / `E` ctors, and (b) the wrapper-struct `this`. -/

namespace Phase2PingPongManual

abbrev Prp := PProp Sig
abbrev Lbl := Sig.Label

/-- The user invariant: every `ePong` is preceded by some `ePing`. Stated
directly as a `Prp` (= `GS → Prop`) since handler triples need a state-
indexed predicate, not the closed `∀ s, …` form a user `invariant`
declaration would emit. Uses the `≺` and `is` notations from
`Surface/Notation.lean` (decisions D6/D16) — the headline P-style
syntax PVerifier cannot express. -/
def PongAfterPing : Prp := fun s =>
  ∀ q : Lbl, s.sent q = true → q is ePong →
    ∃ p : Lbl, s.sent p = true ∧ p is ePing ∧ p ≺ q

/-- The full invariant we preserve at every handler boundary. -/
def Inv : Prp := fun s => DefaultInvariants s ∧ PongAfterPing s

/-! ### Handler 1: `Server.Idle.ePing_handler`

The interesting one — sends a fresh `ePong`, and to preserve
`PongAfterPing` in the post-state we must witness a preceding `ePing`.

**Pre-condition shape (note vs. M1).** The triple is *not* `Inv → Inv`:
the precondition adds the *dispatcher contract* — the framework's
runtime guarantee that this handler is only fired when an inflight
`ePing` label exists targeted at this machine. M1 carries this contract
via its `lbl : Lbl` parameter and explicit precondition clauses
(`inflight lbl s ∧ lbl.action = .event (Ev.ePing replyTo)`); M2's
surface-emitted handler signature drops `lbl` (the surface user
doesn't name the dispatched label), so we existentially-quantify the
witness in the precondition instead. **Without this clause `Inv` is
too weak** — the empty state satisfies `Inv` vacuously, and a buggy
dispatcher firing `ePing_handler` from there would let a stray ePong
break `PongAfterPing`.

The asymmetric pre/post is the same shape PVerifier emits per-handler
(see `Uclid5CodeGenerator.cs` "match conditions"); Phase 3's obligation
generator will synthesise it from the registry so the user never writes
it by hand. -/
theorem Server.Idle.ePing_correct
    (this : Server) (replyTo : PLean.MachineRef) :
    triple (l := Prp)
      (fun s =>
        Inv s ∧
        ∃ p : Lbl, s.sent p = true ∧ p is ePing ∧
          p.actionCount < s.actionCount)
      (Server.Idle.ePing_handler this replyTo)
      (fun _ => Inv) := by
  unfold Server.Idle.ePing_handler Inv PongAfterPing
    DefaultInvariants UniqueActions IncreasingCount ReceivedSubsetSent
    PLean.send precedes is_ePing is_ePong
  -- `wpgen` chains `WPGen.bind`/`WPGen.pure` and the auto-emitted
  -- `#derive_lifted_wp` specs from `#gen_module`; any residual
  -- `WPGen (liftM …)` not in the discrTree gets `WPGen.default`.
  wpgen <;> first | apply WPGen.default | skip
  intro s ⟨⟨⟨hUA, hIC, hRS⟩, hPP⟩, ⟨pPing, hPSent, hPIs, hPLt⟩⟩
  refine ⟨⟨?_, ?_, ?_⟩, ?_⟩
  · -- UniqueActions: pre-existing pairs come from hUA; new label vs.
    -- pre-existing has count `s.actionCount` vs. some `< s.actionCount`,
    -- so they're unequal.
    intro a b hne ha hb
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha hb
    rcases ha with rfl | hAprev
    · rcases hb with rfl | hBprev
      · exact (hne rfl).elim
      · intro hEq; exact absurd (hEq ▸ hIC b hBprev) (Nat.lt_irrefl _)
    · rcases hb with rfl | hBprev
      · intro hEq; exact absurd (hEq.symm ▸ hIC a hAprev) (Nat.lt_irrefl _)
      · exact hUA a b hne hAprev hBprev
  · -- IncreasingCount.
    intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha
    rcases ha with rfl | hPrev
    · exact Nat.lt_succ_self _
    · exact Nat.lt_succ_of_lt (hIC a hPrev)
  · -- ReceivedSubsetSent: handler body doesn't touch `received`.
    intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount]
    exact Or.inr (hRS a ha)
  · -- PongAfterPing in the post-state.
    intro q hq hev
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at hq
    rcases hq with rfl | hQprev
    · -- q is the freshly-sent pong. Witness: the precondition's `pPing`.
      refine ⟨pPing, ?_, hPIs, ?_⟩
      · simp [GlobalState.addSent, GlobalState.bumpActionCount]
        exact Or.inr hPSent
      · exact hPLt
    · obtain ⟨p, hSP, hPIs, hPrec⟩ := hPP q hQprev hev
      refine ⟨p, ?_, hPIs, hPrec⟩
      simp [GlobalState.addSent, GlobalState.bumpActionCount]
      exact Or.inr hSP

/-! ### Handler 2: `Client.Booting.ePong_handler` is `pure ()`

Trivially preserves the invariant. -/
theorem Client.Booting.ePong_correct (this : Client) :
    triple (l := Prp) Inv (Client.Booting.ePong_handler this) (fun _ => Inv) := by
  unfold Client.Booting.ePong_handler
  wpgen
  intro _ h
  exact h

end Phase2PingPongManual
