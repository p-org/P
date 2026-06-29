/-
Surface-syntax ping-pong with hand-written `theorem ..._correct`
proofs. Same `pmodule` shape as `PingPongAuto`; the two handler
triples are stated in the exact shape `#pverify` auto-emits (this and
lbl quantified, dispatcher contract conjoined in the precondition,
executed program is `markReceived lbl >>= handler …`) and discharged
manually rather than via the `pverify` tactic.
-/
import PLean
import Loom.Meta

open PLean PartialCorrectness DemonicChoice

pmodule PingPongManual

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
      ignore ePong
    }
  }

end PingPongManual

#gen_module PingPongManual
#pwf        PingPongManual

namespace PingPongManual

abbrev Prp := PProp Sig
abbrev Lbl := Sig.Label

/-- Every `ePong` is preceded by some `ePing`. State-indexed; uses the
`≺` and `is` notations from `Syntax/Notation.lean`. -/
def PongAfterPing : Prp := fun s =>
  ∀ q : Lbl, s.sent q = true → q is ePong →
    ∃ p : Lbl, s.sent p = true ∧ p is ePing ∧ p ≺ q

/-- The full invariant we preserve at every handler boundary. -/
def Inv : Prp := fun s => DefaultInvariants s ∧ PongAfterPing s

/-! ### Handler 1: `Server.Idle.ePing_handler` — sends a fresh `ePong`. -/
theorem Server.Idle.ePing_correct
    (this : Server) (replyTo : ePing_payload) (lbl : Lbl) :
    triple (l := Prp)
      (fun s =>
        Inv s ∧
        inflight lbl s ∧
        lbl.target = this.ref ∧
        is_Server this.ref s ∧
        (s.machines this.ref).currentState = Server.Idle_st ∧
        lbl.action = .event (E.ePing replyTo))
      (do PLean.markReceived (P := Sig) lbl; Server.Idle.ePing_handler this replyTo)
      (fun _ s => Inv s) := by
  unfold Server.Idle.ePing_handler
  pverify_step_wp
  intro s hInv hInflight _hTgt _hKind _hState hAction
  have hSentLbl : s.sent lbl = true := hInflight.1
  have hLblIsPing : is_ePing lbl := by simp [is_ePing, hAction]
  have hLblLt : lbl.actionCount < s.actionCount := hInv.1.2.1 lbl hSentLbl
  unfold Inv DefaultInvariants PongAfterPing
    UniqueActions IncreasingCount ReceivedSubsetSent at hInv ⊢
  obtain ⟨⟨hUA, hIC, hRS⟩, hPP⟩ := hInv
  refine ⟨⟨?_, ?_, ?_⟩, ?_⟩
  · intro a b hne ha hb
    simp at ha hb
    rcases ha with rfl | hAprev
    · rcases hb with rfl | hBprev
      · exact (hne rfl).elim
      · intro hEq; exact absurd (hEq ▸ hIC b hBprev) (Nat.lt_irrefl _)
    · rcases hb with rfl | hBprev
      · intro hEq; exact absurd (hEq.symm ▸ hIC a hAprev) (Nat.lt_irrefl _)
      · exact hUA a b hne hAprev hBprev
  · intro a ha
    simp at ha
    rcases ha with rfl | hPrev
    · exact Nat.lt_succ_self _
    · exact Nat.lt_succ_of_lt (hIC a hPrev)
  · intro a ha
    simp at ha ⊢
    rcases ha with hNewRcv | hOldRcv
    · subst hNewRcv
      exact Or.inr hSentLbl
    · exact Or.inr (hRS a hOldRcv)
  · intro q hq hev
    simp at hq
    rcases hq with hNew | hQprev
    · refine ⟨lbl, ?_, hLblIsPing, ?_⟩
      · simp [hSentLbl]
      · subst hNew
        exact hLblLt
    · obtain ⟨p, hSP, hPIs, hPrec⟩ := hPP q hQprev hev
      refine ⟨p, ?_, hPIs, hPrec⟩
      simp [hSP]

end PingPongManual
