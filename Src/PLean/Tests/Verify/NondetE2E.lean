/-
End-to-end: handler locals, `choose`, `if ($)`, and `assert` (an
error-freedom obligation).
-/
import PLean

open PLean

pmodule NondetT

  system s

  type tVal = (v : Int)

  event eVal : tVal

  machine Node {
    var last  : Int
    var count : Int

    start state Run {
      entry {
        var x : Int
        x = 0
        let d ← choose (5 : Int)
        x = x + d
        if ($) then
          x = x + 1
        assert x ≥ 0
        send this.ref, eVal, (v = x)
      }

      on eVal (payload : tVal) {
        last = payload.v
        count = count + 1
      }
    }
  }

  init-holds ∀ n : Node, n.last = 0 ∧ n.count = 0

  Theorem safety {
    invariant vals_bounded : ∀ e : eVal, sent e s → 0 ≤ e.v ∧ e.v ≤ 6
    invariant last_bounded : ∀ n : Node, 0 ≤ n.last ∧ n.last ≤ 6
  }
  Proof {
    prove safety ;
  }

end NondetT

#gen_module NondetT

#pverify NondetT
