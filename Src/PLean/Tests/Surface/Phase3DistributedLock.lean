/-
PLean port of [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p).

`Theorem safety` ports all five P-source invariants (including
`not_held_after_release` and `transfer_to_higher`); the P source's
`init-condition` ("exactly one Node holds the lock at startup") is
ported as the `init-holds` clause below. All ten base-case obligations
and both `prove default` inductive obligations discharge via SMT; the
two `prove safety` inductive-step obligations remain disproved — a
genuine inductiveness gap across the `eGrant`/`eAccept` handlers.

Quantifiers over a machine kind (`∀ n : Node, …`) auto-inject
runtime kind guards (`is_Node n.ref s →`) at materialisation, so the
user doesn't have to spell out `Node_allocated n.ref s →` manually
after every `Node`-quantified binder.
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

  -- Ported from the P source's `init-condition`: at startup exactly
  -- one Node holds the lock (with a positive epoch); every other Node
  -- has `held = false` and `epoch = 0`. The `∀ n : Node` / `∃ n : Node`
  -- quantifiers auto-inject `is_Node n.ref s` guards at materialisation
  -- — bare `n : Node` value would otherwise admit refs whose state
  -- slot is unallocated or has a different kind. The `n.held` / `n.epoch`
  -- shorthand desugars to `(s.machines n.ref).fields.Node_held` /
  -- `Node_epoch` via the field-projection sugar.
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

      -- Ported from the P source: when an eAccept is in flight from
      -- node n1, n1 has already released the lock. `e.source` desugars
      -- to `(eAccept_payload_of e).source` via the field-projection sugar.
      invariant not_held_after_release :
        ∀ n1 : Node, ∀ e : eAccept,
          inflight e s →
          e.source = n1.ref →
          n1.held = false

      -- Ported from the P source: an in-flight eAccept always
      -- transfers to a strictly higher epoch.
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

end DistributedLock

#gen_module DistributedLock
#pwf        DistributedLock

namespace DistributedLock
open PartialCorrectness DemonicChoice

/-- Characterisation of the opaque `eAccept_payload_of` extractor on a
concrete label: needed because the extractor is left uninterpreted for
SMT, so the solver can't compute its value on the freshly-sent label. -/
theorem eAccept_payload_of_mk (t : MachineRef) (p : tAccept) (c : Nat) :
    eAccept_payload_of (Label.mk t (.event (E.eAccept p)) c) = p := by
  unfold eAccept_payload_of; rfl

set_option loom.solver "cvc5" in
set_option loom.solver.smt.timeout 30 in
set_option maxHeartbeats 4000000 in
/-- Manual proof of the `eGrant` inductive step. Splitting the post into
its five invariant conjuncts (across the handler's two `if`-branches)
closes nine of ten leaves by SMT directly. The tenth —
`transfer_to_higher` on the then-branch, which sends a fresh `eAccept` —
needs the opaque payload extractor's value on the new label, so it is
discharged by an explicit new-vs-old-label case split. -/
@[pverifyProof]
theorem Node.Act.eGrant_correct_Safety_safety (this : Node) (param : eGrant_payload)
    (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s =>
        (safety s ∧ True) ∧
        inflight lbl s ∧
        lbl.target = this.ref ∧
        is_Node this.ref s ∧
        (s.machines this.ref).currentState = Node.Act_st ∧
        lbl.action = .event (E.eGrant param))
      (do PLean.markReceived (P := Sig) lbl; Node.Act.eGrant_handler this param)
      (fun _ s => safety s ∧ True) := by
  unfold Node.Act.eGrant_handler
  pverify_step_wp
  intro s hpre
  -- `pverify_step_wp` peeled the dispatcher conjuncts into goal
  -- antecedents; `hpre : safety s`. Destructure the bundle (keeping each
  -- invariant folded so `pverify_smt_close`'s own prep unfolds them), then
  -- intro the dispatcher facts.
  -- Unfold the bundle + each invariant in the pre-state so the conjuncts
  -- are first-order (folded `is_Node`/`inflight` are higher-order to
  -- lean-auto); the ∧-structure is preserved so the `obtain` still splits
  -- five ways. `is_eAccept` stays folded — its bridge lemma handles it.
  simp only [safety, unique_holder, no_lock_while_transfer, unique_accept,
    not_held_after_release, transfer_to_higher, is_Node, Node_allocated,
    Node_kind, inflight] at hpre
  obtain ⟨hUH, hNLT, hUA, hNHR, hTH, _⟩ := hpre
  intro hInf hTgt hThisKind hAct
  simp only [is_Node, Node_allocated, Node_kind, inflight] at hThisKind hInf
  refine ⟨?_, ?_⟩
  · -- then-branch: `held && param.epoch > epoch` — release + send eAccept.
    intro hcond
    rw [Bool.and_eq_true, decide_eq_true_eq] at hcond
    obtain ⟨hHeld, hGt⟩ := hcond
    simp only [safety]
    refine ⟨?_, ?_, ?_, ?_, ?_, trivial⟩
    · simp only [unique_holder, is_Node, Node_allocated, Node_kind]; pverify_smt_close
    · simp only [no_lock_while_transfer, is_Node, Node_allocated, Node_kind, inflight]; pverify_smt_close
    · simp only [unique_accept, inflight]; pverify_smt_close
    · simp only [not_held_after_release, is_Node, Node_allocated, Node_kind, inflight]; pverify_smt_close
    · -- transfer_to_higher: new eAccept carries `param.epoch > this.epoch`
      -- (from `hGt`); old labels keep the pre-state bound (`hTH`).
      -- Keep `is_Node` folded so it matches `hTH`'s pre-state kind guard.
      -- The handler updates only `held`/`epoch`, never `kind`/`currentState`,
      -- so `is_Node n1.ref s_post ↔ is_Node n1.ref s` (the `if` on the
      -- machines map preserves both fields).
      simp only [transfer_to_higher, inflight] at ⊢
      intro n1 hn1kind e hisE hsent hsrc
      obtain ⟨hsent1, hrecv⟩ := hsent
      rw [Bool.or_eq_true] at hsent1
      simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
      -- The post-state kind guard `hn1kind` reduces to the pre-state one
      -- (the machine update preserves `kind` and `currentState`). `hTH` is
      -- now in unfolded `kind`-conjunction form, so match that shape.
      have hn1pre : (s.machines n1.ref).kind ≠ 0 ∧
          (s.machines n1.ref).kind = 1 ∧ True := by
        simp only [is_Node, Node_allocated, Node_kind] at hn1kind
        by_cases hn1 : n1.ref = this.ref <;> simp_all
      rcases hsent1 with hNew | hOld
      · rw [decide_eq_true_eq] at hNew
        subst hNew
        rw [eAccept_payload_of_mk] at hsrc ⊢
        simp only at hsrc ⊢
        rw [if_pos hsrc.symm]
        simpa using hGt
      · have hinflE : s.sent e = true ∧ s.received e = false := ⟨hOld, hrecv.2⟩
        have hbound := hTH n1 hn1pre e hisE hinflE hsrc
        by_cases hn1 : n1.ref = this.ref
        · rw [if_pos hn1]; simpa [hn1] using hbound
        · rw [if_neg hn1]; exact hbound
  · -- else-branch: `if`-condition false — state unchanged, invariants hold
    -- verbatim from the pre-state.
    intro hcond
    simp only [safety]
    refine ⟨?_, ?_, ?_, ?_, ?_, trivial⟩ <;>
      (simp only [unique_holder, no_lock_while_transfer, unique_accept,
         not_held_after_release, transfer_to_higher, is_Node, Node_allocated,
         Node_kind, inflight]
       pverify_smt_close)

end DistributedLock

set_option pverify.failOnIncomplete false in
#pverify DistributedLock
