/-
Profiling probe: a tiny pmodule with two trivial SMT obligations,
run with `pverify.profile := true` and `pverify.cache := false`
(so the solver runs on every obligation, no cache hits).

We use a tiny synthetic pmodule rather than importing the real
`DistributedLock` example so re-elaboration is forced and the
instrumented branch of `pverify_smt_close` actually fires.

The profile output is two `logInfo` tables:
- per-obligation top-10 by wall time (cache.pp/hash/fs + smt.prep/auto/solver/assign);
- stage aggregate with % of total.

Read the aggregate row to identify the bottleneck. Headline finding
(2026-06-19): `smt.auto` (lean-auto translation) dominates over
`smt.solver` (cvc5 process), confirming the first cache attempt's
postmortem — the solver is NOT the wall-clock bottleneck.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option pverify.profile true
set_option pverify.cache true
set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 30
set_option maxHeartbeats 4000000

pmodule ProfileProbe

  type tGrant  = (node : PLean.MachineRef, epoch : Int)
  type tAccept = (epoch : Int, source : PLean.MachineRef)

  event eGrant  : tGrant
  event eAccept : tAccept

  machine Node {
    var epoch : Int
    var held  : Bool

    start state Act {
      on eGrant (payload : tGrant) {
        if (held && decide (payload.epoch > epoch)) then do
          held = false
          send payload.node, eAccept, (epoch = payload.epoch, source = this.ref)
      }

      on eAccept (payload : tAccept) {
        if (decide (payload.epoch > epoch)) then do
          held = true
          epoch = payload.epoch
      }
    }
  }

  init-holds (
    ∃ n : Node,
      n.held = true ∧
      n.epoch > 0 ∧
      ∀ n1 : Node,
        n1 ≠ n →
        n1.held = false ∧
        n1.epoch = 0)

  Theorem safety {
    system s {
      invariant unique_holder :
        ∀ n1 n2 : Node,
          n1.held = true →
          n2.held = true →
          n1 = n2

      invariant no_lock_while_transfer :
        ∀ n : Node, ∀ e : eAccept,
          inflight e s →
          n.held = false

      invariant unique_accept :
        ∀ e1 e2 : eAccept,
          inflight e1 s → inflight e2 s → e1 = e2

      invariant not_held_after_release :
        ∀ n1 : Node, ∀ e : eAccept,
          inflight e s →
          e.source = n1.ref →
          n1.held = false

      invariant transfer_to_higher :
        ∀ (n1 : Node) (e : eAccept),
          inflight e s →
          e.source = n1.ref →
          e.epoch > n1.epoch
    }
  }

  Proof Safety {
    prove safety ;
    prove default ;
  }

end ProfileProbe

#gen_module ProfileProbe
#pverify ProfileProbe
