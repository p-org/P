/-
Which obligations `#pverify` emits: one per (state, event) handler and
target (handled, `goto`, ignored), one per state entry and target, a
base case per target, error freedom per handler, and a never-receives
obligation per (state, event) that is neither handled, ignored nor
deferred. Deferred events get none.
-/
import PLean

open PLean

pmodule ShapeT
  system s

  event ePing
  event eGo
  event eHold
  event eStray

  machine Node {
    var n : Int

    start state A {
      on ePing {
        n = n + 1
      }
      on eGo goto B
      defer eHold
    }

    state B {
      ignore ePing, eGo
      on eHold {
        n = 0
      }
    }
  }

  init-holds ∀ x : Node, x.n = 0

  Theorem safety {
    invariant nonneg : ∀ x : Node, x.n ≥ 0
    invariant no_stray : ∀ e : eStray, ¬ inflight e s
  }
  Proof {
    prove safety ;
  }
end ShapeT

#gen_module ShapeT

/--
info: #pverify ShapeT: 25 obligations, 25 proved (0 trusting the solver), 0 failed
  ✓ Node.A.ePing_correct_safety
  ✓ Node.A.eGo_correct_safety
  ✓ Node.B.ePing_correct_safety
  ✓ Node.B.eGo_correct_safety
  ✓ Node.B.eHold_correct_safety
  ✓ Node.A.entry_correct_safety
  ✓ Node.B.entry_correct_safety
  ✓ base_safety
  ✓ Node.A.ePing_correct_NoHalt
  ✓ Node.A.eGo_correct_NoHalt
  ✓ Node.B.ePing_correct_NoHalt
  ✓ Node.B.eGo_correct_NoHalt
  ✓ Node.B.eHold_correct_NoHalt
  ✓ Node.A.entry_correct_NoHalt
  ✓ Node.B.entry_correct_NoHalt
  ✓ base_NoHalt
  ✓ Node.A.ePing_safe
  ✓ Node.A.eGo_safe
  ✓ Node.B.ePing_safe
  ✓ Node.B.eGo_safe
  ✓ Node.B.eHold_safe
  ✓ Node.A.entry_safe
  ✓ Node.B.entry_safe
  ✓ Node.A.eStray_never
  ✓ Node.B.eStray_never
-/
#guard_msgs in
set_option pverify.showTimes false in
#pverify ShapeT

/--
info: ShapeT.Node.A.ePing_correct_safety : ∀ (g : ShapeT.GW) (i : Nat) (src dst : MachineRef),
  Framework g →
    g.error = none →
      ShapeT.safety g →
        g.net i = some { src := src, dst := dst, ev := ShapeT.E.ePing } →
          g.log i = some { src := src, dst := dst, ev := ShapeT.E.ePing } →
            i < g.clock →
              (g.machines dst).state = some ShapeT.S.Node_A →
                (g.machines dst).pending = none →
                  (g.machines dst).waiting = none →
                    (g.machines dst).halted = false →
                      RunPost (fun g' ↦ PostInv "nonneg" (ShapeT.nonneg g') ∧ PostInv "no_stray" (ShapeT.no_stray g'))
                        (GWorld.consume g i) dst (g.machines dst) ShapeT.Node.A.ePing_handler
-/
#guard_msgs in
#check @ShapeT.Node.A.ePing_correct_safety

/-- info: 'ShapeT.safety_holds' depends on axioms: [propext, Classical.choice, Quot.sound] -/
#guard_msgs in #print axioms ShapeT.safety_holds
