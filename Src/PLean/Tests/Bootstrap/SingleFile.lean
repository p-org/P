/-
Single-file pmodule registration test.

Confirms a self-contained `pmodule` (events + machine + invariant +
pinstance) compiles and `#pwf` reports clean.
-/
import PLean

class TotalOrder (t : Type) where
  le : t → t → Prop
  le_refl : ∀ x, le x x
  le_trans : ∀ x y z, le x y → le y z → le x z
  le_total : ∀ x y, le x y ∨ le y x

pmodule SelfContained

  type Round
  enum Phase { Prepare, Commit }
  type Msg = (round : Nat, phase : Phase)

  event eMsg : Msg

  machine Worker {
    var counter : Nat

    start state Idle {
      entry {
        counter = 0
      }

      on eMsg (m : Msg) {
        send this, eMsg, (round = m.round + 1, phase = Phase.Commit)
      }
    }
  }

  paxiom round_distinct : ∀ (a b : Round), a = b ∨ a ≠ b
  pinstance roundOrd : TotalOrder Round
  pinstance natOrd   : TotalOrder Nat        -- works on built-in types too

  invariant trivial : True

end SelfContained

#gen_module SelfContained
#pwf        SelfContained
