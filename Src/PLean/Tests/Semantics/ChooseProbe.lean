/-
Probe: confirm `PLean.choose` reaches `wpgen` and `pverify_smt`.

The handler-shaped triple below picks a nondeterministic `n : Int`
unconditionally bumps `actionCount`, and proves a clause that should
hold for every chosen `n`. If `wpgen` doesn't step through
`MonadNonDet.pick`, this stalls.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule ChooseProbe
  event eDone

  machine M {
    var v : Int
    start state S {
      on eDone {
        let n ← PLean.choose 10
        v = v + n + 1
      }
    }
  }

  Theorem trivial {

    invariant t : ∀ m : M, True
  
  }

  Proof Safety { prove trivial ; prove default ; }
end ChooseProbe

#gen_module ChooseProbe
#pwf        ChooseProbe
#pverify    ChooseProbe

-- Smoke check that wpgen reaches choose / pick.
namespace ChooseProbe
open PLean PartialCorrectness DemonicChoice

example : triple (l := PProp Sig)
    (fun _ => True)
    (do let _ ← PLean.choose 10; pure ())
    (fun _ _ => True) := by
  pverify_step_wp

end ChooseProbe
