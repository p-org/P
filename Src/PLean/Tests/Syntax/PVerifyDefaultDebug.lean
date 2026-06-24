/-
Phase-3 debug — what does the default obligation look like just
before `loom_smt`/`grind` get it?

This file mirrors the obligation generator's emission shape on a
small `send`-bearing handler. We want to confirm:

  1. After `wpgen <;> WPGen.default`, the goal is over raw `wp` plus
     `WPGen.default (liftM get)`-style residuals.
  2. The bare `simp [wp_bind, wp_pure, StateT.wp_get, StateT.wp_set]`
     pattern (M1's recipe) walks the residual.
  3. The final `default-invariant` post is closable by the
     `simp + rcases + Nat.lt_succ_*` shape M1 hand-wrote.

When (3) closes by hand, we know the auto path needs the same simp
set. The Tactic.lean update wires it through `pverify_step_wp`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule PVerifyDefaultDebug

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

end PVerifyDefaultDebug

#gen_module PVerifyDefaultDebug

namespace PVerifyDefaultDebug

/-- M1-style proof shape on a default-only obligation. If this
closes, the recipe `unfold + wpgen + WPGen.default + intro + simp +
rcases` is what `pverify_default` needs to embody. -/
example (this : Server) (param : ePing_payload) :
    triple (l := PProp Sig)
      (fun s =>
        DefaultInvariants s ∧
        ∃ lbl : Sig.Label,
          PLean.inflight lbl s ∧
          lbl.target = this.ref ∧
          lbl.action = .event (E.ePing param))
      (Server.Idle.ePing_handler this param)
      (fun _ s => DefaultInvariants s) := by
  unfold Server.Idle.ePing_handler PLean.send DefaultInvariants
    UniqueActions IncreasingCount ReceivedSubsetSent PLean.inflight
  wpgen <;> first | apply WPGen.default | skip
  intro s ⟨⟨hUA, hIC, hRS⟩, lbl, ⟨hSent, _hNotRecv⟩, _hTgt, _hAct⟩
  refine ⟨?_, ?_, ?_⟩
  · -- UniqueActions
    intro a b hne ha hb
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha hb
    rcases ha with rfl | hAprev
    · rcases hb with rfl | hBprev
      · exact (hne rfl).elim
      · intro hEq; exact absurd (hEq ▸ hIC b hBprev) (Nat.lt_irrefl _)
    · rcases hb with rfl | hBprev
      · intro hEq; exact absurd (hEq.symm ▸ hIC a hAprev) (Nat.lt_irrefl _)
      · exact hUA a b hne hAprev hBprev
  · -- IncreasingCount
    intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount] at ha
    rcases ha with rfl | hPrev
    · exact Nat.lt_succ_self _
    · exact Nat.lt_succ_of_lt (hIC a hPrev)
  · -- ReceivedSubsetSent
    intro a ha
    simp [GlobalState.addSent, GlobalState.bumpActionCount]
    exact Or.inr (hRS a ha)

/-- Trace the goal at each step of `pverify_default`. The simp set
in `pverify_step_wp` curries the `∧` on the precondition, so we
need to consume the dispatcher-contract clause separately. -/
example (this : Server) (param : ePing_payload) :
    triple (l := PProp Sig)
      (fun s =>
        DefaultInvariants s ∧
        ∃ lbl : Sig.Label,
          PLean.inflight lbl s ∧
          lbl.target = this.ref ∧
          lbl.action = .event (E.ePing param))
      (Server.Idle.ePing_handler this param)
      (fun _ s => DefaultInvariants s) := by
  unfold Server.Idle.ePing_handler PLean.send
  pverify_step_wp
  intro s ⟨hUA, hIC, hRS⟩ ⟨lbl, _hSent, _hTgt, _hAct⟩
  -- Use `default_inv` to close the default-invariant conjunction.
  default_inv

example (this : Server) (param : ePing_payload) :
    triple (l := PProp Sig)
      (fun s =>
        DefaultInvariants s ∧
        ∃ lbl : Sig.Label,
          PLean.inflight lbl s ∧
          lbl.target = this.ref ∧
          (s.machines this.ref).currentState = Idle_st ∧
          lbl.action = .event (E.ePing param))
      (Server.Idle.ePing_handler this param)
      (fun _ s => DefaultInvariants s) := by
  unfold Server.Idle.ePing_handler PLean.send
  pverify_step_wp
  intro s ⟨hUA, hIC, hRS⟩ ⟨lbl, _hSent, _hTgt, _hAct⟩
  default_inv

end PVerifyDefaultDebug
