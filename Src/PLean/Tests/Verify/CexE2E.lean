/-
Counterexamples: each failing obligation reports which invariant fails after
which handler, and a smallest pre-state the solver finds (machines by kind,
packets in flight).
-/
import PLean

open PLean

pmodule CexT

  system s

  type tPing = (n : Int)

  event ePing : tPing
  event ePong

  machine Pinger {
    var server : PLean.MachineRef
    var sent   : Int

    start state Start {
      entry {
        send server, ePing, (n = 1)
        sent = sent + 1
      }
      ignore ePong
    }
  }

  machine Ponger {
    var count : Int

    start state Serve {
      on ePing (p : tPing) {
        assert p.n > 0
        count = count + p.n
      }
    }
  }

  init-holds ∀ q : Pinger, q.sent = 0
  init-holds ∀ p : Ponger, p.count = 0

  Lemma counts {
    -- fails after a Ponger handles a second `ePing`
    invariant count_small : ∀ p : Ponger, p.count ≤ 1
    -- fails initially
    invariant count_pos : ∀ p : Ponger, p.count > 0
  }

  Theorem pinger {
    -- fails after a Pinger's entry
    invariant sent_zero : ∀ q : Pinger, q.sent = 0
  }

  Proof {
    prove counts ;
    prove pinger ;
  }

end CexT

#gen_module CexT

set_option pverify.failOnIncomplete false
set_option pverify.showTimes false

/--
warning: #pverify CexT: 21 obligations, 14 proved (0 trusting the solver), 7 failed
  proved by normalisation or a hypothesis: 3, by crush before SMT: 0, with an SMT solver: 11
  ✓ Pinger.Start.ePong_correct_counts [smt 2]
  ✗ [counterexample] Ponger.Serve.ePing_correct_counts
      invariant `count_small` can fail after Ponger #1, in state Serve, handles ePing (packet #0 from null).
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Ponger
          #1  Serve  count = 1  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  null → Ponger #1  ePing {n := 2}  ◀
  ✓ Pinger.Start.entry_correct_counts [smt 2]
  ✓ Ponger.Serve.entry_correct_counts [smt 2]
  ✗ [counterexample] base_counts
      invariant `count_pos` can fail in an initial world.
      Initial world:
      Machines:
        Ponger
          #1  Serve  count = 0  [entry pending]
        (references from #2 on are unallocated)
      In flight:
        (none)
  ✓ Pinger.Start.ePong_correct_pinger [smt 1]
  ✓ Ponger.Serve.ePing_correct_pinger [smt 1]
  ✗ [counterexample] Pinger.Start.entry_correct_pinger
      invariant `sent_zero` can fail after Pinger #1 runs the entry of state Start.
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Pinger
          #1  Start  server = 2, sent = 0  [entry pending]  ◀
        (references from #2 on are unallocated)
      In flight:
        (none)
  ✓ Ponger.Serve.entry_correct_pinger [smt 1]
  ✓ base_pinger [hyp 1]
  ✓ Pinger.Start.ePong_correct_NoHalt [smt 1]
  ✓ Ponger.Serve.ePing_correct_NoHalt [smt 1]
  ✓ Pinger.Start.entry_correct_NoHalt [smt 1]
  ✓ Ponger.Serve.entry_correct_NoHalt [smt 1]
  ✓ base_NoHalt [smt 1]
  ✓ Pinger.Start.ePong_safe [norm]
  ✗ [counterexample] Ponger.Serve.ePing_safe
      Ponger #1, in state Serve, can fail handling ePing (packet #0 from #2 (unallocated)) (an `assert`, a send to `null`, a map or sequence access, or a `choose` fails).
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Ponger
          #1  Serve  count = 1  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  #2 (unallocated) → Ponger #1  ePing {n := -2}  ◀
  ✗ [counterexample] Pinger.Start.entry_safe
      Pinger #1 can fail running the entry of state Start (an `assert`, a send to `null`, a map or sequence access, or a `choose` fails).
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Pinger
          #1  Start  server = 0, sent = 0  [entry pending]  ◀
        (references from #2 on are unallocated)
      In flight:
        (none)
  ✓ Ponger.Serve.entry_safe [norm]
  ✗ [counterexample] Pinger.Start.ePing_never
      ePing (packet #0 from null) can reach Pinger #1 in state Start, which neither handles, defers nor ignores it.
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Pinger
          #1  Start  server = 0, sent = 0  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  null → Pinger #1  ePing {n := 1}  ◀
  ✗ [counterexample] Ponger.Serve.ePong_never
      ePong (packet #0 from #2 (unallocated)) can reach Ponger #1 in state Serve, which neither handles, defers nor ignores it.
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Ponger
          #1  Serve  count = 1  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  #2 (unallocated) → Ponger #1  ePong  ◀
  CexT.safe assumes: `sorry`
-/
#guard_msgs in
#pverify CexT

/-! A set-valued field: the counterexample shows its members. -/

pmodule CexSetT

  system s

  type tJoin = (who : PLean.MachineRef)

  event eJoin : tJoin

  machine Hub {
    var members : set[PLean.MachineRef]

    start state Run {
      on eJoin (j : tJoin) {
        members += (j.who)
      }
    }
  }

  init-holds ∀ h : Hub, ∀ r : PLean.MachineRef, ¬ (r ∈ h.members)

  Theorem small {
    invariant one_member :
      ∀ (h : Hub) (a b : PLean.MachineRef), a ∈ h.members → b ∈ h.members → a = b
  }

  Proof {
    prove small ;
  }

end CexSetT

#gen_module CexSetT

/--
warning: #pverify CexSetT: 1 obligations, 0 proved (0 trusting the solver), 1 failed
  proved by normalisation or a hypothesis: 0, by crush before SMT: 0, with an SMT solver: 0
  ✗ [counterexample] Hub.Run.eJoin_correct_small
      invariant `one_member` can fail after Hub #1, in state Run, handles eJoin (packet #0 from null).
      Pre-state (it satisfies the premises; it need not be reachable):
      Machines:
        Hub
          #1  Run  members = {0}  ◀
        (references from #2 on are unallocated)
      In flight:
        #0  null → Hub #1  eJoin {who := 1}  ◀
-/
#guard_msgs in
set_option pverify.timeout 2 in
set_option pverify.only "eJoin_correct_small" in
#pverify CexSetT
