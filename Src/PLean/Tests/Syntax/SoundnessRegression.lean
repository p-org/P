/-
Soundness pins: a false invariant is not proved (its theorem depends on
`sorryAx`), an invariant cannot rebind the world it is checked on, a
`paxiom` cannot depend on the state, and `#pverify` warns when no initial
world exists.
-/
import PLean

open PLean

pmodule FalseInv
  system s
  event ePing
  machine Node {
    var count : Int
    start state Run {
      entry {
        count = count + 1
      }
      ignore ePing
    }
  }
  init-holds ∀ n : Node, n.count = 0
  Theorem stays_zero {
    invariant zero : ∀ n : Node, n.count = 0
  }
  Proof {
    prove stays_zero ;
  }
end FalseInv

#gen_module FalseInv

set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#guard_msgs (drop warning) in
#pverify FalseInv

/--
info: 'FalseInv.stays_zero_holds' depends on axioms: [propext, sorryAx, Classical.choice, Quot.sound]
-/
#guard_msgs in
#print axioms FalseInv.stays_zero_holds

pmodule Shadow
  system s
  event ePing
  machine Node {
    var count : Int
    start state Run {
      ignore ePing
    }
  }
  invariant rebinds : ∀ s : GW, ∀ n : Node, n.count = 0
end Shadow

/--
error: invariant `rebinds` body names the world type. A binder of that type shadows the pmodule's `system` state binder and decouples the invariant from the world it is checked on. Refer to the `system`-declared binder instead.
-/
#guard_msgs in
#gen_module Shadow

/-! A `paxiom` that depends on the state would be assumed of every world. -/

pmodule AxProbe
  system s
  event ePing
  machine Node {
    var cfg : Int
    start state Run {
      ignore ePing
    }
  }
  paxiom config_const : ∀ n : Node, n.cfg = 0
end AxProbe

/--
error: `paxiom config_const` depends on the state (it names `s`, a machine field, or quantifies over a machine kind). An axiom is assumed of every world, and worlds are arbitrary, so it would be false. State an assumption on initial worlds as `init-holds`, and a property of reachable worlds as an invariant to prove; quantify over `PLean.MachineRef` for a fact about every reference.
-/
#guard_msgs in
#gen_module AxProbe

/-! Contradictory `init-holds` make every theorem vacuous; `#pverify` says so. -/

pmodule Vacuous
  system s
  event ePing
  machine Node {
    var x : Int
    start state Run {
      ignore ePing
    }
  }
  init-holds ∃ n : Node, n.x = 0
  init-holds ∀ n : Node, n.x = 1
  Theorem anything {
    invariant seven : ∀ n : Node, n.x = 7
  }
  Proof {
    prove anything ;
  }
end Vacuous

#gen_module Vacuous

/--
error: #pverify Vacuous: 8 obligations, 8 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 6
  ✓ Node.Run.ePing_correct_anything [smt 1]
  ✓ Node.Run.entry_correct_anything [smt 1]
  ✓ base_anything [smt 1]
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  warning: no world satisfies Vacuous.InitConditions (with the axioms), so every theorem holds vacuously
-/
#guard_msgs in
set_option pverify.showTimes false in
#pverify Vacuous
