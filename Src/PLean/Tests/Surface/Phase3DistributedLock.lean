/-
PLean Phase-3 — port of Tutorial/Advanced/6_DistributedLock.

Status (2026-06-06): the surface, registry, and obligation-generator
pieces are in place — `pmodule DistributedLock` parses, `#gen_module`
synthesises types/events/machines, `#pwf` reports clean, and the
obligation generator builds the right per-handler `theorem` shapes
from the `Proof Safety` block.

What's *not* yet automated:
- `pverify` doesn't currently close obligations whose handlers read
  or write machine `var`s. The `wpgen` step generates
  `WPGen.default (epoch_get ...)` opaque applications that the tactic
  doesn't know how to step through. PLAN_P3 R15 calls this out;
  resolution requires emitting per-accessor `#derive_lifted_wp`
  declarations alongside `<v>_get` / `<v>_set`, plus `loomSpec` lemmas
  for the `send`/`goto`/`raise`/`announce` primitives. That work is a
  follow-up.

For now the file commented-out `#pverify DistributedLock` line shows
the intended invocation; uncomment when the tactic is ready.

The reference P source is in
`Tutorial/Advanced/6_DistributedLock/PSrc/System.p` — an unmodified
P program that the eventual Phase-3 verification mirrors.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule DistributedLock

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

  Theorem safety {
    invariant unique_holder :
      ∀ s : GlobalState Sig, ∀ n1 n2 : Node,
        Node_allocated n1.ref s → Node_allocated n2.ref s →
        (s.machines n1.ref).fields.Node_held = true →
        (s.machines n2.ref).fields.Node_held = true →
        n1 = n2

    invariant no_lock_while_transfer :
      ∀ s : GlobalState Sig, ∀ n : Node, ∀ e : Sig.Label,
        Node_allocated n.ref s → e is eAccept → inflight e s →
        (s.machines n.ref).fields.Node_held = false

    invariant unique_accept :
      ∀ s : GlobalState Sig, ∀ e1 e2 : Sig.Label,
        e1 is eAccept → e2 is eAccept → inflight e1 s → inflight e2 s → e1 = e2
  }

  Proof Safety {
    prove safety ;
    prove default ;
  }

end DistributedLock

#gen_module DistributedLock
#pwf        DistributedLock

-- Phase-3 status: the obligation generator builds 4 obligations
-- (2 handlers × 2 prove-directives), but `pverify`'s present
-- automation doesn't close them — see file-level comment / R15. The
-- `#pverify` invocation is left commented out so this file builds
-- clean alongside the rest of the test suite. Uncomment once the
-- accessor / primitive `#derive_lifted_wp` chain is in place.
-- #pverify DistributedLock
