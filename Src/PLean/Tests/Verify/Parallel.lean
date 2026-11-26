/-
Parallel obligations: the same module proved one obligation at a time and four
at a time gives the same report (counterexamples included).
-/
import PLean

open PLean

set_option pverify.failOnIncomplete false
set_option pverify.showTimes false

pmodule ParOne
  system s
  event ePing : Int
  machine Node {
    var total : Int
    start state Run {
      on ePing (x : Int) {
        total = total + x
      }
    }
  }
  init-holds ∀ n : Node, n.total = 0
  Theorem safety {
    -- fails: a negative ping
    invariant nonneg : ∀ n : Node, n.total ≥ 0
    invariant stable : ∀ n : Node, n.total = n.total
  }
  Proof {
    prove safety ;
  }
end ParOne

#gen_module ParOne

/--
warning: #pverify ParOne: 8 obligations, 7 proved (0 trusting the solver), 1 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 5
  ✗ [counterexample] Node.Run.ePing_correct_safety
      invariant `nonneg` can fail after Node #1, in state Run, handles ePing (packet #0 from null).
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Node
          #1  Run  total = 1  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  null → Node #1  ePing -2  ◀
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ base_safety [smt 1]
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  ParOne.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.jobs 1 in
#pverify ParOne

pmodule ParFour
  system s
  event ePing : Int
  machine Node {
    var total : Int
    start state Run {
      on ePing (x : Int) {
        total = total + x
      }
    }
  }
  init-holds ∀ n : Node, n.total = 0
  Theorem safety {
    -- fails: a negative ping
    invariant nonneg : ∀ n : Node, n.total ≥ 0
    invariant stable : ∀ n : Node, n.total = n.total
  }
  Proof {
    prove safety ;
  }
end ParFour

#gen_module ParFour

/--
warning: #pverify ParFour: 8 obligations, 7 proved (0 trusting the solver), 1 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 5
  ✗ [counterexample] Node.Run.ePing_correct_safety
      invariant `nonneg` can fail after Node #1, in state Run, handles ePing (packet #0 from null).
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Node
          #1  Run  total = 1  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  null → Node #1  ePing -2  ◀
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ base_safety [smt 1]
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  ParFour.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.jobs 4 in
#pverify ParFour
