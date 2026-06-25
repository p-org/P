/-
Pin: `foreach` and `while` parse, lower into a Loom-compatible
`@[loomSpec]` shape, and reach SMT after `pverify_step_wp`'s loop
cleanup simp set runs.

Two probes:

1. **`foreach` over a `seq[T]`** — desugars to `PLean.pforeach xs
   invList (fun x => do body)`. `WPGen.pforeach` (registered
   `@[loomSpec]` in `Semantics/Loop.lean`) matches that shape and
   reduces the iteration's WP to "every `body x` preserves the
   invariant conjunction". Closes the user-invariant inductive
   obligation by SMT.

2. **`while` with explicit invariant** — desugars to `for _ in
   Lean.Loop.mk do invariantGadget … ; onDoneGadget … ;
   decreasingGadget … ; if cond then body else break`. Loom's
   `@[loomSpec] WPGen.forWithInvariantLoop` matches that shape.
   `pverify_step_wp`'s `Pi.inf_apply` / `Pi.top_apply` /
   `inf_Prop_eq` simp set reduces the post-`wpgen`
   `min (fun s => I s) ⊤` shape to a plain `Prop`-level conjunction,
   after which SMT closes the iteration VC.

The `auto-default` obligation (default invariants under the loop)
remains a logical gap for both shapes: the trivial loop invariants
don't preserve `UniqueActions` / `IncreasingCount` etc. across loop
iterations that `send` (foreach case) or are otherwise non-trivial.
Closing that requires either a loop invariant that pins those
default invariants, or a smarter loop-aware `default_inv` tactic —
future work.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Probe 1: `foreach` over a `seq[T]`. -/

pmodule ForeachParse

  event eAck : PLean.MachineRef

  machine Broadcaster {
    var peers : seq[PLean.MachineRef]

    start state Idle {
      on eAck (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant trivial : True;
        {
          send q, eAck, p;
        }
      }
    }
  }

  Theorem trivial_foreach {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_foreach ;
  }

end ForeachParse

#gen_module ForeachParse

-- User-invariant inductive obligation closes via `WPGen.pforeach`
-- (PLean-local `@[loomSpec]`) + SMT. Auto-default obligation reports
-- a real counter-example: the loop body sends, but the trivial
-- invariant doesn't pin `UniqueActions` / `IncreasingCount` /
-- `ReceivedSubsetSent` across iterations. To verify default
-- invariants in a loop-bearing handler, the user must state them
-- explicitly as loop invariants.
set_option pverify.failOnIncomplete false in
#pverify    ForeachParse

/-! ## Probe 2: `while` — verifies via SMT.

`while` uses `Lean.Loop.mk` as the carrier, which Loom's
`@[loomSpec] WPGen.forWithInvariantLoop` matches. The trivial
invariant + trivial body case closes the inductive obligation
end-to-end via SMT after `pverify_step_wp`'s loop-cleanup simp set
runs. -/

pmodule WhileVer

  event eTick : PLean.MachineRef

  machine Counter {
    var n : Nat

    start state Run {
      on eTick (p : PLean.MachineRef) {
        while (decide (n < 10))
          invariant trivial : True;
          done_with True;
        {
          n = n;
        }
      }
    }
  }

  Theorem trivial_while {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_while ;
  }

end WhileVer

#gen_module WhileVer

-- The user-invariant inductive obligation closes by SMT. The
-- auto-default obligation gets a (genuine) counter-example: under a
-- nominal `True`-only loop invariant, the default invariants aren't
-- preserved without a stronger spec linking the loop's post-state to
-- the pre-state. Cleaner closure waits on a loop-aware `default_inv`.
set_option pverify.failOnIncomplete false in
#pverify    WhileVer
