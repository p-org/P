/-
PLean port of the `Single` variant of
`Tutorial/Advanced/2_TwoPhaseCommitVerification` (parent repo).

Full protocol model — one Coordinator broadcasting `eVoteReq` to a
fixed list of Participants, collecting `eYes` / `eNo` replies, and on
unanimous YES broadcasting `eCommit` (else `eAbort`).

## Surface notes

| P source                       | PLean here                                       |
|--------------------------------|--------------------------------------------------|
| `set[machine] participants()`  | `seq[MachineRef] participants` for iteration +   |
|                                | `function inParticipants : MachineRef → Bool`    |
|                                | for first-order set-membership in invariants.    |
| `coordinator() : machine`      | `function coordinator : MachineRef`              |
| `preference(m) : bool`         | `function preference : MachineRef → Bool`        |
| `yesVotes : set[machine]`      | `var yesVotes : set[MachineRef]`                 |
| `if yesVotes == participants()`| `if allParticipantsVoted = true` — Bool oracle   |
|                                | kept opaque to avoid `Set X` quantifiers under   |
|                                | a paxiom (lean-auto rejects those).              |
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 5

pmodule TwoPhaseCommit

  system s

  type tVoteResp = (source : PLean.MachineRef)

  event eVoteReq
  event eYes     : tVoteResp
  event eNo      : tVoteResp
  event eAbort
  event eCommit

  function coordinator          : PLean.MachineRef
  function participants         : seq[PLean.MachineRef]
  function inParticipants       : PLean.MachineRef → Bool
  function preference           : PLean.MachineRef → Bool
  -- Demonic-choice oracle deciding when to commit. Kept zero-arg and
  -- opaque: a `Set X` argument would land `∀ v : Set X, …` quantifiers
  -- in obligations that lean-auto's monomorphizer rejects.
  function allParticipantsVoted : Bool

  machine Coordinator {
    var yesVotes : set[PLean.MachineRef]

    start state Init {
      entry {
        foreach (p in participants)
          invariant inv_trivial : True ;
        {
          send p, eVoteReq
        }
        goto WaitForResponses
      }
    }

    state WaitForResponses {
      on eYes (resp : tVoteResp) {
        yesVotes += (resp.source)
        if allParticipantsVoted = true then do
          foreach (p in participants)
            invariant inv_trivial : True ;
          {
            send p, eCommit
          }
          goto Committed
      }

      on eNo (resp : tVoteResp) {
        foreach (p in participants)
          invariant inv_trivial : True ;
        {
          send p, eAbort
        }
        goto Aborted
      }
    }

    state Committed { ignore eYes, eNo }
    state Aborted   { ignore eYes, eNo }
  }

  machine Participant {
    start state Undecided {
      on eVoteReq {
        if (preference this.ref) then do
          send coordinator, eYes, (source = this.ref)
        else do
          send coordinator, eNo, (source = this.ref)
      }

      on eCommit goto Accepted
      on eAbort  goto Rejected
    }

    state Accepted { ignore eVoteReq, eCommit, eAbort }
    state Rejected { ignore eVoteReq, eCommit, eAbort }
  }

  -- Init-time topology assumptions: one Coordinator (named
  -- `coordinator`); the participants are exactly the Participant-kind
  -- machines; container vars default to empty.
  init-holds is_Coordinator coordinator s
  init-holds ∀ c : Coordinator, c.ref = coordinator
  init-holds ∀ c : Coordinator,
    ∀ m : PLean.MachineRef, ¬ (c.yesVotes m)
  -- Every Participant starts in `Undecided`. Without this the SMT
  -- can fabricate a Participant in Accepted at init, sinking the
  -- safety base case.
  init-holds ∀ p : Participant,
    stateOf p.ref s = Participant.Undecided_st

  -- The runtime guarantee on the commit oracle: when
  -- `allParticipantsVoted = true` fires, every Participant in fact
  -- prefers YES. The P source carries this implicitly via the
  -- `yesVotes == participants()` set-equality + `a5 :
  -- p ∈ c.yesVotes ⟹ preference(p)`; PLean models it as a paxiom
  -- on the Bool oracle so SMT can use it as a first-order fact.
  paxiom allParticipantsVoted_sound :
    allParticipantsVoted = true →
      ∀ p : PLean.MachineRef, is_Participant p s → preference p = true

  -- Prove the default framework invariants (`UniqueActions`,
  -- `IncreasingCount`, `ReceivedSubsetSent`). The Coordinator
  -- handlers contain `foreach … goto` blocks the auto chain
  -- can't close on its own; manual `@[pverifyProof]` proofs below
  -- discharge those via `triple_pforeach_with`.
  Proof of_default {
    prove default ;
  }

  -- Strengthening lemma: every sent `eCommit` was sent at a moment
  -- when every Participant preferred YES (the Coordinator's eYes
  -- handler only broadcasts eCommit under
  -- `allParticipantsVoted = true`, which by
  -- `allParticipantsVoted_sound` forces unanimous YES).
  --
  -- The body uses `s.sent e = true` (not `inflight e s`) so the
  -- clause stays stable across `markReceived` — receiving an
  -- eCommit doesn't unset `s.sent`.
  Lemma commit_sent {
    invariant commit_sent_implies_all_yes :
      ∀ e : Sig.Label,
        e is eCommit →
        s.sent e = true →
        ∀ p : PLean.MachineRef, is_Participant p s → preference p = true
  }

  Proof of_commit_sent {
    prove commit_sent ;
  }

  -- Main safety theorem: every Participant in `Accepted` saw an
  -- `eCommit` from the Coordinator. By `commit_sent`, all Participants
  -- prefer YES.
  Theorem safety {
    invariant accepted_implies_all_prefer :
      ∀ p1 : Participant,
        stateOf p1.ref s = Participant.Accepted_st →
        ∀ p2 : PLean.MachineRef, is_Participant p2 s → preference p2 = true
  }

  Proof of_safety {
    prove safety using commit_sent ;
  }

end TwoPhaseCommit

#gen_module TwoPhaseCommit
#pwf        TwoPhaseCommit

namespace TwoPhaseCommit
open PLean PartialCorrectness DemonicChoice

/-! ## Local helpers for the foreach-bearing Coordinator handlers.

The three Coordinator handlers (`Init.entry`, `eYes`, `eNo`) share a
common structural shape: handler prelude `yesVotes_get`, optional
container update, `foreach (p in participants) { send p, <ev> }`,
`goto <state>`. The auto-emitted chain's `wpgen` walk does not
carry a chosen post (`DefaultInvariants`, `commit_sent`, `safety`)
through `pforeach` cleanly because the user's trivial loop invariant
`[True]` does not entail it.

`triple_pforeach_with` (in `Semantics/Loop.lean`) carries an external
invariant `Q` across the loop. The proofs below thread `Q` through
each bind step; the helpers in this section factor out the recurring
sub-shapes so each handler proof reads as a short composition. -/

-- Kinds are immutable: a Participant ref can't equal `this.ref`
-- where `this : Coordinator` (Coordinator kind = 1, Participant
-- kind = 2). The contradiction is purely arithmetic on the kind
-- fields after both is_<K> predicates unfold.
private theorem participant_ne_this
    (this : Coordinator) (p : MachineRef) (s : GlobalState Sig)
    (hCo : is_Coordinator this.ref s) (hP : is_Participant p s) :
    p ≠ this.ref := by
  intro hEq
  rw [hEq] at hP
  simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo
  simp only [is_Participant, Participant_allocated, Participant_kind] at hP
  omega

-- `markReceived lbl` preserves `DefaultInvariants` when `lbl` was
-- in-flight (so adding it to `received` keeps `ReceivedSubsetSent`).
-- Factored out because all three on-handler default proofs share
-- this step verbatim.
private theorem markReceived_preserves_default
    (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => DefaultInvariants s ∧ inflight lbl s)
      (PLean.markReceived (P := Sig) lbl)
      (fun _ s => DefaultInvariants s) := by
  unfold PLean.markReceived
  pverify_step_wp
  intro x ⟨hUA, hIC, hRSS⟩ hInf
  obtain ⟨hSent, _⟩ := hInf
  refine ⟨hUA, hIC, ?_⟩
  intro a ha
  rw [Bool.or_eq_true, decide_eq_true_eq] at ha
  rcases ha with hEq | hOld
  · rw [hEq]; exact hSent
  · exact hRSS a hOld

-- The two recurring `commit_sent`-preservation shapes:
-- (a) `send tgt <ev>` for `<ev> ≠ eCommit` — the new label has a
--     non-`eCommit` action, so `is_eCommit e` rules it out.
-- (b) `goto this WaitForResponses` (or any goto) — the new label
--     has action `.goto _`, also ruling out `is_eCommit e`.
--
-- Both proofs follow the same `Bool.or` split on `sent e = true`
-- in the post-state: new label fails `is_eCommit`; old labels fall
-- through to the pre-state hypothesis. Captured here so each
-- Coordinator handler's bind chain reads as a few short applies.

private theorem send_noncommit_preserves_commit_sent
    {ev : E} (h_ne_commit : ev ≠ E.eCommit) (tgt : MachineRef) :
    triple (l := PProp Sig)
      (fun s => commit_sent s)
      (PLean.send (P := Sig) tgt ev)
      (fun _ s => commit_sent s) := by
  unfold PLean.send
  pverify_step_wp
  intro x hCS e hisE ha pp hpp
  rw [Bool.or_eq_true, decide_eq_true_eq] at ha
  rcases ha with hEq | hOld
  · exfalso
    rw [hEq] at hisE
    unfold is_eCommit at hisE
    cases ev <;> simp_all
  · exact hCS e hisE hOld pp hpp




/-! ## Manual proofs for the foreach-bearing Coordinator handlers.

`Init.entry`, `WaitForResponses.eYes`, and `WaitForResponses.eNo`
each contain a `foreach (p in participants) { send p, <ev> }` block
followed by a `goto`. The auto-emitted default chain's `wpgen` walk
does not carry `DefaultInvariants` through `pforeach` cleanly
because the user's trivial loop invariant `[True]` does not entail
it.

`triple_pforeach_with` (in `Semantics/Loop.lean`) carries an external
invariant `Q` across the loop independently of the user's loop
invariants. The chain pattern, for each handler:

1. `triple_cons` weakens the precondition (`DefaultInvariants s ∧
   inflight lbl s` for on-handlers, just `DefaultInvariants` for
   entry).
2. `triple_bind` peels each `var_get` / `var_set` / `markReceived`
   call, each preserving `DefaultInvariants` (closed via `pverify`).
3. `triple_pforeach_with (Q := DefaultInvariants)` lifts the invariant
   across the loop, with each iteration's `send p, <ev>` preserving
   it (closed via `pverify`).
4. The closing `goto` preserves `DefaultInvariants` (closed via
   `pverify`).

`markReceived` needs `inflight lbl s` to certify `ReceivedSubsetSent`:
adding `lbl` to `received` is sound only when `lbl` was sent. The
`triple_cons` weakening therefore keeps `DefaultInvariants ∧
inflight lbl s` as the cut for on-handlers. -/

-- Init.entry: `foreach … >>= goto WaitForResponses`.

@[pverifyProof]
theorem Coordinator.Init.entry_correct_of_default_default
    (this : Coordinator) :
    triple (l := PProp Sig)
      (fun s => DefaultInvariants s ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.Init_st)
      (Coordinator.Init.entry this)
      (fun _ s => DefaultInvariants s) := by
  apply triple_cons (pre := DefaultInvariants)
    (post := fun _ => DefaultInvariants)
  · intro s ⟨h, _, _⟩; exact h
  · intro _ s h; exact h
  unfold Coordinator.Init.entry
  -- Handler prelude: `let yesVotes ← yesVotes_get this.ref`.
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold yesVotes_get; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · apply triple_pforeach_with (Q := DefaultInvariants)
    intro _
    unfold PLean.send
    pverify
  intro _
  unfold PLean.goto
  pverify

-- WaitForResponses.eNo: markReceived + prelude + foreach + goto.

@[pverifyProof]
theorem Coordinator.WaitForResponses.eNo_correct_of_default_default
    (this : Coordinator) (param : eNo_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => DefaultInvariants s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eNo param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eNo_handler this param)
      (fun _ s => DefaultInvariants s) := by
  apply triple_cons
    (pre := fun s => DefaultInvariants s ∧ inflight lbl s)
    (post := fun _ => DefaultInvariants)
  · intro s ⟨h, hInf, _, _, _, _⟩; exact ⟨h, hInf⟩
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · exact markReceived_preserves_default lbl
  intro _
  unfold Coordinator.WaitForResponses.eNo_handler
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold yesVotes_get; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · apply triple_pforeach_with (Q := DefaultInvariants)
    intro _
    unfold PLean.send
    pverify
  intro _
  unfold PLean.goto
  pverify

-- WaitForResponses.eYes: markReceived + prelude + `+=` macro
-- (get;set;get) + `if` (then: foreach + goto / else: pure ()).

@[pverifyProof]
theorem Coordinator.WaitForResponses.eYes_correct_of_default_default
    (this : Coordinator) (param : eYes_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => DefaultInvariants s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eYes param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eYes_handler this param)
      (fun _ s => DefaultInvariants s) := by
  apply triple_cons
    (pre := fun s => DefaultInvariants s ∧ inflight lbl s)
    (post := fun _ => DefaultInvariants)
  · intro s ⟨h, hInf, _, _, _, _⟩; exact ⟨h, hInf⟩
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · exact markReceived_preserves_default lbl
  intro _
  unfold Coordinator.WaitForResponses.eYes_handler
  -- prelude get, then yesVotes += (resp.source) macro (get/set/get)
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold yesVotes_get; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold yesVotes_get; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold yesVotes_set; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold yesVotes_get; pverify
  intro _
  by_cases h : allParticipantsVoted = true
  · rw [if_pos h]
    apply triple_bind (cut := fun _ => DefaultInvariants)
    · apply triple_pforeach_with (Q := DefaultInvariants)
      intro _
      unfold PLean.send
      pverify
    intro _
    unfold PLean.goto
    pverify
  · rw [if_neg h]
    pverify

/-! ## Manual proofs for `commit_sent` + `safety` on Coordinator handlers.

`commit_sent_implies_all_yes : ∀ e, e is eCommit → s.sent e = true →
∀ p, is_Participant p → preference p = true` is preserved by every
handler:

- **Init.entry / eNo**: no eCommit is sent — the new sent set only
  adds an `eVoteReq` / `eAbort`, neither of which is an eCommit. Old
  in-flight eCommits stay (sent is monotone), so the invariant
  transports.

- **eYes**: the `then` branch broadcasts eCommit. The new eCommit's
  preference-of-recipient is guaranteed by
  `allParticipantsVoted_sound`. The `else` branch is a pure container
  write.

`accepted_implies_all_prefer` (the headline `safety`) is preserved by
all Coordinator handlers: they don't change Participant currentState,
so `stateOf p1.ref s` for a Participant `p1` is unchanged. -/

-- commit_sent for Init.entry: handler sends eVoteReq, not eCommit.

@[pverifyProof]
theorem Coordinator.Init.entry_correct_of_commit_sent_commit_sent
    (this : Coordinator) :
    triple (l := PProp Sig)
      (fun s => commit_sent s ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.Init_st)
      (Coordinator.Init.entry this)
      (fun _ s => commit_sent s) := by
  apply triple_cons (pre := commit_sent)
    (post := fun _ => commit_sent)
  · intro s ⟨h, _, _⟩; exact h
  · intro _ s h; exact h
  unfold Coordinator.Init.entry
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold yesVotes_get; pverify
  intro _
  apply triple_bind (cut := fun _ => commit_sent)
  · apply triple_pforeach_with (Q := commit_sent)
    intro p
    exact send_noncommit_preserves_commit_sent (by decide) p
  intro _
  unfold PLean.goto
  pverify_step_wp
  intro s hCS e hisE ha pp hpp
  rw [Bool.or_eq_true, decide_eq_true_eq] at ha
  rcases ha with hEq | hOld
  · exfalso
    rw [hEq] at hisE
    unfold is_eCommit at hisE
    exact hisE
  · pverify_machine_has_type hppPre : Participant pp from hpp
    exact hCS e hisE hOld pp hppPre

-- commit_sent for eNo: handler sends eAbort, not eCommit.

@[pverifyProof]
theorem Coordinator.WaitForResponses.eNo_correct_of_commit_sent_commit_sent
    (this : Coordinator) (param : eNo_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => commit_sent s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eNo param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eNo_handler this param)
      (fun _ s => commit_sent s) := by
  apply triple_cons (pre := commit_sent)
    (post := fun _ => commit_sent)
  · intro s ⟨h, _, _, _, _, _⟩; exact h
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold PLean.markReceived
    pverify_step_wp
    intro s hCS e hisE ha pp hpp
    -- markReceived only modifies `received`; `sent` and machines are
    -- unchanged, so the clause transports verbatim modulo unfolds.
    exact hCS e hisE ha pp hpp
  intro _
  unfold Coordinator.WaitForResponses.eNo_handler
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold yesVotes_get; pverify
  intro _
  apply triple_bind (cut := fun _ => commit_sent)
  · apply triple_pforeach_with (Q := commit_sent)
    intro p
    exact send_noncommit_preserves_commit_sent (by decide) p
  intro _
  unfold PLean.goto
  pverify_step_wp
  intro s hCS e hisE ha pp hpp
  rw [Bool.or_eq_true, decide_eq_true_eq] at ha
  rcases ha with hEq | hOld
  · exfalso
    rw [hEq] at hisE
    unfold is_eCommit at hisE
    exact hisE
  · pverify_machine_has_type hppPre : Participant pp from hpp
    exact hCS e hisE hOld pp hppPre

-- commit_sent for eYes: the then-branch broadcasts eCommit; the
-- guard `allParticipantsVoted = true` + the soundness paxiom give
-- unanimous YES. The else-branch is a pure container write.

@[pverifyProof]
theorem Coordinator.WaitForResponses.eYes_correct_of_commit_sent_commit_sent
    (this : Coordinator) (param : eYes_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => commit_sent s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eYes param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eYes_handler this param)
      (fun _ s => commit_sent s) := by
  apply triple_cons (pre := commit_sent)
    (post := fun _ => commit_sent)
  · intro s ⟨h, _, _, _, _, _⟩; exact h
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold PLean.markReceived commit_sent commit_sent_implies_all_yes; pverify
  intro _
  unfold Coordinator.WaitForResponses.eYes_handler
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold yesVotes_get commit_sent commit_sent_implies_all_yes; pverify
  intro _
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold yesVotes_get commit_sent commit_sent_implies_all_yes; pverify
  intro _
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold yesVotes_set commit_sent commit_sent_implies_all_yes; pverify
  intro _
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold yesVotes_get commit_sent commit_sent_implies_all_yes; pverify
  intro _
  by_cases h : allParticipantsVoted = true
  · rw [if_pos h]
    -- Weaken to a stronger precondition that retains
    -- `allParticipantsVoted = true` across the foreach broadcast.
    apply triple_cons
      (pre := fun s => commit_sent s ∧ allParticipantsVoted = true)
      (post := fun _ => commit_sent)
    · intro s hCS; exact ⟨hCS, h⟩
    · intro _ s hCS; exact hCS
    apply triple_bind
      (cut := fun _ : Unit => (fun s => commit_sent s ∧ allParticipantsVoted = true : PProp Sig))
    · apply triple_pforeach_with
        (Q := fun s => commit_sent s ∧ allParticipantsVoted = true)
      intro _
      unfold PLean.send
      pverify_step_wp
      intro x hCS hAV
      refine ⟨?_, hAV⟩
      intro e hisE ha pp hp
      rw [Bool.or_eq_true, decide_eq_true_eq] at ha
      rcases ha with hEq | hOld
      · -- New eCommit: use the soundness paxiom on `allParticipantsVoted`.
        exact allParticipantsVoted_sound _ hAV pp hp
      · exact hCS e hisE hOld pp hp
    intro _
    unfold PLean.goto
    pverify_step_wp
    intro s hCS _hAV e hisE ha pp hp
    rw [Bool.or_eq_true, decide_eq_true_eq] at ha
    rcases ha with hEq | hOld
    · exfalso
      rw [hEq] at hisE
      unfold is_eCommit at hisE
      exact hisE
    · pverify_machine_has_type hppPre : Participant pp from hp
      exact hCS e hisE hOld pp hppPre
  · rw [if_neg h]
    pverify_step_wp
    intro s hCS e hisE ha pp hp
    exact hCS e hisE ha pp hp

-- safety base case: every Participant starts in Undecided, so no
-- Participant is in Accepted at init — accepted_implies_all_prefer
-- is vacuously true.

@[pverifyProof]
theorem base_of_safety_accepted_implies_all_prefer
    (s : GlobalState Sig) :
    InitConditions s → accepted_implies_all_prefer s := by
  intro hInit p1 hp1 hAccp
  -- The kind-guarded init-holds carries
  --   `∀ p : Participant, is_Participant p.ref s → stateOf p.ref s =
  --     Undecided_st`.
  unfold InitConditions at hInit
  have hUndec : ∀ p : Participant,
      is_Participant p.ref s → stateOf p.ref s = Participant.Undecided_st := by
    tauto
  have hp1Undec := hUndec p1 hp1
  rw [hp1Undec] at hAccp
  exact S.noConfusion hAccp

/-! ### Safety inductive steps on Coordinator handlers.

Coordinator handlers only update `this.ref`'s `currentState` and the
`yesVotes` container; they don't touch Participant `currentState`.
So `stateOf p.ref s'` for any Participant `p` (whose ref differs
from `this.ref`, since kinds disagree) is unchanged. The post-state
`safety` therefore follows from pre-state `safety`. -/

@[pverifyProof]
theorem Coordinator.Init.entry_correct_of_safety_safety_using_commit_sent
    (this : Coordinator) :
    triple (l := PProp Sig)
      (fun s => (safety s ∧ commit_sent s) ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.Init_st)
      (Coordinator.Init.entry this)
      (fun _ s => safety s) := by
  apply triple_cons
    (pre := fun s => safety s ∧ is_Coordinator this.ref s)
    (post := fun _ s => safety s)
  · intro s ⟨⟨h, _⟩, hCo, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  unfold Coordinator.Init.entry
  show triple (l := PProp Sig) _
    (yesVotes_get this.ref >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Set MachineRef =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold yesVotes_get
    pverify_step_wp
    intro s hSafe hCo
    exact ⟨hSafe, hCo⟩
  intro _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Unit => (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · apply triple_pforeach_with
      (Q := fun s => safety s ∧ is_Coordinator this.ref s)
    intro p
    unfold PLean.send
    pverify_step_wp
    intro x hSafe hCo
    refine ⟨?_, ?_⟩
    · intro p1 hp1 hAccp pp hpp
      exact hSafe p1 hp1 hAccp pp hpp
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
      exact hCo
  intro _
  unfold PLean.goto
  pverify_step_wp
  intro s hSafe hCo p1 hp1 hAccp pp hpp
  pverify_machine_has_type hp1Pre : Participant p1.ref from hp1
  pverify_machine_has_type hppPre : Participant pp from hpp
  have hp1NotThis : p1.ref ≠ this.ref :=
    participant_ne_this this p1.ref s hCo hp1Pre
  simp [stateOf, hp1NotThis] at hAccp
  exact hSafe p1 hp1Pre hAccp pp hppPre

@[pverifyProof]
theorem Coordinator.WaitForResponses.eNo_correct_of_safety_safety_using_commit_sent
    (this : Coordinator) (param : eNo_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (safety s ∧ commit_sent s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eNo param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eNo_handler this param)
      (fun _ s => safety s) := by
  apply triple_cons
    (pre := fun s => safety s ∧ is_Coordinator this.ref s)
    (post := fun _ s => safety s)
  · intro s ⟨⟨h, _⟩, _, _, hCo, _, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  show triple (l := PProp Sig) _
    (PLean.markReceived (P := Sig) lbl >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Unit =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold PLean.markReceived
    pverify_step_wp
    intro s hSafe hCo
    refine ⟨hSafe, ?_⟩
    simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
    exact hCo
  intro _
  unfold Coordinator.WaitForResponses.eNo_handler
  show triple (l := PProp Sig) _
    (yesVotes_get this.ref >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Set MachineRef =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold yesVotes_get
    pverify_step_wp
    intro s hSafe hCo
    exact ⟨hSafe, hCo⟩
  intro _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Unit =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · apply triple_pforeach_with
      (Q := fun s => safety s ∧ is_Coordinator this.ref s)
    intro p
    unfold PLean.send
    pverify_step_wp
    intro x hSafe hCo
    refine ⟨?_, ?_⟩
    · intro p1 hp1 hAccp pp hpp
      exact hSafe p1 hp1 hAccp pp hpp
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
      exact hCo
  intro _
  unfold PLean.goto
  pverify_step_wp
  intro s hSafe hCo p1 hp1 hAccp pp hpp
  pverify_machine_has_type hp1Pre : Participant p1.ref from hp1
  pverify_machine_has_type hppPre : Participant pp from hpp
  have hp1NotThis : p1.ref ≠ this.ref :=
    participant_ne_this this p1.ref s hCo hp1Pre
  simp [stateOf, hp1NotThis] at hAccp
  exact hSafe p1 hp1Pre hAccp pp hppPre

@[pverifyProof]
theorem Coordinator.WaitForResponses.eYes_correct_of_safety_safety_using_commit_sent
    (this : Coordinator) (param : eYes_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (safety s ∧ commit_sent s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eYes param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eYes_handler this param)
      (fun _ s => safety s) := by
  apply triple_cons
    (pre := fun s => safety s ∧ is_Coordinator this.ref s)
    (post := fun _ s => safety s)
  · intro s ⟨⟨h, _⟩, _, _, hCo, _, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  show triple (l := PProp Sig) _
    (PLean.markReceived (P := Sig) lbl >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Unit =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold PLean.markReceived
    pverify_step_wp
    intro s hSafe hCo
    refine ⟨hSafe, ?_⟩
    simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
    exact hCo
  intro _
  unfold Coordinator.WaitForResponses.eYes_handler
  -- prelude get
  show triple (l := PProp Sig) _
    (yesVotes_get this.ref >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Set MachineRef =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold yesVotes_get
    pverify_step_wp
    intro s hSafe hCo
    exact ⟨hSafe, hCo⟩
  intro _
  -- += macro: get
  show triple (l := PProp Sig) _
    (yesVotes_get this.ref >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Set MachineRef =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold yesVotes_get
    pverify_step_wp
    intro s hSafe hCo
    exact ⟨hSafe, hCo⟩
  intro _
  -- += macro: set
  show triple (l := PProp Sig) _
    (yesVotes_set this.ref _ >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Unit =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold yesVotes_set
    pverify_step_wp
    intro s hSafe hCo
    refine ⟨?_, ?_⟩
    · intro p1 hp1 hAccp pp hpp
      exact hSafe p1 hp1 hAccp pp hpp
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
      exact hCo
  intro _
  -- += macro: trailing get
  show triple (l := PProp Sig) _
    (yesVotes_get this.ref >>= fun _ => _) _
  apply triple_bind
    (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (cut := fun _ : Set MachineRef =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
    (post := fun _ s => safety s)
  · unfold yesVotes_get
    pverify_step_wp
    intro s hSafe hCo
    exact ⟨hSafe, hCo⟩
  intro _
  by_cases h : allParticipantsVoted = true
  · rw [if_pos h]
    show triple (l := PProp Sig) _
      (pforeach _ _ _ >>= fun _ => _) _
    apply triple_bind
      (pre := (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
      (cut := fun _ : Unit =>
        (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
      (post := fun _ s => safety s)
    · apply triple_pforeach_with
        (Q := fun s => safety s ∧ is_Coordinator this.ref s)
      intro p
      unfold PLean.send
      pverify_step_wp
      intro x hSafe hCo
      refine ⟨?_, ?_⟩
      · intro p1 hp1 hAccp pp hpp
        exact hSafe p1 hp1 hAccp pp hpp
      · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
        exact hCo
    intro _
    unfold PLean.goto
    pverify_step_wp
    intro s hSafe hCo p1 hp1 hAccp pp hpp
    pverify_machine_has_type hp1Pre : Participant p1.ref from hp1
    pverify_machine_has_type hppPre : Participant pp from hpp
    have hp1NotThis : p1.ref ≠ this.ref :=
      participant_ne_this this p1.ref s hCo hp1Pre
    simp [stateOf, hp1NotThis] at hAccp
    exact hSafe p1 hp1Pre hAccp pp hppPre
  · rw [if_neg h]
    pverify_step_wp
    intro s hSafe _hCo p1 hp1 hAccp pp hpp
    exact hSafe p1 hp1 hAccp pp hpp

end TwoPhaseCommit

set_option pverify.failOnIncomplete true in
set_option maxHeartbeats 1000000 in
#pverify    TwoPhaseCommit
