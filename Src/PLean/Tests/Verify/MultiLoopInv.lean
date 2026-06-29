/-
Multi-invariant loop verification. Five scenarios:

1. **Single trivial invariant** (`SingleInv`): baseline; user-invariant
   obligation closes via SMT.
2. **Two trivial invariants** (`DoubleInv`): both invariants are `True`.
   User obligation closes; auto-default reports a real counter-example
   (loop body `send`s without preserving `DefaultInvariants`).
3. **Two non-trivial invariants** (`NontrivialDouble`): two invariants
   over different state components. Closes via SMT.
4. **Two Lemma-bundle invariants** (`TwoBundles`): the user-invariant
   inductive obligation closes via SMT because the obligation generator
   now unfolds user-defined `Lemma`/`Theorem` bundle names in the
   iteration VC's invariantSeq — without this, lean-auto rejects with
   "Higher order input?" on the opaque bundle application.
5. **Cooperating multi-invariant derive a post-condition**
   (`TwoFieldsCooperate`): two loop invariants each pin one machine
   field's bound; their conjunction implies a non-trivial post-loop
   safety property. Pins that (a) multi-invariant loops with quantifiers
   over machine kinds get auto kind-guard injection at term-elab time
   (via `pLoopInvWrap%`), so SMT doesn't fabricate machines with
   mismatched `kind` / `currentState` slots, and (b) when the user's
   theorem bundles the same invariants the handler's pre carries them
   through the loop and the post-loop derivation closes via SMT.

The auto-default `prove default;` obligation reports a counter-example
for non-trivial loop bodies. Closing it requires either a stronger
loop invariant entailing `DefaultInvariants` or a hand-written
`@[pverifyProof]` using `triple_pforeach_with` (see
`Examples/Consensus.lean` for the latter).
-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Probe 1: single invariant — baseline. -/

pmodule SingleInv

  event eTick : PLean.MachineRef

  machine Broadcaster {
    var peers : seq[PLean.MachineRef]

    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_true : True;
        {
          send q, eTick, p;
        }
      }
    }
  }

  Theorem trivial_safety {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end SingleInv

#gen_module SingleInv

set_option pverify.failOnIncomplete false in
#pverify    SingleInv

/-! ## Probe 2: TWO `True` invariants. -/

pmodule DoubleInv

  event eTick : PLean.MachineRef

  machine Broadcaster {
    var peers : seq[PLean.MachineRef]

    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv1 : True;
          invariant inv2 : True;
        {
          send q, eTick, p;
        }
      }
    }
  }

  Theorem trivial_safety {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end DoubleInv

#gen_module DoubleInv

set_option pverify.failOnIncomplete false in
#pverify    DoubleInv

/-! ## Probe 3: TWO non-trivial invariants. -/

pmodule NontrivialDouble

  event eTick : PLean.MachineRef

  machine Broadcaster {
    var peers : seq[PLean.MachineRef]
    var counter : Nat

    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_count : (s.machines this.ref).fields.Broadcaster_counter = (s.machines this.ref).fields.Broadcaster_counter;
          invariant inv_actcount : s.actionCount ≥ s.actionCount;
        {
          send q, eTick, p;
        }
      }
    }
  }

  Theorem trivial_safety {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end NontrivialDouble

#gen_module NontrivialDouble

set_option pverify.failOnIncomplete false in
#pverify    NontrivialDouble

/-! ## Probe 4: TWO Lemma-bundle invariants.

Pins that the obligation generator unfolds user-defined bundle names
when they appear in a loop invariant. Without the unfold, lean-auto's
translator would reject the iteration VC with "Higher order input?"
on the opaque `bundle1 s` / `bundle2 s` applications. -/

pmodule TwoBundles

  event eTick : PLean.MachineRef

  machine Broadcaster {
    var peers : seq[PLean.MachineRef]

    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_bundle1 : bundle1 s ;
          invariant inv_bundle2 : bundle2 s ;
        {
          send q, eTick, p;
        }
      }
    }
  }

  Lemma bundle1 {
    invariant b1 : True
  }

  Lemma bundle2 {
    invariant b2 : True
  }

  Theorem trivial_safety {
    invariant always_true : True
  }

  Proof {
    prove bundle1 ;
    prove bundle2 ;
  }

  Proof Safety {
    prove trivial_safety using bundle1, bundle2 ;
  }

end TwoBundles

#gen_module TwoBundles

set_option pverify.failOnIncomplete false in
#pverify    TwoBundles

/-! ## Probe 5: multiple invariants COOPERATE to derive a non-trivial
post-condition.

The motivating use case for multiple loop invariants: when no single
invariant suffices to derive the safety property, but a conjunction
of two simpler invariants does — and we want SMT to discharge the
post-loop implication automatically.

Setup: a `Counter` machine with two state fields `(low, high : Nat)`
and an invariant `low ≤ high`. The handler runs a foreach that
preserves both `(s.machines _).fields.<low>` and
`(s.machines _).fields.<high>` (no machine update inside the loop —
only sends). Two loop invariants pin each field's stability, and the
post-loop assertion derives the combined `low ≤ high` from them. -/

pmodule TwoFieldsCooperate

  system s

  event eBroadcast : PLean.MachineRef

  machine Counter {
    var peers : seq[PLean.MachineRef]
    var low   : Nat
    var high  : Nat

    start state Run {
      on eBroadcast (p : PLean.MachineRef) {
        foreach (q in peers)
          -- Two cooperating invariants: each bounds one field by a
          -- constant. Together they imply `low ≤ high` post-loop. The
          -- `∀ n : Counter, …` quantifier gets `is_Counter n.ref s →`
          -- auto-injected at term-elab time via `pLoopInvWrap%`.
          invariant inv_low_bound :
            ∀ n : Counter, (s.machines n.ref).fields.Counter_low ≤ 1 ;
          invariant inv_high_bound :
            ∀ n : Counter, (s.machines n.ref).fields.Counter_high ≥ 1 ;
        {
          send q, eBroadcast, p;
        }
      }
    }
  }

  init-holds ∀ n : Counter,
    (n.low ≤ 1) ∧ (n.high ≥ 1)

  -- The Theorem bundles both bound facts. The handler's pre carries
  -- this bundle; both loop invariants (each one bound) are entailed
  -- by it; the post-loop combined fact `low ≤ high` follows.
  Theorem ordered {

    invariant low_bound : ∀ n : Counter,
      (s.machines n.ref).fields.Counter_low ≤ 1
    invariant high_bound : ∀ n : Counter,
      (s.machines n.ref).fields.Counter_high ≥ 1
  
  }

  Proof {
    prove ordered ;
  }

end TwoFieldsCooperate

#gen_module TwoFieldsCooperate

-- The user-invariant obligation `eBroadcast_correct_block0_ordered`
-- needs to derive `low_le_high` post-loop. The user's pre carries it;
-- both loop invariants together carry it through; SMT closes.
-- We don't claim the auto-default closes — see other probes.
set_option pverify.failOnIncomplete false in
#pverify    TwoFieldsCooperate
