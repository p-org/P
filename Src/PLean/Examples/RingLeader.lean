/-
PLean port of [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p).

The two relational fragments are bundled into typeclasses and
instantiated with `pinstance` over `MachineRef`:
- `LeOrder` — total order `le` + 4 axioms.
- `RingTopology` — cyclic betweenness `btw`, successor `right`,
  10 joint axioms.

Each `pinstance` synthesises an axiomatic witness, an anonymous
`instance` for typeclass resolution, and one top-level axiom per
Prop-typed class field. The obligation generator injects every
pmodule axiom (hand-written `paxiom` or `pinstance`-synthesised) into
every VC's local context, so `loom_smt [*]` sees them.

`x is Won` is a state check (distinct from the event/machine-kind
`is`); ported as `stateOf x s = Won_st`.

Closure rate: **14 / 14** — 12 by SMT, 2 by `@[pverifyProof]`. The
two manual proofs are the inductive steps through `goto Won`:
- `Safety` needs `SelfPendingMax` applied to the in-flight
  `eNominate` to instantiate "this node is the global max"; with that
  hint, antisymmetry collapses any two `Won` nodes.
- `lemmas` needs the cyclic-betweenness axioms (`btw_1`..`btw_4`) to
  preserve `NoBypass` when the handler forwards to the ring
  successor: a `btw_3` totality case-split contradicts the bypass
  hypothesis via `btw`-asymmetry; the self-forwarding branch is
  vacuous via `right_neq_self`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

class LeOrder (t : Type) where
  le          : t → t → Bool
  le_refl     : ∀ x : t, le x x = true
  le_symm     : ∀ x y : t, le x y = true ∨ le y x = true
  le_trans    : ∀ x y z : t, le x y = true → le y z = true → le x z = true
  le_antisymm : ∀ x y : t, le x y = true → le y x = true → x = y

/-- Ring topology bundle: `btw` (ternary cyclic betweenness), `right`
(clockwise successor), and the 8 joint axioms relating them.

`btw_1`..`btw_4` are the betweenness axioms (transitivity, asymmetry,
totality, rotation). `right_neq_self` / `btw_right` / `btw_Aux1` /
`right_btw` mix both names, so they share the class. -/
class RingTopology (t : Type) where
  btw            : t → t → t → Bool
  right          : t → t
  btw_1          : ∀ w x y z : t, btw w x y = true → btw w y z = true → btw w x z = true
  btw_2          : ∀ x y z : t, btw x y z = true → btw x z y = false
  btw_3          : ∀ x y z : t, btw x y z = true ∨ btw x z y = true ∨ x = y ∨ x = z ∨ y = z
  btw_4          : ∀ x y z : t, btw x y z = true → btw y z x = true
  right_neq_self : ∀ x : t, x ≠ right x
  btw_right      : ∀ x y : t, x ≠ y → y ≠ right x → btw x (right x) y = true
  btw_Aux1       : ∀ x y : t, btw x y (right x) = false
  right_btw      : ∀ x n m : t, m = right n → (btw n m x = true ∨ x = m ∨ x = n)

pmodule RingLeader

  system s

  type tNominate = (voteFor : PLean.MachineRef)

  event eNominate : tNominate

  -- Total order on machine ids (the running-max comparison).
  pinstance order : LeOrder PLean.MachineRef

  -- Ring topology: cyclic betweenness + clockwise successor.
  pinstance ring : RingTopology PLean.MachineRef

  machine Server {
    start state Proposing {
      entry {
        send (RingTopology.right this.ref), eNominate, (voteFor = this.ref)
      }

      on eNominate (n : tNominate) {
        if n.voteFor = this.ref then do
          goto Won
        else if LeOrder.le this.ref n.voteFor then do
          send (RingTopology.right this.ref), eNominate, (voteFor = n.voteFor)
        else do
          send (RingTopology.right this.ref), eNominate, (voteFor = this.ref)
      }
    }

    state Won {
      ignore eNominate
    }
  }

  -- voteFor is the running max.
  Lemma lemmas {
    invariant LeaderMax :
      ∀ x y : MachineRef, stateOf x s = Server.Won_st → LeOrder.le y x = true

    invariant Aux :
      ∀ x y : MachineRef, LeOrder.le x y = true → LeOrder.le y x = true → x = y

    invariant NoBypass :
      ∀ (n m : MachineRef) (e : Sig.Label) (p : tNominate),
        inflight e s → (e targets m) → e.action = .event (E.eNominate p) →
        RingTopology.btw p.voteFor n m = true → LeOrder.le n p.voteFor = true

    invariant SelfPendingMax :
      ∀ (n m : MachineRef) (e : Sig.Label) (p : tNominate),
        inflight e s → (e targets m) → e.action = .event (E.eNominate p) →
        p.voteFor = m → LeOrder.le n m = true
  }
  Proof {
    prove lemmas ;
  }

  -- Main theorem: at most one server reaches the `Won` state.
  Theorem Safety {
    invariant UniqueLeader :
      ∀ x y : MachineRef,
        stateOf x s = Server.Won_st → stateOf y s = Server.Won_st → x = y
  }
  Proof {
    prove Safety using lemmas ;
    prove default ;
  }

end RingLeader

#gen_module RingLeader
#pwf        RingLeader

/-
Manual proofs for the two `goto Won` inductive-step obligations
`#pverify` leaves open. Registered via `@[pverifyProof]` under the
obligation names `#pverify` prints.
-/
namespace RingLeader
open PLean PartialCorrectness DemonicChoice

-- `Safety` inductive step through `goto Won`. The solver can't
-- synthesise the one fact it needs: `SelfPendingMax` applied to the
-- in-flight `eNominate` says `this` is the global max, so any
-- pre-existing `Won` node equals it. Supply that hint by hand. The
-- forwarding branches don't touch `currentState`, so the `Won` set
-- is unchanged.
set_option maxHeartbeats 2000000 in
@[pverifyProof]
theorem Server.Proposing.eNominate_correct_block1_Safety_using_lemmas
    (this : Server) (param : eNominate_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (Safety s ∧ lemmas s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Server this.ref s ∧
        (s.machines this.ref).currentState = Server.Proposing_st ∧
        lbl.action = .event (E.eNominate param))
      (do PLean.markReceived (P := Sig) lbl; Server.Proposing.eNominate_handler this param)
      (fun _ s => Safety s) := by
  unfold Server.Proposing.eNominate_handler
  unfold Safety lemmas UniqueLeader LeaderMax Aux NoBypass SelfPendingMax
  try unfold PLean.send PLean.goto PLean.raise PLean.markReceived PLean.announce
  pverify_step_wp
  intro s
  intros
  -- Hypothesis order: invariants UniqueLeader, LeaderMax, Aux,
  -- NoBypass, SelfPendingMax; then dispatcher facts (inflight,
  -- target, is_Server, currentState, action). `lbl` is a universal
  -- theorem binder, not an existential — nothing to `obtain`.
  rename_i hUniq hLM hAux hNB hSPM hinf htgt hThisKind hst hact
  refine ⟨?_, ?_⟩
  · -- `goto Won` branch: `this` received its own nomination ⇒ global max.
    intro hg
    have hMax : ∀ n : MachineRef, LeOrder.le n this.ref = true :=
      fun n => hSPM n this.ref lbl param hinf htgt hact hg
    -- lean-auto rejects `(post.machines _).currentState` under `∀`
    -- ("Higher order input?"), so close by hand: case-split on whether
    -- `x` or `y` is `this.ref`, then use `hMax` + `hAux` (antisymmetry)
    -- or `hUniq` (uniqueness of pre-state `Won` nodes).
    simp only [PLean.stateOf, apply_ite (f := PLean.MachineState.currentState)]
    intro x y hx hy
    by_cases hxThis : x = this.ref <;> try pverify_smt
    · by_cases hyThis : y = this.ref <;> try pverify_grind
      · -- x post-Won, y reads pre-state: `hLM y` + `hMax y` + antisymmetry.
        exfalso; apply hyThis
        rw [if_neg hyThis] at hy
        have h_le_y : LeOrder.le this.ref y = true := hLM y this.ref hy
        have hmax_y : LeOrder.le y this.ref = true := hMax y
        symm
        exact hAux this.ref y h_le_y hmax_y
    · by_cases hyThis : y = this.ref
      · -- symmetric to the previous case.
        exfalso; apply hxThis
        rw [if_neg hxThis] at hx
        have h_le_x : LeOrder.le this.ref x = true := hLM x this.ref hx
        have hmax_x : LeOrder.le x this.ref = true := hMax x
        symm
        exact hAux this.ref x h_le_x hmax_x
      · -- both x, y read from pre-state.
        rw [if_neg hxThis] at hx
        rw [if_neg hyThis] at hy
        exact hUniq x y hx hy
  · -- forwarding branches: `currentState` unchanged → `Won` set
    -- unchanged → `hUniq` applies directly to the post-state.
    intro _
    refine ⟨?_, ?_⟩ <;> intro _ <;>
      (intro x y hx hy
       simp only [PLean.stateOf] at hx hy
       exact hUniq x y hx hy)

-- `lemmas` inductive step through `goto Won`. Same higher-order
-- rejection as Safety: case-split on whether `x = this.ref` for the
-- state-dependent clauses (goto-Won branch) and on whether
-- `e` is the freshly-sent label for the routing clauses (forwarding
-- branches).
set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem Server.Proposing.eNominate_correct_block0_lemmas
    (this : Server) (param : eNominate_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (lemmas s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Server this.ref s ∧
        (s.machines this.ref).currentState = Server.Proposing_st ∧
        lbl.action = .event (E.eNominate param))
      (do PLean.markReceived (P := Sig) lbl; Server.Proposing.eNominate_handler this param)
      (fun _ s => lemmas s) := by
  unfold Server.Proposing.eNominate_handler
  unfold lemmas
  unfold LeaderMax Aux NoBypass SelfPendingMax
  try unfold PLean.send PLean.goto PLean.raise PLean.markReceived PLean.announce
  pverify_step_wp
  intro s
  intros
  -- Hypothesis order: invariants LeaderMax, Aux, NoBypass,
  -- SelfPendingMax; then dispatcher facts (inflight, target,
  -- is_Server, currentState, action). `LeOrder.*` / `RingTopology.*`
  -- class fields are referenced by namespaced name below.
  rename_i hLM hAux hNB hSPM hinf htgt hThisKind hst hact
  refine ⟨?_, ?_⟩
  · -- `goto Won`: `this`'s `currentState` flips, nothing sent.
    intro hg
    have hMax : ∀ n : MachineRef, LeOrder.le n this.ref = true :=
      fun n => hSPM n this.ref lbl param hinf htgt hact hg
    simp only [PLean.stateOf, apply_ite (f := PLean.MachineState.currentState)]
    refine ⟨?_, ?_, ?_, ?_⟩
    · -- LeaderMax: new `Won` node is `this` → `hMax`; old → `hLM`.
      intro x y hx
      by_cases hxThis : x = this.ref
      · rw [hxThis]; exact hMax y
      · rw [if_neg hxThis] at hx
        exact hLM x y hx
    · exact hAux  -- Aux is state-independent.
    · -- NoBypass: new label has action `goto _`, not `event _` →
      -- `hacte` (clause asserts `event …`) rules it out.
      intro n m e p hinfe htgte hacte hbtwe
      apply hNB n m e p ?_ htgte hacte hbtwe
      pverify_inflight_by hinfe using h => rw [h] at hacte; simp at hacte
    · -- SelfPendingMax: same argument.
      intro n m e p hinfe htgte hacte hsvf
      apply hSPM n m e p ?_ htgte hacte hsvf
      pverify_inflight_by hinfe using h => rw [h] at hacte; simp at hacte
  · -- forwarding branches: machines unchanged, one new eNominate sent.
    intro hng
    refine ⟨?_, ?_⟩
    · -- forward `param.voteFor` to `right this`.
      intro hgle
      have hguard : LeOrder.le this.ref param.voteFor = true := hgle
      -- `NoBypass` instantiated at the dispatched label `lbl`.
      have hNBlbl : ∀ n : MachineRef,
          RingTopology.btw param.voteFor n this.ref = true → LeOrder.le n param.voteFor = true :=
        fun n hb => hNB n this.ref lbl param hinf htgt hact hb
      -- `NoBypass` for the new vote (`param.voteFor` → `right this`).
      have hNBnew : ∀ n : MachineRef,
          RingTopology.btw param.voteFor n (RingTopology.right this.ref) = true → LeOrder.le n param.voteFor = true := by
        intro n hbH
        rcases RingTopology.btw_3 param.voteFor n this.ref with hA | hB | hC | hD | hE
        · exact hNBlbl n hA
        · exfalso
          have s1 : RingTopology.btw n param.voteFor this.ref = true :=
            RingTopology.btw_4 this.ref n param.voteFor (RingTopology.btw_4 param.voteFor this.ref n hB)
          have s2 : RingTopology.btw n (RingTopology.right this.ref) param.voteFor = true :=
            RingTopology.btw_4 param.voteFor n (RingTopology.right this.ref) hbH
          have hR : RingTopology.btw this.ref (RingTopology.right this.ref) n = true := by
            rcases RingTopology.btw_3 this.ref n (RingTopology.right this.ref) with r1 | r2 | r3 | r4 | r5
            · exact absurd r1 (by simp [RingTopology.btw_Aux1 this.ref n])
            · exact r2
            · exfalso; rw [← r3] at hB
              have hcon := RingTopology.btw_2 param.voteFor this.ref this.ref hB
              rw [hB] at hcon; exact Bool.noConfusion hcon
            · exact absurd r4 (RingTopology.right_neq_self this.ref)
            · exfalso; rw [r5] at hbH
              have hcon := RingTopology.btw_2 param.voteFor (RingTopology.right this.ref) (RingTopology.right this.ref) hbH
              rw [hbH] at hcon; exact Bool.noConfusion hcon
          have s4 : RingTopology.btw n this.ref (RingTopology.right this.ref) = true :=
            RingTopology.btw_4 (RingTopology.right this.ref) n this.ref (RingTopology.btw_4 this.ref (RingTopology.right this.ref) n hR)
          have s5 : RingTopology.btw n this.ref param.voteFor = true :=
            RingTopology.btw_1 n this.ref (RingTopology.right this.ref) param.voteFor s4 s2
          have s6 : RingTopology.btw n this.ref param.voteFor = false :=
            RingTopology.btw_2 n param.voteFor this.ref s1
          rw [s5] at s6; exact Bool.noConfusion s6
        · rw [← hC]; exact LeOrder.le_refl param.voteFor
        · exact absurd hD hng
        · rw [hE]; exact hguard
      -- `SelfPendingMax` for the new vote.
      have hSPMnew : param.voteFor = RingTopology.right this.ref →
          ∀ n : MachineRef, LeOrder.le n (RingTopology.right this.ref) = true := by
        intro heq n
        rcases RingTopology.right_btw n this.ref (RingTopology.right this.ref) rfl with hc | hc1 | hc2
        · have hb2 : RingTopology.btw (RingTopology.right this.ref) n this.ref = true :=
            RingTopology.btw_4 this.ref (RingTopology.right this.ref) n hc
          rw [← heq]; exact hNBlbl n (by rw [heq]; exact hb2)
        · rw [hc1]; exact LeOrder.le_refl (RingTopology.right this.ref)
        · rw [hc2, ← heq]; exact hguard
      refine ⟨?_, ?_, ?_, ?_⟩
      · -- LeaderMax: machines unchanged → `hLM` direct.
        intro x y hx; exact hLM x y hx
      · exact hAux
      · -- NoBypass: case-split on e = new label vs old.
        rintro n m e p hinfe htgte hacte hbtwe
        by_cases hee : e =
            (⟨RingTopology.right this.ref, EventOrGoto.event (E.eNominate param), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hNBnew n hbtwe
        · refine hNB n m e p ?_ htgte hacte hbtwe
          pverify_inflight_by hinfe using h => exact hee h
      · -- SelfPendingMax: same case-split.
        rintro n m e p hinfe htgte hacte hsvf
        by_cases hee : e =
            (⟨RingTopology.right this.ref, EventOrGoto.event (E.eNominate param), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hSPMnew hsvf n
        · refine hSPM n m e p ?_ htgte hacte hsvf
          pverify_inflight_by hinfe using h => exact hee h
    · -- forward `this.ref` to `right this`: new vote is `this`'s own id.
      intro _
      have hNBnew2 : ∀ n : MachineRef,
          RingTopology.btw this.ref n (RingTopology.right this.ref) = true → LeOrder.le n this.ref = true :=
        fun n hb => absurd hb (by simp [RingTopology.btw_Aux1 this.ref n])
      have hSPMnew2 : this.ref = RingTopology.right this.ref →
          ∀ n : MachineRef, LeOrder.le n (RingTopology.right this.ref) = true :=
        fun heq _ => absurd heq (RingTopology.right_neq_self this.ref)
      refine ⟨?_, ?_, ?_, ?_⟩
      · intro x y hx; exact hLM x y hx
      · exact hAux
      · rintro n m e p hinfe htgte hacte hbtwe
        by_cases hee : e =
            (⟨RingTopology.right this.ref, EventOrGoto.event (E.eNominate ⟨this.ref⟩), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hNBnew2 n hbtwe
        · refine hNB n m e p ?_ htgte hacte hbtwe
          pverify_inflight_by hinfe using h => exact hee h
      · rintro n m e p hinfe htgte hacte hsvf
        by_cases hee : e =
            (⟨RingTopology.right this.ref, EventOrGoto.event (E.eNominate ⟨this.ref⟩), s.actionCount⟩ : Sig.Label)
        · subst hee
          injection hacte with hac; injection hac with hac2; subst hac2
          simp only [PLean.Label.targets?] at htgte; subst htgte
          exact hSPMnew2 hsvf n
        · refine hSPM n m e p ?_ htgte hacte hsvf
          pverify_inflight_by hinfe using h => exact hee h

-- `lemmas` preservation by the `Proposing` entry handler. The entry's
-- `send` carries `voteFor = this.ref`, matching the `this.ref → right this`
-- branch of `eNominate_correct_block0_lemmas`. There's no dispatched
-- `lbl` to case-split on (entry has no inflight precondition), so the
-- proof is the second branch of the on-handler stripped of its
-- forwarding-branch wrapper.
set_option maxHeartbeats 2000000 in
@[pverifyProof]
theorem Server.Proposing.entry_correct_block0_lemmas (this : Server) :
    triple (l := PProp Sig)
      (fun s =>
        (lemmas s) ∧
        is_Server this.ref s ∧
        (s.machines this.ref).currentState = Server.Proposing_st)
      (Server.Proposing.entry this)
      (fun _ s => lemmas s) := by
  unfold Server.Proposing.entry
  unfold lemmas
  unfold LeaderMax Aux NoBypass SelfPendingMax
  try unfold PLean.send PLean.goto PLean.raise PLean.markReceived PLean.announce
  pverify_step_wp
  intro s
  intros
  rename_i hLM hAux hNB hSPM _hThisKind _hst
  have hNBnew2 : ∀ n : MachineRef,
      RingTopology.btw this.ref n (RingTopology.right this.ref) = true →
        LeOrder.le n this.ref = true :=
    fun n hb => absurd hb (by simp [RingTopology.btw_Aux1 this.ref n])
  have hSPMnew2 : this.ref = RingTopology.right this.ref →
      ∀ n : MachineRef, LeOrder.le n (RingTopology.right this.ref) = true :=
    fun heq _ => absurd heq (RingTopology.right_neq_self this.ref)
  refine ⟨?_, ?_, ?_, ?_⟩
  · intro x y hx; exact hLM x y hx
  · exact hAux
  · rintro n m e p hinfe htgte hacte hbtwe
    by_cases hee : e =
        (⟨RingTopology.right this.ref, EventOrGoto.event (E.eNominate ⟨this.ref⟩), s.actionCount⟩ : Sig.Label)
    · subst hee
      injection hacte with hac; injection hac with hac2; subst hac2
      simp only [PLean.Label.targets?] at htgte; subst htgte
      exact hNBnew2 n hbtwe
    · refine hNB n m e p ?_ htgte hacte hbtwe
      -- Entry leaves `received` untouched, so `inflight e s'` simplifies
      -- to `(e = newLbl ∨ s.sent e = true) ∧ s.received e = false` —
      -- one less `∧` level than the on-handler form `pverify_inflight_by`
      -- expects. Discharge by hand.
      simp only [PLean.inflight, Bool.or_eq_true, decide_eq_true_eq] at hinfe ⊢
      refine ⟨?_, hinfe.2⟩
      rcases hinfe.1 with hNew | hOld
      · exact absurd hNew hee
      · exact hOld
  · rintro n m e p hinfe htgte hacte hsvf
    by_cases hee : e =
        (⟨RingTopology.right this.ref, EventOrGoto.event (E.eNominate ⟨this.ref⟩), s.actionCount⟩ : Sig.Label)
    · subst hee
      injection hacte with hac; injection hac with hac2; subst hac2
      simp only [PLean.Label.targets?] at htgte; subst htgte
      exact hSPMnew2 hsvf n
    · refine hSPM n m e p ?_ htgte hacte hsvf
      simp only [PLean.inflight, Bool.or_eq_true, decide_eq_true_eq] at hinfe ⊢
      refine ⟨?_, hinfe.2⟩
      rcases hinfe.1 with hNew | hOld
      · exact absurd hNew hee
      · exact hOld

end RingLeader

-- The `stateOf`-bearing obligations are expensive to reduce in `whnf`,
-- so raise the heartbeat budget.
set_option maxHeartbeats 1000000 in
#pverify RingLeader
