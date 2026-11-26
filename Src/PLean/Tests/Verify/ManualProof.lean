/-
Manual proofs: a `@[pverifyProof]` theorem named like an obligation, with its
statement, replaces the automatic proof; one whose statement differs is a
failure, and one named like no obligation is reported.
-/
import PLean

open PLean

pmodule ManualT
  system s
  event ePing
  machine Node {
    var count : Int
    start state Run {
      on ePing {
        count = count + 1
      }
    }
  }
  init-holds ∀ n : Node, n.count = 0
  Theorem safety {
    invariant nonneg : ∀ n : Node, n.count ≥ 0
  }
  Proof {
    prove safety ;
  }
end ManualT

#gen_module ManualT

namespace ManualT
open PLean Std.WP

@[pverifyProof] theorem base_safety (g : GW) :
    InitConditions g → PostInv "nonneg" (nonneg g) := by
  intro h
  obtain ⟨-, -, -, -, -, -, -, -, hinit⟩ := h
  exact fun n hn => by simp [hinit n hn]

end ManualT

set_option pverify.showTimes false in
#guard_msgs (drop info) in
#pverify ManualT

/-- info: 'ManualT.safety_holds' depends on axioms: [propext, Classical.choice, Quot.sound] -/
#guard_msgs in
#print axioms ManualT.safety_holds

pmodule ManualBad
  system s
  event ePing
  machine Node {
    var count : Int
    start state Run {
      ignore ePing
    }
  }
  Theorem safety {
    invariant small : ∀ n : Node, n.count ≤ 5
  }
  Proof {
    prove safety ;
  }
end ManualBad

#gen_module ManualBad

namespace ManualBad
open PLean Std.WP

-- not the obligation's statement (the premise is missing)
@[pverifyProof] theorem base_safety (_ : GW) : True → PostInv "small" True := by
  intro _; exact trivial

-- named like no obligation
@[pverifyProof] theorem base_other : True := trivial

end ManualBad

/--
warning: #pverify ManualBad: 8 obligations, 7 proved (0 trusting the solver), 1 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 5
  ✓ Node.Run.ePing_correct_safety [smt 1]
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✗ [error] base_safety: the statement of `@[pverifyProof]` theorem `ManualBad.base_safety` is not the obligation's, which is: ∀ (g : GW), InitConditions g → PostInv "small" (small g)
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  assembly error: Application type mismatch: The argument   hI✝ has type   InitConditions g✝ but is expected to have type   True in the application   base_safety g✝ hI✝
  `@[pverifyProof]` theorem `ManualBad.base_other` is not an obligation
  ManualBad.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify ManualBad
