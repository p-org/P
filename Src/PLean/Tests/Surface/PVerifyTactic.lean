/-
PLean Phase-3 — `pverify` tactic regression on the simplest M2 case.

Goal: rewrite the `Client.Booting.ePong_correct` triple from
[`Tests/Surface/Phase2PingPong.lean`] using `by pverify` instead of
the manual `unfold; wpgen; intro _ h; exact h` tail.

If this test passes, `pverify` covers the trivial-handler case. The
non-trivial cases (handlers with `send`, default-invariant goals) get
exercised in Phase3DistributedLock and friends.
-/
import PLean
import PLean.Verify.Tactic

open PLean PartialCorrectness DemonicChoice

pmodule PVerifyTacticTest

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

end PVerifyTacticTest

#gen_module PVerifyTacticTest

namespace PVerifyTacticTest

abbrev Prp := PProp Sig
abbrev Lbl := Sig.Label

/-- Same user invariant as M2. -/
def PongAfterPing : Prp := fun s =>
  ∀ q : Lbl, s.sent q = true → q is ePong →
    ∃ p : Lbl, s.sent p = true ∧ p is ePing ∧ p ≺ q

def Inv : Prp := fun s => DefaultInvariants s ∧ PongAfterPing s

/-- The trivial-handler case. `pure ()` doesn't change state, so any
state-indexed predicate is preserved. M2 uses a manual
`unfold; wpgen; intro _ h; exact h` chain. -/
theorem Client.Booting.ePong_correct_manual (this : Client) :
    triple (l := Prp) Inv (Client.Booting.ePong_handler this) (fun _ => Inv) := by
  unfold Client.Booting.ePong_handler
  wpgen
  intro _ h
  exact h

/-- Same triple, discharged via `pverify`. The trivial-handler branch
of the tactic suffices here. -/
theorem Client.Booting.ePong_correct_pverify (this : Client) :
    triple (l := Prp) Inv (Client.Booting.ePong_handler this) (fun _ => Inv) := by
  unfold Client.Booting.ePong_handler Inv
  pverify

end PVerifyTacticTest
