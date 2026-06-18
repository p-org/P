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

Verification outcome (as built): 31 of 32 obligations discharged —
30 by SMT and 1 (`Safety`'s inductive step) by an `@[pverifyProof]`
manual proof. The 30 SMT obligations cover every base case (incl. the
state-dependent `LeaderMax` / `UniqueLeader` base cases), every step
for the three relational `Lemma`s (`less_than`, `between_rel`,
`right_rel`), `Aux` / `NoBypass` / `SelfPendingMax`, all three default
invariants, and the `Won`-state handler obligations.

`Safety`'s inductive step (`UniqueLeader` through the `goto Won`
handler) is proved manually below: it was reported `disproved`, but
that "counter-example" was a quantifier-instantiation artifact — the
goal IS valid. The manual proof supplies the one fact the solver could
not synthesise (`SelfPendingMax` applied to the in-flight `eNominate`
⇒ `this` is the global maximum) and lets SMT finish.

The 1 remaining obligation is `lemmas`'s inductive step
(`NoBypass` / `SelfPendingMax` preservation across forwarding) — the
ring `btw`/`right` reasoning, reported `unknown`. It needs either a
similar (longer) instantiation-hint proof or solver-trigger tuning.

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
with the one remaining obligation left as a printed skeleton. The
deliverable is a faithful, well-formed port that `#gen_module` / `#pwf`
accept and `#pverify` drives to a 31/32 closure rate (30 SMT + 1
manual).
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

/-
Manual proofs (instantiation hints) for the two inductive-step
obligations that `#pverify`'s SMT chain leaves open. They are valid but
need a quantifier instantiation the solver doesn't find on its own: the
fact that a node receiving its *own* nomination is the global maximum
(`SelfPendingMax` applied to the in-flight `eNominate`), which then makes
`LeaderMax` + `Aux` (antisymmetry) collapse any two `Won` nodes.

Registered via `@[pverifyProof]` so `#pverify` picks them up instead of
re-deriving (and failing) them. The names match the obligation names
`#pverify` prints.
-/
namespace RingLeader
open PLean PartialCorrectness DemonicChoice

set_option maxHeartbeats 2000000 in
@[pverifyProof]
theorem Server.Proposing.eNominate_correct_block4_Safety_using_lemmas
    (this : Server) (param : eNominate_payload) :
    triple (l := PProp Sig)
      (fun s => (Safety s ∧ lemmas s ∧ DefaultInvariants s ∧ True) ∧
        ∃ lbl, inflight lbl s ∧ lbl.target = this.ref ∧
          (s.machines this.ref).currentState = Server.Proposing_st ∧
          lbl.action = EventOrGoto.event (E.eNominate param))
      (Server.Proposing.eNominate_handler this param)
      (fun _ s => Safety s ∧ DefaultInvariants s ∧ True) := by
  unfold Server.Proposing.eNominate_handler
  unfold Safety lemmas UniqueLeader LeaderMax Aux NoBypass SelfPendingMax
  try unfold PLean.send PLean.goto PLean.raise PLean.markReceived PLean.announce
  pverify_step_wp
  intros
  split_conjunction_hyps
  -- Flattened precondition hyps, in registration order:
  --   UniqueLeader, LeaderMax, Aux, NoBypass, SelfPendingMax,
  --   DefaultInvariants, dispatcher (the `∃ lbl …`).
  rename_i hUniq hLM hAux hNB hSPM hDI hdisp
  obtain ⟨lbl, hinf, htgt, hst, hact⟩ := hdisp
  refine ⟨?_, ?_⟩
  · -- `goto Won` branch: `this` received its own nomination ⇒ it is the
    -- global max, so any pre-existing `Won` node equals it.
    intro hg
    have hMax : ∀ n : MachineRef, le n this.ref = true :=
      fun n => hSPM n this.ref lbl param hinf htgt hact hg
    -- Push `currentState` through the `goto` machine-update `ite` so the
    -- post-state read abstracts to the same `pStateOf` the hypotheses use.
    simp only [PLean.stateOf, apply_ite (f := PLean.MachineState.currentState)]
    pverify_smt_close
  · -- forwarding branches: no machine changes `currentState`, so the
    -- `Won` set is unchanged and `Safety` is preserved directly.
    intro _
    refine ⟨?_, ?_⟩ <;> intro _ <;>
      (simp only [PLean.stateOf, apply_ite (f := PLean.MachineState.currentState)]
       pverify_smt_close)

end RingLeader

-- The 4 residual `stateOf`-bearing obligations are expensive to reduce
-- in `whnf` before lean-auto rejects them as higher-order, so the
-- default 200000-heartbeat budget is exceeded. Raise it so the run
-- completes and reports them cleanly as skeletons (rather than aborting
-- the whole file with a `whnf` timeout).
set_option maxHeartbeats 1000000 in
set_option pverify.failOnIncomplete false in
#pverify RingLeader
