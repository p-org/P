/-
Soundness pins: a false invariant is not proved (its theorem depends on
`sorryAx`), and an invariant cannot rebind the world it is checked on.
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
