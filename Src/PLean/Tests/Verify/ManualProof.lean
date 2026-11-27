/-
Manual proofs: a `@[pverifyProof]` theorem named like an obligation, with its
statement, replaces the automatic proof (`pverify_auto` runs the solver from
inside one). A manual proof fails when its statement differs (binders
included), when it uses `sorry` or an axiom the module does not declare,
and when the kernel does not accept it as a proof of the obligation (with
the lemmas it rests on); only a theorem can be one; one named like no
obligation is reported. A manual proof that fails is not used: `M.safe`
takes its obligation as `sorry`.
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
  `@[pverifyProof]` theorem `ManualBad.base_other` is not an obligation
  ManualBad.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify ManualBad

/-! Proofs that never went through the kernel: `#pverify` checks each manual
proof again, against the obligation's statement, with every lemma and
definition it depends on outside Lean's and PLean's libraries. -/

pmodule ManualCheat
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
    -- false: `ePing` takes a count of 5 to 6
    invariant small : ∀ n : Node, n.count ≤ 5
  }
  Proof {
    prove safety ;
  }
end ManualCheat

#gen_module ManualCheat

namespace ManualCheat
open PLean Std.WP Lean Elab Tactic

-- an honest proof, through a lemma of this file
theorem le_five_of_zero (x : Int) (h : x = 0) : x ≤ 5 := by omega

@[pverifyProof] theorem base_safety (g : GW) :
    InitConditions g → PostInv "small" (small g) := by
  intro h
  obtain ⟨-, -, -, -, -, -, -, -, hinit⟩ := h
  exact fun n hn => le_five_of_zero _ (hinit n hn)

-- an ill-typed proof of the false obligation, kept from the kernel
set_option debug.skipKernelTC true in
@[pverifyProof] theorem Node.Run.ePing_correct_safety : ∀ (g : GW) (i : Nat) (src dst : MachineRef),
    Framework g → g.error = none → safety g →
    g.net i = some { src := src, dst := dst, ev := E.ePing } →
    g.log i = some { src := src, dst := dst, ev := E.ePing } →
    i < g.clock → (g.machines dst).state = some S.Node_Run → (g.machines dst).pending = none →
    (g.machines dst).waiting = none → (g.machines dst).halted = false →
    RunPost (fun g' => PostInv "small" (small g')) (GWorld.consume g i) dst (g.machines dst)
      Node.Run.ePing_handler := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)

-- a proof through a lemma kept from the kernel
set_option debug.skipKernelTC true in
theorem oops : False := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)

@[pverifyProof] theorem Node.Run.entry_correct_safety : ∀ (g : GW) (m : MachineRef) (arg : G),
    Framework g → g.error = none → safety g → (g.machines m).state = some S.Node_Run →
    (g.machines m).pending = some arg → (g.machines m).halted = false →
    RunPost (fun g' => PostInv "small" (small g')) g m { g.machines m with pending := none }
      (Node.Run.entry arg) :=
  oops.elim

end ManualCheat

-- it uses Lean's axioms only: only the kernel can tell
/--
info: 'ManualCheat.Node.Run.ePing_correct_safety' depends on axioms: [propext, Classical.choice, Quot.sound]
-/
#guard_msgs in
#print axioms ManualCheat.Node.Run.ePing_correct_safety

/--
warning: #pverify ManualCheat: 8 obligations, 6 proved (0 trusting the solver), 2 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 3, by hand: 1
  ✗ [error] Node.Run.ePing_correct_safety: the kernel does not accept `ManualCheat.Node.Run.ePing_correct_safety` as a proof of the obligation: while replaying declaration 'ManualCheat.Node.Run.ePing_correct_safety': (kernel) declaration type mismatch, 'ManualCheat.Node.Run.ePing_correct_safety' has type   True but it is expected to have typ…
  ✗ [error] Node.Run.entry_correct_safety: the kernel does not accept `ManualCheat.Node.Run.entry_correct_safety` as a proof of the obligation: while replaying declaration 'ManualCheat.oops': (kernel) declaration type mismatch, 'ManualCheat.oops' has type   True but it is expected to have type   False
  ✓ (manual) base_safety
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  ManualCheat.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify ManualCheat

/-- info: 'ManualCheat.safe' depends on axioms: [propext, sorryAx, Classical.choice, Quot.sound] -/
#guard_msgs in
#print axioms ManualCheat.safe
