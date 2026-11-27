/-
The kernel check of what `#pverify`'s theorems rest on. A declaration that
skipped the kernel (`debug.skipKernelTC`, a metaprogram) can state anything, so
`#pverify` checks again, through the kernel, every declaration made before it
ran, outside PLean's library and its imports, that its theorems use: manual
proofs and the lemmas they rest on, lemmas the automatic proofs use (a
`@[pverifySimp]` lemma), and lemmas the assembly's tactics use (a `@[simp]`
lemma). An obligation that rests on one the kernel rejects fails, and `M.safe`
takes it as `sorry`, with a warm proof cache too; so does the assembly's step,
when the assembly rests on one. (Code that runs during `#pverify` is trusted;
`leanchecker` covers it.)

Each module is a counter that `ePing` increments. The probes: a copy of `oops`
declared in advance under the name the check would give it; a manual proof
replaced, with a warm cache, by one the kernel rejects; an honest proof through
the recursor of a mutual inductive type (accepted); a forged `@[pverifySimp]`
lemma; a forged `@[simp]` lemma the assembly uses; a forged lemma whose proof
points at a theorem the run declares.
-/
import PLean

open PLean Lean Elab Tactic

/-- Run commands, then drop the declarations they added. -/
elab "#scratch " "{" cs:command* "}" : command => do
  let env ← getEnv
  for c in cs do Command.elabCommand c
  setEnv env

/-! A copy already declared under the check's name for it. -/

pmodule KByp
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
end KByp

#gen_module KByp

namespace KByp
open PLean Std.WP

set_option debug.skipKernelTC true in
theorem oops : False := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)

set_option debug.skipKernelTC true in
theorem _root_._pvcheck.KByp.oops : False := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)

@[pverifyProof] theorem Node.Run.ePing_correct_safety : ∀ (g : GW) (i : Nat) (src dst : MachineRef),
    Framework g → g.error = none → safety g →
    g.net i = some { src := src, dst := dst, ev := E.ePing } →
    g.log i = some { src := src, dst := dst, ev := E.ePing } →
    i < g.clock → (g.machines dst).state = some S.Node_Run → (g.machines dst).pending = none →
    (g.machines dst).waiting = none → (g.machines dst).halted = false →
    RunPost (fun g' => PostInv "small" (small g')) (GWorld.consume g i) dst (g.machines dst)
      Node.Run.ePing_handler :=
  oops.elim

end KByp

/--
warning: #pverify KByp: 8 obligations, 7 proved (0 trusting the solver), 1 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 5
  ✗ [error] Node.Run.ePing_correct_safety: the kernel does not accept `KByp.Node.Run.ePing_correct_safety` as a proof of the obligation: while replaying declaration 'KByp.oops': (kernel) declaration type mismatch, 'KByp.oops' has type   True but it is expected to have type   False
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ base_safety [smt 1]
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  KByp.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify KByp

/--
info: 'KByp.safe' depends on axioms: [propext, sorryAx, Classical.choice, Quot.sound]
-/
#guard_msgs in
#print axioms KByp.safe

/-! A cached assembly, after the manual proof it used is rejected. -/

#eval show IO Unit from do
  try IO.FS.removeFile (PLean.Cache.fileOf `KAsm) catch _ => pure ()

pmodule KAsm
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
end KAsm

#gen_module KAsm

-- run 1: an honest proof, and the cache filled
/--
info: #pverify KAsm: 8 obligations, 8 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 4, from the cache: 1, by hand: 1
  ✓ Node.Run.ePing_correct_safety [smt 1]
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ (manual) base_safety
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [cache 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
-/
#guard_msgs in
#scratch {
  namespace KAsm
  open PLean Std.WP
  @[pverifyProof] theorem base_safety (g : GW) :
      InitConditions g → PostInv "nonneg" (nonneg g) := by
    intro h
    obtain ⟨-, -, -, -, -, -, -, -, hinit⟩ := h
    exact fun n hn => by simp [hinit n hn]
  end KAsm
  set_option pverify.cache true in
  set_option pverify.showTimes false in
  #pverify KAsm
}

-- run 2: a proof that skipped the kernel
namespace KAsm
open PLean Std.WP
set_option debug.skipKernelTC true in
@[pverifyProof] theorem base_safety (g : GW) :
    InitConditions g → PostInv "nonneg" (nonneg g) := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)
end KAsm

/--
warning: #pverify KAsm: 8 obligations, 7 proved (0 trusting the solver), 1 failed
  proved by normalisation or a hypothesis: 0, by crush before SMT: 0, with an SMT solver: 0, from the cache: 7
  ✓ Node.Run.ePing_correct_safety [cached]
  ✓ Node.Run.entry_correct_safety [cached]
  ✗ [error] base_safety: the kernel does not accept `KAsm.base_safety` as a proof of the obligation: while replaying declaration 'KAsm.base_safety': (kernel) declaration type mismatch, 'KAsm.base_safety' has type   ∀ (g : KAsm.GW), True but it is expected to have type   ∀ (g : KAsm.GW), KAsm.InitConditions g → PLean.PostInv…
  ✓ Node.Run.ePing_correct_NoHalt [cached]
  ✓ Node.Run.entry_correct_NoHalt [cached]
  ✓ base_NoHalt [cached]
  ✓ Node.Run.ePing_safe [cached]
  ✓ Node.Run.entry_safe [cached]
  KAsm.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.cache true in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify KAsm

/--
info: 'KAsm.safe' depends on axioms: [propext, sorryAx, Classical.choice, Quot.sound]
-/
#guard_msgs in
#print axioms KAsm.safe

/-! An honest proof through the recursor of a mutual inductive type. -/

pmodule KMut
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
end KMut

#gen_module KMut

namespace KMut
open PLean Std.WP

mutual
inductive A where | a : A | ofB : B → A
inductive B where | b : B | ofA : A → B
end

def isLeaf (x : A) : Bool := match x with | .a => true | .ofB _ => false

theorem leaf : isLeaf A.a = true := rfl

@[pverifyProof] theorem base_safety (g : GW) :
    InitConditions g → PostInv "nonneg" (nonneg g) := by
  intro h
  have _hleaf := leaf
  obtain ⟨-, -, -, -, -, -, -, -, hinit⟩ := h
  exact fun n hn => by simp [hinit n hn]

end KMut

/--
info: #pverify KMut: 8 obligations, 8 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 5, by hand: 1
  ✓ Node.Run.ePing_correct_safety [smt 1]
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ (manual) base_safety
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
-/
#guard_msgs in
set_option pverify.showTimes false in
#pverify KMut

/-! A lemma the automatic proofs use (last: the simp set is global). -/

pmodule KPois
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
    -- false: `ePing` takes a count of 7 to 8
    invariant small : ∀ n : Node, n.count ≤ 7
  }
  Proof {
    prove safety ;
  }
end KPois

#gen_module KPois

namespace KPois

set_option debug.skipKernelTC true in
@[pverifySimp] theorem cheat (x : Int) : (x ≤ 7) = True := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)

end KPois

/--
warning: #pverify KPois: 8 obligations, 5 proved (0 trusting the solver), 3 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 3
  ✗ [error] Node.Run.ePing_correct_safety: `KPois.Node.Run.ePing_correct_safety` rests on `KPois.cheat`, which the kernel does not accept: while replaying declaration 'KPois.cheat': (kernel) declaration type mismatch, 'KPois.cheat' has type   ∀ (x : Int), True but it is expected to have type   ∀ (x : Int), (x ≤ 7) = True
  ✗ [error] Node.Run.entry_correct_safety: `KPois.Node.Run.entry_correct_safety` rests on `KPois.cheat`, which the kernel does not accept: while replaying declaration 'KPois.cheat': (kernel) declaration type mismatch, 'KPois.cheat' has type   ∀ (x : Int), True but it is expected to have type   ∀ (x : Int), (x ≤ 7) = True
  ✗ [error] base_safety: `KPois.base_safety` rests on `KPois.cheat`, which the kernel does not accept: while replaying declaration 'KPois.cheat': (kernel) declaration type mismatch, 'KPois.cheat' has type   ∀ (x : Int), True but it is expected to have type   ∀ (x : Int), (x ≤ 7) = True
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  KPois.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify KPois

/--
info: 'KPois.safe' depends on axioms: [propext, sorryAx, Classical.choice, Quot.sound]
-/
#guard_msgs in
#print axioms KPois.safe

/-! A lemma the assembly uses (the default simp set of its tactics). -/

pmodule KSimp
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
end KSimp

#gen_module KSimp

namespace KSimp

set_option debug.skipKernelTC true in
@[simp↓] theorem no_resume (k : W) (e : E) : prog.resume k e = none := by
  run_tac do (← getMainGoal).assign (mkConst ``True.intro)

end KSimp

/--
warning: #pverify KSimp: 8 obligations, 8 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 6
  ✓ Node.Run.ePing_correct_safety [smt 1]
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ base_safety [smt 1]
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  assembly error: the assembly rests on `KSimp.no_resume`, which the kernel does not accept: while replaying declaration 'KSimp.no_resume': (kernel) declaration type mismatch, 'KSimp.no_resume' has type   ∀ (k : KSimp.W) (e : KSimp.E), True but it is expected to have type   ∀ (k : KSimp.W) (e : KSimp.E), KSimp.prog.r…
  KSimp.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify KSimp

/--
info: 'KSimp.safe' depends on axioms: [propext, sorryAx, Quot.sound]
-/
#guard_msgs in
#print axioms KSimp.safe

/-! A lemma made before the run that uses a theorem the run makes. -/

pmodule KFwd
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
    -- false: `ePing` takes a count of 9 to 10
    invariant small : ∀ n : Node, n.count ≤ 9
  }
  Proof {
    prove safety ;
  }
end KFwd

#gen_module KFwd

namespace KFwd

-- its proof points at `base_safety`, which `#pverify` declares (and proves with it)
set_option debug.skipKernelTC true in
@[pverifySimp] theorem ahead (x : Int) : (x ≤ 9) = True := by
  run_tac do (← getMainGoal).assign (mkConst `KFwd.base_safety)

end KFwd

/--
warning: #pverify KFwd: 8 obligations, 5 proved (0 trusting the solver), 3 failed
  proved by normalisation or a hypothesis: 2, by crush before SMT: 0, with an SMT solver: 3
  ✗ [error] Node.Run.ePing_correct_safety: `KFwd.Node.Run.ePing_correct_safety` rests on `KFwd.ahead`, which the kernel does not accept: `KFwd.ahead` uses `KFwd.base_safety`, which was declared after it
  ✗ [error] Node.Run.entry_correct_safety: `KFwd.Node.Run.entry_correct_safety` rests on `KFwd.ahead`, which the kernel does not accept: `KFwd.ahead` uses `KFwd.base_safety`, which was declared after it
  ✗ [error] base_safety: `KFwd.base_safety` rests on `KFwd.ahead`, which the kernel does not accept: `KFwd.ahead` uses `KFwd.base_safety`, which was declared after it
  ✓ Node.Run.ePing_correct_NoHalt [smt 1]
  ✓ Node.Run.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.ePing_safe [norm]
  ✓ Node.Run.entry_safe [norm]
  KFwd.safe assumes: `sorry`
-/
#guard_msgs in
set_option pverify.failOnIncomplete false in
set_option pverify.showTimes false in
#pverify KFwd

/-- info: 'KFwd.safe' depends on axioms: [propext, sorryAx, Classical.choice, Quot.sound] -/
#guard_msgs in
#print axioms KFwd.safe
