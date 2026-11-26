/-
Manual proofs: a `@[pverifyProof]` theorem named like an obligation, with its
statement, replaces the automatic proof (`pverify_auto` runs the solver from
inside one). A manual proof fails when its statement differs (binders
included), when it uses `sorry` or an axiom the module does not declare;
only a theorem can be one; one named like no obligation is reported.
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

-- the solver, from inside a manual proof
@[pverifyProof] theorem Node.Run.ePing_correct_safety : ∀ (g : GW) (i : Nat) (src dst : MachineRef),
    Framework g → g.error = none → safety g →
    g.net i = some { src := src, dst := dst, ev := E.ePing } →
    g.log i = some { src := src, dst := dst, ev := E.ePing } →
    i < g.clock → (g.machines dst).state = some S.Node_Run → (g.machines dst).pending = none →
    (g.machines dst).waiting = none → (g.machines dst).halted = false →
    RunPost (fun g' => PostInv "nonneg" (nonneg g')) (GWorld.consume g i) dst (g.machines dst)
      Node.Run.ePing_handler := by
  pverify_auto

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

-- an unfilled skeleton
#guard_msgs (drop warning) in
@[pverifyProof] theorem Node.Run.entry_correct_safety : ∀ (g : GW) (m : MachineRef) (arg : G),
    Framework g → g.error = none → safety g → (g.machines m).state = some S.Node_Run →
    (g.machines m).pending = some arg → (g.machines m).halted = false →
    RunPost (fun g' => PostInv "small" (small g')) g m { g.machines m with pending := none }
      (Node.Run.entry arg) := by
  sorry

-- an axiom the module does not declare
axiom cheat : False
@[pverifyProof] theorem Node.Run.ePing_correct_safety : ∀ (g : GW) (i : Nat) (src dst : MachineRef),
    Framework g → g.error = none → safety g →
    g.net i = some { src := src, dst := dst, ev := E.ePing } →
    g.log i = some { src := src, dst := dst, ev := E.ePing } →
    i < g.clock → (g.machines dst).state = some S.Node_Run → (g.machines dst).pending = none →
    (g.machines dst).waiting = none → (g.machines dst).halted = false →
    RunPost (fun g' => PostInv "small" (small g')) (GWorld.consume g i) dst (g.machines dst)
      (pure ()) :=
  cheat.elim

-- an implicit binder where the obligation has an explicit one
@[pverifyProof] theorem base_NoHalt {g : GW} : InitConditions g → PostInv "NoHalt" (NoHalt g) :=
  fun h r => (h.2.2.2.2.2.2.2 r).1

-- only a theorem
/--
error: `@[pverifyProof]`: `ManualBad.stray` is not a theorem; a manual proof must be one
-/
#guard_msgs in
@[pverifyProof] axiom stray : True

end ManualBad

/--
warning: #pverify ManualBad: 8 obligations, 4 proved (0 trusting the solver), 4 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 2
  ✗ [error] Node.Run.ePing_correct_safety: `ManualBad.Node.Run.ePing_correct_safety` uses `ManualBad.cheat`, which the module does not declare
  ✗ [error] Node.Run.entry_correct_safety: `ManualBad.Node.Run.entry_correct_safety` uses `sorry`
  ✗ [error] base_safety: the statement of `@[pverifyProof]` theorem `ManualBad.base_safety` is not the obligation's, which is: ∀ (g : GW), InitConditions g → PostInv "small" (small g)
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✗ [error] base_NoHalt: the statement of `@[pverifyProof]` theorem `ManualBad.base_NoHalt` is not the obligation's, which is: ∀ (g : GW), InitConditions g → PostInv "NoHalt" (NoHalt g)
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  assembly error: Application type mismatch: The argument   hI✝ has type   InitConditions g✝ but is expected to have type   True in the application   base_safety g✝ hI✝
  assembly error: Application type mismatch: The argument   g✝ has type   GW of sort `Type` but is expected to have type   InitConditions ?m.95 of sort `Prop` in the application   base_NoHalt g✝
  `@[pverifyProof]` theorem `ManualBad.base_other` is not an obligation
  ManualBad.safe assumes: `sorry`, `ManualBad.cheat`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify ManualBad
