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

Verification outcome (as built): 32 of 32 obligations discharged —
30 by SMT and 2 (the `lemmas` and `Safety` inductive steps through the
`goto Won` handler) by `@[pverifyProof]` manual proofs. The 30 SMT
obligations cover every base case (incl. the state-dependent
`LeaderMax` / `UniqueLeader` base cases), every step for the three
relational `Lemma`s (`less_than`, `between_rel`, `right_rel`), `Aux` /
`NoBypass` / `SelfPendingMax`, all three default invariants, and the
`Won`-state handler obligations.

`Safety`'s inductive step (`UniqueLeader` through the `goto Won`
handler) is proved manually below: it was reported `disproved`, but
that "counter-example" was a quantifier-instantiation artifact — the
goal IS valid. The manual proof supplies the one fact the solver could
not synthesise (`SelfPendingMax` applied to the in-flight `eNominate`
⇒ `this` is the global maximum) and lets SMT finish.

`lemmas`'s inductive step (`NoBypass` / `SelfPendingMax` preservation
across the two forwarding branches) is likewise proved manually below.
This is the genuinely hard obligation: preserving `NoBypass` when the
handler forwards `eNominate` to the ring successor requires the cyclic
betweenness axioms (`btw_1`..`btw_4`). The proof splits each forwarded
label into the new in-flight `eNominate` (discharged by the ring
geometry — a `btw_3` totality case-split that contradicts the bypass
hypothesis via `btw`-asymmetry) versus the pre-existing labels
(discharged by the inductive hypothesis), and the self-forwarding
branch is vacuous via `Aux` and `right_neq_self`.

Two framework fixes (all upstream of this file) got us here:
  • `goto`-hygiene: `<Mod>.G.unit` is now emitted unhygienically so the
    `goto` doElem macro resolves it (first exercised by this benchmark);
  • `InStart` init modelling + reducible state aliases:
    `emitInitConditions` now asserts every machine begins in a start
    state, and `<S>_st` aliases are `@[reducible] def` so the solver
    sees the raw `S` constructors — together these close the two
    state-dependent base cases.

The deliverable is a faithful, well-formed port that `#gen_module` /
`#pwf` accept and `#pverify` drives to a full 32/32 closure rate
(30 SMT + 2 manual `@[pverifyProof]`s).
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

-- `Safety` inductive step through the `goto Won` handler. Goes manual
-- because the solver can't synthesise the one fact it needs:
-- `SelfPendingMax` applied to the in-flight `eNominate` ⇒ `this` is the
-- global maximum, so any pre-existing `Won` node equals it. With that
-- hint in the local context plus `pverify_defunctionalize_state`
-- (to flatten the universally-quantified `(post.machines _).currentState`
-- reads lean-auto otherwise rejects), `pverify_smt_close` finishes.
--
-- The forwarding branches don't change any machine's `currentState`,
-- so the `Won` set is unchanged and `Safety` is preserved from the
-- pre-state.
set_option maxHeartbeats 2000000 in
@[pverifyProof]
theorem Server.Proposing.eNominate_correct_block4_Safety_using_lemmas
    (this : Server) (param : eNominate_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (Safety s ∧ lemmas s ∧ True) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Server this.ref s ∧
        (s.machines this.ref).currentState = Server.Proposing_st ∧
        lbl.action = .event (E.eNominate param))
      (do PLean.markReceived (P := Sig) lbl; Server.Proposing.eNominate_handler this param)
      (fun _ s => Safety s ∧ True) := by
  unfold Server.Proposing.eNominate_handler
  unfold Safety lemmas UniqueLeader LeaderMax Aux NoBypass SelfPendingMax
  try unfold PLean.send PLean.goto PLean.raise PLean.markReceived PLean.announce
  pverify_step_wp
  intro s
  intros
  -- Flattened precondition hyps, in registration order:
  --   UniqueLeader, LeaderMax, Aux, NoBypass, SelfPendingMax, then the
  --   dispatcher facts (inflight lbl, target = this.ref, is_Server this.ref,
  --   currentState = Proposing_st, lbl.action = eNominate param).
  -- `lbl` is a universal binder on the theorem, not an existential in
  -- the precondition, so there is no `obtain` to do here.
  rename_i hUniq hLM hAux hNB hSPM hinf htgt hThisKind hst hact
  refine ⟨?_, ?_⟩
  · -- `goto Won` branch: `this` received its own nomination ⇒ it is the
    -- global max, so any pre-existing `Won` node equals it.
    intro hg
    have hMax : ∀ n : MachineRef, le n this.ref = true :=
      fun n => hSPM n this.ref lbl param hinf htgt hact hg
    -- Push `currentState` through the `goto` machine-update `ite` and
    -- defunctionalise the resulting `(s.machines _).currentState` reads
    -- so lean-auto sees first-order content.
    -- Direct manual reasoning (lean-auto rejects `(s.machines _).currentState`
    -- under `∀` quantifiers with "Higher order input?", so SMT can't close
    -- this directly). Case-split on whether x or y is `this.ref`, then use
    -- either `hMax` (giving `le y this.ref`) or `hUniq` (uniqueness of pre-
    -- state `Won` nodes) to discharge.
    simp only [PLean.stateOf, apply_ite (f := PLean.MachineState.currentState)]
    intro x y hx hy
    by_cases hxThis : x = this.ref <;> try pverify_smt_close
    · by_cases hyThis : y = this.ref <;> try pverify_grind
      · -- x = this.ref (post `Won`), y ≠ this.ref so y reads from pre-state.
        -- `hy` then says `stateOf y s = Won_st` in the pre-state, but `hLM`
        -- (LeaderMax) gives `le this.ref y = true`; combined with `hMax y`
        -- (which gives `le y this.ref = true`), `hAux` (antisymmetry) gives
        -- `y = this.ref`, contradicting hyThis.
        exfalso; apply hyThis
        rw [if_neg hyThis] at hy
        have h_le_y : le this.ref y = true := hLM y this.ref hy
        have hmax_y : le y this.ref = true := hMax y
        symm
        exact hAux this.ref y h_le_y hmax_y
    · by_cases hyThis : y = this.ref
      · -- symmetric.
        exfalso; apply hxThis
        rw [if_neg hxThis] at hx
        have h_le_x : le this.ref x = true := hLM x this.ref hx
        have hmax_x : le x this.ref = true := hMax x
        symm
        exact hAux this.ref x h_le_x hmax_x
      · -- Both x ≠ this.ref, y ≠ this.ref: both read from pre-state.
        rw [if_neg hxThis] at hx
        rw [if_neg hyThis] at hy
        exact hUniq x y hx hy
  · -- forwarding branches: no machine changes `currentState`, so the
    -- post-state `Won` set is unchanged and `Safety` is preserved.
    intro _
    refine ⟨?_, ?_⟩ <;> intro _ <;>
      (intro x y hx hy
       -- Forwarding doesn't update machines; `stateOf` reads through the
       -- send'd state map (= pre-state's `machines`), so `hUniq` applies
       -- directly. Unfold `stateOf` so the projection reaches hUniq's shape.
       simp only [PLean.stateOf] at hx hy
       exact hUniq x y hx hy)

-- `lemmas` inductive step through the `goto Won` handler. Like Safety,
-- this can't be closed by SMT directly because lean-auto rejects
-- `(s.machines _).currentState` under `∀` quantifiers. Manual reasoning:
-- goto-Won branch updates only `this`'s state, so case-split on whether
-- `x = this.ref` for state-dependent invariants; the forwarding branches
-- enqueue a new `eNominate` label, so case-split on whether `e = new` for
-- the routing invariants `NoBypass` and `SelfPendingMax`.
set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem Server.Proposing.eNominate_correct_block3_lemmas_using_less_than_between_rel_right_rel
    (this : Server) (param : eNominate_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (lemmas s ∧ less_than s ∧ between_rel s ∧ right_rel s ∧ True) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Server this.ref s ∧
        (s.machines this.ref).currentState = Server.Proposing_st ∧
        lbl.action = .event (E.eNominate param))
      (do PLean.markReceived (P := Sig) lbl; Server.Proposing.eNominate_handler this param)
      (fun _ s => lemmas s ∧ True) := by
  unfold Server.Proposing.eNominate_handler
  unfold lemmas less_than between_rel right_rel
  unfold LeaderMax Aux NoBypass SelfPendingMax
  unfold le_refl le_symm le_trans le_antisymm
  unfold btw_1 btw_2 btw_3 btw_4
  unfold right_neq_self btw_right Aux1 right_btw
  try unfold PLean.send PLean.goto PLean.raise PLean.markReceived PLean.announce
  pverify_step_wp
  intro s
  intros
  -- 20 hyps in registration order: LeaderMax, Aux, NoBypass, SelfPendingMax,
  -- le_refl..le_antisymm, btw_1..btw_4, right_neq_self, btw_right, Aux1,
  -- right_btw, plus 5 dispatcher facts (inflight, target, is_Server,
  -- currentState, action).
  rename_i hLM hAux hNB hSPM hle_refl hle_symm hle_trans hle_antisymm
           hbtw1 hbtw2 hbtw3 hbtw4 hrns hbtwr hAux1 hrbtw
           hinf htgt hThisKind hst hact
  refine ⟨?_, ?_⟩
  · -- `goto Won`: `this`'s currentState flips to Won_st. No new label sent.
    intro hg
    have hMax : ∀ n : MachineRef, le n this.ref = true :=
      fun n => hSPM n this.ref lbl param hinf htgt hact hg
    simp only [PLean.stateOf, apply_ite (f := PLean.MachineState.currentState)]
    refine ⟨?_, ?_, ?_, ?_⟩
    · -- LeaderMax: ∀ x y, stateOf x s_post = Won → le y x.
      intro x y hx
      by_cases hxThis : x = this.ref
      · rw [hxThis]; exact hMax y
      · rw [if_neg hxThis] at hx
        exact hLM x y hx
    · -- Aux: state-independent fact.
      exact hAux
    · -- NoBypass: goto-Won enqueues a goto-label (action = goto G.unit),
      -- which is ruled out by `hacte` (action must be event eNominate).
      intro n m e p hinfe htgte hacte hbtwe
      apply hNB n m e p ?_ htgte hacte hbtwe
      pverify_inflight_by hinfe using h => rw [h] at hacte; simp at hacte
    · -- SelfPendingMax: same — ruled out by `hacte`.
      intro n m e p hinfe htgte hacte hsvf
      apply hSPM n m e p ?_ htgte hacte hsvf
      pverify_inflight_by hinfe using h => rw [h] at hacte; simp at hacte
  · -- forwarding branches: state unchanged but a new eNominate label is sent.
    intro hng
    refine ⟨?_, ?_⟩
    · -- forward `param.voteFor` to `right this`.
      intro hgle
      have hguard : le this.ref param.voteFor = true := hgle
      -- `NoBypass` on the dispatched label `lbl`.
      have hNBlbl : ∀ n : MachineRef,
          btw param.voteFor n this.ref = true → le n param.voteFor = true :=
        fun n hb => hNB n this.ref lbl param hinf htgt hact hb
      -- `NoBypass` for the new vote (`param.voteFor` → `right this`).
      have hNBnew : ∀ n : MachineRef,
          btw param.voteFor n (right this.ref) = true → le n param.voteFor = true := by
        intro n hbH
        rcases hbtw3 param.voteFor n this.ref with hA | hB | hC | hD | hE
        · exact hNBlbl n hA
        · exfalso
          have s1 : btw n param.voteFor this.ref = true :=
            hbtw4 this.ref n param.voteFor (hbtw4 param.voteFor this.ref n hB)
          have s2 : btw n (right this.ref) param.voteFor = true :=
            hbtw4 param.voteFor n (right this.ref) hbH
          have hR : btw this.ref (right this.ref) n = true := by
            rcases hbtw3 this.ref n (right this.ref) with r1 | r2 | r3 | r4 | r5
            · exact absurd r1 (by simp [hAux1 this.ref n])
            · exact r2
            · exfalso; rw [← r3] at hB
              have hcon := hbtw2 param.voteFor this.ref this.ref hB
              rw [hB] at hcon; exact Bool.noConfusion hcon
            · exact absurd r4 (hrns this.ref)
            · exfalso; rw [r5] at hbH
              have hcon := hbtw2 param.voteFor (right this.ref) (right this.ref) hbH
              rw [hbH] at hcon; exact Bool.noConfusion hcon
          have s4 : btw n this.ref (right this.ref) = true :=
            hbtw4 (right this.ref) n this.ref (hbtw4 this.ref (right this.ref) n hR)
          have s5 : btw n this.ref param.voteFor = true :=
            hbtw1 n this.ref (right this.ref) param.voteFor s4 s2
          have s6 : btw n this.ref param.voteFor = false :=
            hbtw2 n param.voteFor this.ref s1
          rw [s5] at s6; exact Bool.noConfusion s6
        · rw [← hC]; exact hle_refl param.voteFor
        · exact absurd hD hng
        · rw [hE]; exact hguard
      -- `SelfPendingMax` for the new vote.
      have hSPMnew : param.voteFor = right this.ref →
          ∀ n : MachineRef, le n (right this.ref) = true := by
        intro heq n
        rcases hrbtw n this.ref (right this.ref) rfl with hc | hc1 | hc2
        · have hb2 : btw (right this.ref) n this.ref = true :=
            hbtw4 this.ref (right this.ref) n hc
          rw [← heq]; exact hNBlbl n (by rw [heq]; exact hb2)
        · rw [hc1]; exact hle_refl (right this.ref)
        · rw [hc2, ← heq]; exact hguard
      refine ⟨?_, ?_, ?_, ?_⟩
      · -- LeaderMax: forwarding doesn't change machines, no Won state changes.
        intro x y hx; exact hLM x y hx
      · exact hAux
      · -- NoBypass split on new vs old label.
        rintro n m e p hinfe htgte hacte hbtwe
        by_cases hee : e =
            (⟨right this.ref, EventOrGoto.event (E.eNominate param), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hNBnew n hbtwe
        · refine hNB n m e p ?_ htgte hacte hbtwe
          pverify_inflight_by hinfe using h => exact hee h
      · -- SelfPendingMax split on new vs old label.
        rintro n m e p hinfe htgte hacte hsvf
        by_cases hee : e =
            (⟨right this.ref, EventOrGoto.event (E.eNominate param), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hSPMnew hsvf n
        · refine hSPM n m e p ?_ htgte hacte hsvf
          pverify_inflight_by hinfe using h => exact hee h
    · -- forward `this.ref` to `right this`: new vote is `this`'s own id.
      intro _
      have hNBnew2 : ∀ n : MachineRef,
          btw this.ref n (right this.ref) = true → le n this.ref = true :=
        fun n hb => absurd hb (by simp [hAux1 this.ref n])
      have hSPMnew2 : this.ref = right this.ref →
          ∀ n : MachineRef, le n (right this.ref) = true :=
        fun heq _ => absurd heq (hrns this.ref)
      refine ⟨?_, ?_, ?_, ?_⟩
      · intro x y hx; exact hLM x y hx
      · exact hAux
      · rintro n m e p hinfe htgte hacte hbtwe
        by_cases hee : e =
            (⟨right this.ref, EventOrGoto.event (E.eNominate ⟨this.ref⟩), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hNBnew2 n hbtwe
        · refine hNB n m e p ?_ htgte hacte hbtwe
          pverify_inflight_by hinfe using h => exact hee h
      · rintro n m e p hinfe htgte hacte hsvf
        by_cases hee : e =
            (⟨right this.ref, EventOrGoto.event (E.eNominate ⟨this.ref⟩), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hSPMnew2 hsvf n
        · refine hSPM n m e p ?_ htgte hacte hsvf
          pverify_inflight_by hinfe using h => exact hee h

end RingLeader

-- The `stateOf`-bearing obligations are expensive to reduce in `whnf`,
-- so raise the heartbeat budget.
set_option maxHeartbeats 1000000 in
#pverify RingLeader
