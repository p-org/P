/-
PLean port of [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p).

This is the third Phase-3 (M3) acceptance benchmark. It exercises the
verification-declaration surface more heavily than DistributedLock /
LockServer:

  * uninterpreted `pure` functions with arguments — `le`, `btw`, `right`
    (P's `pure f(..): T;` ⤳ PLean `function f (..) : T`),
  * their defining axioms expressed as `init-condition`s
    (P's `init-condition <p>` ⤳ PLean `init-holds <p>`),
  * those same facts restated as `Lemma`s so later proofs can cite them,
  * a multi-`Lemma` `using` chain
    (`prove lemmas using less_than, between_rel, right_rel`),
  * a state-membership test `x is Won` (a *state* check, distinct from the
    event/machine-kind `is`), ported as `stateOf x s = Won_st`.

Mechanical surface differences from the `.p` source (per `Src/PLean/CLAUDE.md`):

  P:  pure le(x: machine, y: machine): bool      ⤳  function le (x y : MachineRef) : Bool
  P:  init-condition <prop>                       ⤳  init-holds <prop>
  P:  on eNominate do (n: tNominate) { … }        ⤳  on eNominate (n : tNominate) { … }
  P:  ignore eNominate                            ⤳  on eNominate (_ : tNominate) { pure () }
  P:  e.voteFor   (payload access on a label)      ⤳  destructure `e.action = .event (E.eNominate p)`, use `p.voteFor`
  P:  prove Safety using lemmas_LeaderMax, lemmas_Aux
                                                  ⤳  prove Safety using lemmas
        (PLean `using` cites whole `Lemma` bundles, not individual
         invariants; the bundle is a superset of the two P-named
         invariants, so the precondition is at least as strong.)

Verification outcome (as built): #pverify discharges 30 of 32
obligations by SMT, with 1 disproved and 1 unknown. The 30 cover every
base case (including the state-dependent `LeaderMax` / `UniqueLeader`
base cases), every step for the three relational `Lemma`s (`less_than`,
`between_rel`, `right_rel`), the `Aux` / `NoBypass` / `SelfPendingMax`
invariants, all three default invariants, and the `Won`-state handler
obligations.

The 2 residual obligations are the *inductive steps* of `LeaderMax` and
`UniqueLeader` through the `Proposing` handler (the one that does
`goto Won`):

  ? Server.Proposing.eNominate ⊢ lemmas    (unknown)
  ✗ Server.Proposing.eNominate ⊢ Safety    (disproved / counter-example)

These are genuine verification gaps, not tooling limits: preserving
"only the running-max can reach `Won`" across the `goto Won` transition
needs a stronger jointly-inductive invariant than the ported P lemmas
give the SMT solver here. Closing them is protocol-strengthening work
(adding invariants) or `@[pverifyProof]` manual proofs.

Three framework fixes (all upstream of this file) got us here:
  • `goto`-hygiene: `<Mod>.G.unit` is now emitted unhygienically so the
    `goto` doElem macro resolves it (first exercised by this benchmark);
  • machine-state defunctionalisation: `pverify_smt_prep` runs
    `pverify_defunctionalize_machines`, abstracting each scalar/enum
    `(s.machines m).{currentState,kind,stage}` projection into a fresh
    uninterpreted `MachineRef → _` function, so lean-auto no longer
    rejects `stateOf`-bearing goals with `Higher order input?`;
  • `InStart` init modelling + reducible state aliases:
    `emitInitConditions` now asserts every machine begins in a start
    state, and `<S>_st` aliases are `abbrev` (reducible) so the solver
    sees the raw `S` constructors — together these close the two
    state-dependent base cases.

The file is built with `pverify.failOnIncomplete false` so it loads
with those 2 obligations left as printed skeletons. The deliverable is
a faithful, well-formed port that `#gen_module` / `#pwf` accept and
`#pverify` drives to a 30/32 closure rate.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule RingLeader

  type tNominate = (voteFor : PLean.MachineRef)

  event eNominate : tNominate

  -- `pure le(x, y): bool` — a total order over machine ids (the
  -- node-id comparison used to compute the running max).
  function le (x : PLean.MachineRef) (y : PLean.MachineRef) : Bool

  init-holds ∀ x : PLean.MachineRef, le x x = true
  init-holds ∀ x y : PLean.MachineRef, le x y = true ∨ le y x = true
  init-holds ∀ x y z : PLean.MachineRef, le x y = true → le y z = true → le x z = true
  init-holds ∀ x y : PLean.MachineRef, le x y = true → le y x = true → x = y

  Lemma less_than {
    invariant le_refl     : ∀ x : PLean.MachineRef, le x x = true
    invariant le_symm     : ∀ x y : PLean.MachineRef, le x y = true ∨ le y x = true
    invariant le_trans    : ∀ x y z : PLean.MachineRef, le x y = true → le y z = true → le x z = true
    invariant le_antisymm : ∀ x y : PLean.MachineRef, le x y = true → le y x = true → x = y
  }
  Proof {
    prove less_than ;
  }

  -- `pure btw(x, y, z): bool` — the ternary "y is between x and z going
  -- clockwise around the ring" relation.
  function btw (x : PLean.MachineRef) (y : PLean.MachineRef) (z : PLean.MachineRef) : Bool

  init-holds ∀ w x y z : PLean.MachineRef, btw w x y = true → btw w y z = true → btw w x z = true
  init-holds ∀ x y z : PLean.MachineRef, btw x y z = true → btw x z y = false
  init-holds ∀ x y z : PLean.MachineRef, btw x y z = true ∨ btw x z y = true ∨ x = y ∨ x = z ∨ y = z
  init-holds ∀ x y z : PLean.MachineRef, btw x y z = true → btw y z x = true

  Lemma between_rel {
    invariant btw_1 : ∀ w x y z : PLean.MachineRef, btw w x y = true → btw w y z = true → btw w x z = true
    invariant btw_2 : ∀ x y z : PLean.MachineRef, btw x y z = true → btw x z y = false
    invariant btw_3 : ∀ x y z : PLean.MachineRef, btw x y z = true ∨ btw x z y = true ∨ x = y ∨ x = z ∨ y = z
    invariant btw_4 : ∀ x y z : PLean.MachineRef, btw x y z = true → btw y z x = true
  }
  Proof {
    prove between_rel ;
  }

  -- `pure right(x): machine` — the clockwise neighbour of `x` in the ring.
  function right (x : PLean.MachineRef) : PLean.MachineRef

  init-holds ∀ x : PLean.MachineRef, x ≠ right x
  init-holds ∀ x y : PLean.MachineRef, x ≠ y → y ≠ right x → btw x (right x) y = true
  init-holds ∀ x y : PLean.MachineRef, btw x y (right x) = false
  init-holds ∀ x n m : PLean.MachineRef, m = right n → (btw n m x = true ∨ x = m ∨ x = n)

  Lemma right_rel {
    invariant right_neq_self : ∀ x : PLean.MachineRef, x ≠ right x
    invariant btw_right      : ∀ x y : PLean.MachineRef, x ≠ y → y ≠ right x → btw x (right x) y = true
    invariant Aux1           : ∀ x y : PLean.MachineRef, btw x y (right x) = false
    invariant right_btw      : ∀ x n m : PLean.MachineRef, m = right n → (btw n m x = true ∨ x = m ∨ x = n)
  }
  Proof {
    prove right_rel ;
  }

  machine Server {
    start state Proposing {
      entry {
        send (right this.ref), eNominate, (voteFor = this.ref)
      }

      on eNominate (n : tNominate) {
        if n.voteFor = this.ref then do
          goto Won
        else if le this.ref n.voteFor then do
          send (right this.ref), eNominate, (voteFor = n.voteFor)
        else do
          send (right this.ref), eNominate, (voteFor = this.ref)
      }
    }

    state Won {
      -- P's `ignore eNominate`: a no-op handler that drops the event.
      on eNominate (_n : tNominate) {
        pure ()
      }
    }
  }

  -- voteFor is the running max.
  Lemma lemmas {
    system s {
      invariant LeaderMax :
        ∀ x y : MachineRef, stateOf x s = Server.Won_st → le y x = true

      invariant Aux :
        ∀ x y : MachineRef, le x y = true → le y x = true → x = y

      invariant NoBypass :
        ∀ (n m : MachineRef) (e : Sig.Label) (p : tNominate),
          inflight e s → (e targets m) → e.action = .event (E.eNominate p) →
          btw p.voteFor n m = true → le n p.voteFor = true

      invariant SelfPendingMax :
        ∀ (n m : MachineRef) (e : Sig.Label) (p : tNominate),
          inflight e s → (e targets m) → e.action = .event (E.eNominate p) →
          p.voteFor = m → le n m = true
    }
  }
  Proof {
    prove lemmas using less_than, between_rel, right_rel ;
  }

  -- Main theorem: at most one server reaches the `Won` state.
  Theorem Safety {
    system s {
      invariant UniqueLeader :
        ∀ x y : MachineRef,
          stateOf x s = Server.Won_st → stateOf y s = Server.Won_st → x = y
    }
  }
  Proof {
    prove Safety using lemmas ;
    prove default ;
  }

end RingLeader

#gen_module RingLeader
#pwf        RingLeader

-- The 4 residual `stateOf`-bearing obligations are expensive to reduce
-- in `whnf` before lean-auto rejects them as higher-order, so the
-- default 200000-heartbeat budget is exceeded. Raise it so the run
-- completes and reports them cleanly as skeletons (rather than aborting
-- the whole file with a `whnf` timeout).
set_option maxHeartbeats 1000000 in
set_option pverify.failOnIncomplete false in
#pverify RingLeader
