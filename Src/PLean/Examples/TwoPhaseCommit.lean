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
| `if yesVotes == participants()`| `if ∀ p, inParticipants p → yesVotes p` — the    |
|                                | guard directly states set-coverage, so no oracle |
|                                | or axiom is needed (safety is fully derived).    |
-/
import PLean

open PLean PartialCorrectness DemonicChoice
-- The eYes commit guard is a `∀`-Prop over the vote set (`∀ p,
-- inParticipants p → yesVotes p`), matching P's `yesVotes ==
-- participants()`. `open Classical` supplies the `Decidable` instance the
-- surface `if` needs (an unbounded `∀ : MachineRef` isn't computably
-- decidable — but this model is only ever symbolically evaluated).
open Classical

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
  -- First-order membership oracle for `participants()`. Tied to the
  -- runtime `is_Participant` kind by `system_config` below.
  function inParticipants       : PLean.MachineRef → Bool
  function preference           : PLean.MachineRef → Bool

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
        if (∀ p : PLean.MachineRef, inParticipants p = true → yesVotes p) then do
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

  -- P's `init-condition forall m :: m in participants() == m is
  -- Participant`: the membership oracle names exactly the Participant-kind
  -- machines. Ported as an `init-holds` so the base case discharges.
  init-holds ∀ p : PLean.MachineRef,
    inParticipants p = true ↔ is_Participant p s

  -- P's `system_config.participant_set`: the topology fact above is an
  -- invariant (neither `inParticipants` nor the kind ever changes), so it
  -- holds in every reachable state — no axiom needed.
  Lemma system_config {
    invariant participant_set :
      ∀ p : PLean.MachineRef, inParticipants p = true ↔ is_Participant p s
  }

  Proof of_system_config {
    prove system_config ;
  }

  -- Vote-tracking bundle (P's `kondo` invariants a2a + a5). Carries
  -- `preference` from the eVoteReq guard (only yes-preferrers send eYes)
  -- into the Coordinator's `yesVotes` set, so the eCommit-guard
  -- derivation below reads `preference` off the collected votes.
  Lemma votes {
    -- a2a: an in-flight eYes was sent by a yes-preferring source.
    invariant yes_implies_pref :
      ∀ e : eYes,
        s.sent e = true → preference e.source = true

    -- a5: every collected vote is from a yes-preferring participant.
    invariant votes_all_prefer :
      ∀ (c : Coordinator) (p : PLean.MachineRef),
        c.yesVotes p → preference p = true
  }

  Proof of_votes {
    prove votes ;
  }

  -- Prove the default framework invariants (`UniqueActions`,
  -- `IncreasingCount`, `ReceivedSubsetSent`). The Coordinator
  -- handlers contain `foreach … goto` blocks the auto chain
  -- can't close on its own; manual `@[pverifyProof]` proofs below
  -- discharge those via `triple_pforeach_with`.
  Proof of_default {
    prove default ;
  }

  -- Strengthening lemma: every sent `eCommit` was sent at a moment
  -- when every participant preferred YES. Derived (not assumed): the
  -- eYes handler only broadcasts eCommit under the guard `∀ p,
  -- inParticipants p → yesVotes p`, which puts every participant into
  -- `yesVotes`; `votes_all_prefer` (from `votes`) then gives unanimous
  -- YES, and `system_config` bridges `is_Participant` to `inParticipants`.
  --
  -- Uses `s.sent e = true` (not `inflight e s`) so the clause stays
  -- stable across `markReceived`.
  Lemma commit_sent {
    invariant commit_sent_implies_all_yes :
      ∀ e : Sig.Label,
        e is eCommit →
        s.sent e = true →
        ∀ p : PLean.MachineRef, is_Participant p s → preference p = true
  }

  Proof of_commit_sent {
    prove commit_sent using votes, system_config ;
  }

  -- Main safety theorem: every Participant in `Accepted` saw an
  -- `eCommit` from the Coordinator. By `commit_sent`, all participants
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

-- `send tgt <ev>` with `<ev> ≠ eCommit` preserves `commit_sent`: the
-- new label's action isn't an eCommit, so `is_eCommit e` rules it out;
-- old labels fall through to the pre-state hypothesis.
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
  -- `if (∀ p, inParticipants p → yesVotes p) then (foreach send eCommit;
  -- goto) else pure ()`. Clear the guard hypothesis in the then-branch —
  -- `DefaultInvariants` preservation doesn't need it, and leaving the `∀`
  -- in context derails `default_inv`'s per-conjunct `simp`/`solve_by_elim`.
  split
  · rename_i hGuard; clear hGuard
    apply triple_bind (cut := fun _ => DefaultInvariants)
    · apply triple_pforeach_with (Q := DefaultInvariants)
      intro _
      unfold PLean.send
      pverify
    intro _
    unfold PLean.goto
    pverify
  · pverify

/-! ## `votes`-bundle preservation on the Coordinator handlers.

`votes` = { `yes_implies_pref` (a2a), `votes_all_prefer` (a5) }.

- **eYes**: `+=` grows `yesVotes` by `resp.source`. The handled `lbl`
  is an eYes, so `yes_implies_pref` on it gives `preference resp.source`
  — the new member's preference. `votes_all_prefer` for pre-existing
  members transfers.
- **eNo / entry**: no eYes sent, `yesVotes` unchanged; both clauses
  transfer (sent grows only by a non-eYes label / eVoteReq).
- Participant handlers close by SMT. -/

-- The `votes` bundle plus "the acting Coordinator keeps its kind" — the
-- cut threaded through the eNo / entry handlers. Carrying `is_Coordinator
-- this` lets the closing `goto`'s `votes_all_prefer` clause bridge its
-- Coordinator-kind guard back to the pre-state in the `c.ref = this` case.
private def votesCo (this : MachineRef) : PProp Sig :=
  fun s => votes s ∧ is_Coordinator this s

-- A `send tgt <ev>` with `<ev> ≠ eYes` preserves `votesCo`.
private theorem send_nonyes_preserves_votesCo
    {ev : E} (h_ne_yes : ∀ p, ev ≠ E.eYes p) (this tgt : MachineRef) :
    triple (l := PProp Sig)
      (fun s => votesCo this s)
      (PLean.send (P := Sig) tgt ev)
      (fun _ s => votesCo this s) := by
  unfold PLean.send votesCo
  unfold votes yes_implies_pref votes_all_prefer
  pverify_step_wp
  intro x hYP hVP hCo
  refine ⟨⟨?_, hVP⟩, ?_⟩
  · intro e hisE hsent
    rw [Bool.or_eq_true, decide_eq_true_eq] at hsent
    rcases hsent with hEq | hOld
    · exfalso; rw [hEq] at hisE; unfold is_eYes at hisE
      cases ev with
      | eYes p => exact h_ne_yes p rfl
      | _ => simp_all
    · exact hYP e hisE hOld
  · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢
    exact hCo

-- A Coordinator `goto` preserves `votesCo` and lands in `votes`: the new
-- label is a `goto` (fails `is_eYes`), and `goto` leaves the `yesVotes`
-- container and every kind untouched. `is_Coordinator this` (in the cut)
-- bridges `votes_all_prefer`'s kind guard for the `c.ref = this` case.
private theorem goto_preserves_votes (this : MachineRef) (st : Sig.S) :
    triple (l := PProp Sig)
      (fun s => votesCo this s)
      (PLean.goto (P := Sig) this st G.unit)
      (fun _ s => votes s) := by
  unfold PLean.goto votesCo
  unfold votes yes_implies_pref votes_all_prefer
  pverify_step_wp
  intro x hYP hVP hCo
  refine ⟨?_, ?_⟩
  · intro e hisE hsent
    rw [Bool.or_eq_true, decide_eq_true_eq] at hsent
    rcases hsent with hEq | hOld
    · exfalso; rw [hEq] at hisE; unfold is_eYes at hisE; exact hisE
    · exact hYP e hisE hOld
  · intro c hcKind p hmem
    have hcPre : is_Coordinator c.ref x := by
      by_cases hct : c.ref = this
      · rw [hct]; exact hCo
      · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hcKind ⊢
        simp_all
    exact hVP c hcPre p hmem

-- eYes votes: the handled eYes label backs the new `yesVotes` member.
@[pverifyProof]
theorem Coordinator.WaitForResponses.eYes_correct_of_votes_votes
    (this : Coordinator) (param : eYes_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => votes s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧ is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eYes param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eYes_handler this param)
      (fun _ s => votes s) := by
  -- Reduce to a precondition recording the freshly-added voter's
  -- preference (`hPref`, from `yes_implies_pref` on the handled `lbl`)
  -- and the acting Coordinator's kind.
  apply triple_cons
    (pre := fun s => votesCo this.ref s ∧
      inflight lbl s ∧ preference param.source = true)
    (post := fun _ => votes)
  · rintro s ⟨⟨hYP, hVP⟩, hInfl, _, hCo, _, hAct⟩
    have hisLbl : is_eYes lbl := by simp only [is_eYes, hAct]
    have hPay : eYes_payload_of lbl = param := eYes_payload_of_spec lbl param hAct
    exact ⟨⟨⟨hYP, hVP⟩, hCo⟩, hInfl, by have := hYP lbl hisLbl hInfl.1; rwa [hPay] at this⟩
  · intro _ s h; exact h
  -- markReceived (only `received` grows) then the prelude get.
  apply triple_bind (cut := fun _ s => votesCo this.ref s ∧ preference param.source = true)
  · unfold PLean.markReceived votesCo votes yes_implies_pref votes_all_prefer
    pverify_step_wp
    rintro s hYP hVP hCo hInfl hPref; exact ⟨⟨⟨hYP, hVP⟩, hCo⟩, hPref⟩
  intro _
  unfold Coordinator.WaitForResponses.eYes_handler
  apply triple_bind (cut := fun _ s => votesCo this.ref s ∧ preference param.source = true)
  · unfold Coordinator.yesVotes_get votesCo votes yes_implies_pref votes_all_prefer
    pverify_step_wp
    rintro s hYP hVP hCo hPref; exact ⟨⟨⟨hYP, hVP⟩, hCo⟩, hPref⟩
  intro _
  -- The `+=` get whose value `yv` feeds the set. Record that every member
  -- of `yv` prefers YES (from `votes_all_prefer` at `this.ref`) — a fact
  -- that survives the get's value-abstraction, unlike the raw container.
  apply triple_bind
    (cut := fun yv s => votesCo this.ref s ∧ preference param.source = true ∧
      ∀ z, yv z → preference z = true)
  · unfold Coordinator.yesVotes_get votesCo votes yes_implies_pref votes_all_prefer
    pverify_step_wp
    rintro s hYP hVP hCo hPref
    exact ⟨⟨⟨hYP, hVP⟩, hCo⟩, hPref, fun z hz => hVP this hCo z hz⟩
  intro yv
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · unfold Coordinator.yesVotes_set votesCo votes yes_implies_pref votes_all_prefer
    pverify_step_wp
    rintro s hYP hVP hCo hPref hYvPref
    refine ⟨⟨hYP, ?_⟩, ?_⟩
    · -- grown `yesVotes` at `this.ref`: new member is `param.source`
      -- (uses `hPref`), else a member of `yv` (uses `hYvPref`); at other
      -- Coordinators the container is unchanged (uses `hVP`).
      intro c hcKind p hmem
      by_cases hct : c.ref = this.ref
      · rw [if_pos hct] at hmem
        simp only [decide_eq_true_eq] at hmem
        rcases hmem with rfl | hold
        · exact hPref
        · exact hYvPref p hold
      · rw [if_neg hct] at hmem
        exact hVP c hcKind p hmem
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  -- trailing `+=` get preserves `votesCo`.
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · unfold Coordinator.yesVotes_get votesCo votes yes_implies_pref votes_all_prefer
    pverify_step_wp
    rintro s hYP hVP hCo; exact ⟨⟨hYP, hVP⟩, hCo⟩
  intro _
  -- `if (∀ p, inParticipants p → yesVotes p) then (foreach send eCommit;
  -- goto) else pure ()`.
  split
  · apply triple_bind (cut := fun _ => votesCo this.ref)
    · apply triple_pforeach_with (Q := votesCo this.ref)
      intro p
      exact send_nonyes_preserves_votesCo (fun _ => by nofun) this.ref p
    intro _
    exact goto_preserves_votes this.ref Coordinator.Committed_st
  · -- else-branch `pure ()`: `votesCo → votes` and the program is a no-op.
    apply triple_cons (pre := votesCo this.ref) (post := fun _ => votesCo this.ref)
    · intro s h; exact h
    · intro _ s ⟨h, _⟩; exact h
    unfold votesCo votes yes_implies_pref votes_all_prefer
    pverify_step_wp
    rintro s hYP hVP hCo; exact ⟨⟨hYP, hVP⟩, hCo⟩

-- eNo votes: sends eAbort, not eYes; yesVotes unchanged.
@[pverifyProof]
theorem Coordinator.WaitForResponses.eNo_correct_of_votes_votes
    (this : Coordinator) (param : eNo_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => votes s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧ is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eNo param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eNo_handler this param)
      (fun _ s => votes s) := by
  apply triple_cons (pre := votesCo this.ref) (post := fun _ => votes)
  · intro s ⟨h, _, _, hCo, _, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · unfold PLean.markReceived votesCo votes yes_implies_pref votes_all_prefer; pverify
  intro _
  unfold Coordinator.WaitForResponses.eNo_handler
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · unfold Coordinator.yesVotes_get votesCo votes yes_implies_pref votes_all_prefer; pverify
  intro _
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · apply triple_pforeach_with (Q := votesCo this.ref)
    intro p
    exact send_nonyes_preserves_votesCo (fun _ => by nofun) this.ref p
  intro _
  exact goto_preserves_votes this.ref Coordinator.Aborted_st

-- entry votes: sends eVoteReq, not eYes; yesVotes unchanged.
@[pverifyProof]
theorem Coordinator.Init.entry_correct_of_votes_votes
    (this : Coordinator) :
    triple (l := PProp Sig)
      (fun s => votes s ∧ is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.Init_st)
      (Coordinator.Init.entry this)
      (fun _ s => votes s) := by
  apply triple_cons (pre := votesCo this.ref) (post := fun _ => votes)
  · intro s ⟨h, hCo, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  unfold Coordinator.Init.entry
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · unfold Coordinator.yesVotes_get votesCo votes yes_implies_pref votes_all_prefer; pverify
  intro _
  apply triple_bind (cut := fun _ => votesCo this.ref)
  · apply triple_pforeach_with (Q := votesCo this.ref)
    intro p
    exact send_nonyes_preserves_votesCo (fun _ => by nofun) this.ref p
  intro _
  exact goto_preserves_votes this.ref Coordinator.WaitForResponses_st

/-! ## `system_config` preservation on the Coordinator handlers.

`participant_set : ∀ p, inParticipants p = true ↔ is_Participant p s`.
`inParticipants` is a static function and machine *kinds* never change,
so this transfers across every step. The only subtlety is `goto`, which
rewrites the acting Coordinator's `currentState`: `is_Participant` folds
`kind = 2 ∧ currentState ∈ Participant-states`, and a Coordinator's kind
is `1`, so the acting machine is a non-Participant before and after. -/

-- A Coordinator `goto`: the acting machine's kind stays `1 ≠ 2`, so it is
-- a non-Participant in both states; every other machine is untouched. So
-- `participant_set` transfers, given the actor is a Coordinator (`hCo`).
private theorem goto_preserves_system_config (this : MachineRef) (st : Sig.S) :
    triple (l := PProp Sig)
      (fun s => system_config s ∧ is_Coordinator this s)
      (PLean.goto (P := Sig) this st G.unit)
      (fun _ s => system_config s) := by
  unfold PLean.goto system_config participant_set
  pverify_step_wp
  intro x hPS hCo p
  simp only [is_Participant, Participant_allocated, Participant_kind,
             is_Coordinator, Coordinator_allocated, Coordinator_kind] at hPS hCo ⊢
  by_cases hpt : p = this <;> simp_all

-- The `system_config` cut threaded through the Coordinator handlers.
private def scCo (this : MachineRef) : PProp Sig :=
  fun s => system_config s ∧ is_Coordinator this s

private theorem send_preserves_scCo (this tgt : MachineRef) (ev : E) :
    triple (l := PProp Sig)
      (fun s => scCo this s)
      (PLean.send (P := Sig) tgt ev)
      (fun _ s => scCo this s) := by
  unfold PLean.send scCo system_config participant_set
  pverify_step_wp
  intro x hPS hCo
  refine ⟨fun p => ?_, ?_⟩
  · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢
    exact hPS p
  · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo

@[pverifyProof]
theorem Coordinator.WaitForResponses.eYes_correct_of_system_config_system_config
    (this : Coordinator) (param : eYes_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => system_config s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧ is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eYes param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eYes_handler this param)
      (fun _ s => system_config s) := by
  apply triple_cons (pre := scCo this.ref) (post := fun _ => system_config)
  · intro s ⟨h, _, _, hCo, _, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold PLean.markReceived scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  unfold Coordinator.WaitForResponses.eYes_handler
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold Coordinator.yesVotes_get scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold Coordinator.yesVotes_get scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold Coordinator.yesVotes_set scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold Coordinator.yesVotes_get scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  split
  · apply triple_bind (cut := fun _ => scCo this.ref)
    · apply triple_pforeach_with (Q := scCo this.ref)
      intro p
      exact send_preserves_scCo this.ref p E.eCommit
    intro _
    apply triple_cons (pre := scCo this.ref) (post := fun _ => system_config)
    · intro s h; exact h
    · intro _ s h; exact h
    exact goto_preserves_system_config this.ref Coordinator.Committed_st
  · apply triple_cons (pre := scCo this.ref) (post := fun _ => scCo this.ref)
    · intro s h; exact h
    · intro _ s ⟨h, _⟩; exact h
    unfold scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo; exact ⟨hPS, hCo⟩

@[pverifyProof]
theorem Coordinator.WaitForResponses.eNo_correct_of_system_config_system_config
    (this : Coordinator) (param : eNo_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => system_config s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧ is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eNo param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eNo_handler this param)
      (fun _ s => system_config s) := by
  apply triple_cons (pre := scCo this.ref) (post := fun _ => system_config)
  · intro s ⟨h, _, _, hCo, _, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold PLean.markReceived scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  unfold Coordinator.WaitForResponses.eNo_handler
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold Coordinator.yesVotes_get scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  apply triple_bind (cut := fun _ => scCo this.ref)
  · apply triple_pforeach_with (Q := scCo this.ref)
    intro p
    exact send_preserves_scCo this.ref p E.eAbort
  intro _
  apply triple_cons (pre := scCo this.ref) (post := fun _ => system_config)
  · intro s h; exact h
  · intro _ s h; exact h
  exact goto_preserves_system_config this.ref Coordinator.Aborted_st

@[pverifyProof]
theorem Coordinator.Init.entry_correct_of_system_config_system_config
    (this : Coordinator) :
    triple (l := PProp Sig)
      (fun s => system_config s ∧ is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.Init_st)
      (Coordinator.Init.entry this)
      (fun _ s => system_config s) := by
  apply triple_cons (pre := scCo this.ref) (post := fun _ => system_config)
  · intro s ⟨h, hCo, _⟩; exact ⟨h, hCo⟩
  · intro _ s h; exact h
  unfold Coordinator.Init.entry
  apply triple_bind (cut := fun _ => scCo this.ref)
  · unfold Coordinator.yesVotes_get scCo system_config participant_set
    pverify_step_wp
    intro x hPS hCo
    refine ⟨fun p => ?_, ?_⟩
    · simp only [is_Participant, Participant_allocated, Participant_kind] at hPS ⊢; exact hPS p
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  apply triple_bind (cut := fun _ => scCo this.ref)
  · apply triple_pforeach_with (Q := scCo this.ref)
    intro p
    exact send_preserves_scCo this.ref p E.eVoteReq
  intro _
  apply triple_cons (pre := scCo this.ref) (post := fun _ => system_config)
  · intro s h; exact h
  · intro _ s h; exact h
  exact goto_preserves_system_config this.ref Coordinator.WaitForResponses_st

/-! ## `commit_sent` preservation on the Coordinator handlers.

`commit_sent_implies_all_yes : ∀ e, e is eCommit → s.sent e = true →
∀ p, is_Participant p → preference p = true`.

- **Init.entry / eNo**: no eCommit is sent, so old in-flight eCommits
  transfer (sent is monotone).
- **eYes**: the `then` branch broadcasts eCommit. Its preference
  guarantee is *derived*: the guard `∀ p, inParticipants p → yesVotes p`
  puts every participant in `yesVotes`, and `votes_all_prefer` (from the
  `votes` bundle) then gives `preference` (with `system_config` bridging
  `is_Participant` ↔ `inParticipants`). -/

@[pverifyProof]
theorem Coordinator.Init.entry_correct_of_commit_sent_commit_sent_using_votes_system_config
    (this : Coordinator) :
    triple (l := PProp Sig)
      (fun s => (commit_sent s ∧ votes s ∧ system_config s) ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.Init_st)
      (Coordinator.Init.entry this)
      (fun _ s => commit_sent s) := by
  apply triple_cons (pre := commit_sent) (post := fun _ => commit_sent)
  · intro s ⟨⟨h, _, _⟩, _, _⟩; exact h
  · intro _ s h; exact h
  unfold Coordinator.Init.entry
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold Coordinator.yesVotes_get; pverify
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
  · exfalso; rw [hEq] at hisE; unfold is_eCommit at hisE; exact hisE
  · pverify_machine_has_type hppPre : Participant pp from hpp
    exact hCS e hisE hOld pp hppPre

@[pverifyProof]
theorem Coordinator.WaitForResponses.eNo_correct_of_commit_sent_commit_sent_using_votes_system_config
    (this : Coordinator) (param : eNo_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (commit_sent s ∧ votes s ∧ system_config s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eNo param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eNo_handler this param)
      (fun _ s => commit_sent s) := by
  apply triple_cons (pre := commit_sent) (post := fun _ => commit_sent)
  · intro s ⟨⟨h, _, _⟩, _, _, _, _, _⟩; exact h
  · intro _ s h; exact h
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold PLean.markReceived
    pverify_step_wp
    intro s hCS e hisE ha pp hpp
    exact hCS e hisE ha pp hpp
  intro _
  unfold Coordinator.WaitForResponses.eNo_handler
  apply triple_bind (cut := fun _ => commit_sent)
  · unfold Coordinator.yesVotes_get; pverify
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
  · exfalso; rw [hEq] at hisE; unfold is_eCommit at hisE; exact hisE
  · pverify_machine_has_type hppPre : Participant pp from hpp
    exact hCS e hisE hOld pp hppPre

set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem Coordinator.WaitForResponses.eYes_correct_of_commit_sent_commit_sent_using_votes_system_config
    (this : Coordinator) (param : eYes_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (commit_sent s ∧ votes s ∧ system_config s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧
        is_Coordinator this.ref s ∧
        (s.machines this.ref).currentState = Coordinator.WaitForResponses_st ∧
        lbl.action = EventOrGoto.event (E.eYes param))
      (do PLean.markReceived (P := Sig) lbl;
          Coordinator.WaitForResponses.eYes_handler this param)
      (fun _ s => commit_sent s) := by
  -- Cut carried up to the guard: `commit_sent`, `system_config`, the
  -- acting kind, `votes_all_prefer` (grown-set members prefer), and the
  -- new member's preference `preference param.source` (from
  -- `yes_implies_pref` on the handled `lbl`, needed to keep
  -- `votes_all_prefer` through the `+=`). At the guard these combine —
  -- guard ⇒ participants ⊆ yesVotes, `votes_all_prefer` ⇒ members prefer,
  -- `system_config` ⇒ `is_Participant ↔ inParticipants` — to give
  -- unanimous preference, which then discharges each broadcast eCommit.
  apply triple_cons
    (pre := fun s => commit_sent s ∧ system_config s ∧
      is_Coordinator this.ref s ∧
      (∀ (c : Coordinator), is_Coordinator c.ref s →
        ∀ p : PLean.MachineRef,
          s.containers.Coordinator_yesVotes (c.ref, p) = true → preference p = true) ∧
      preference param.source = true)
    (post := fun _ => commit_sent)
  · rintro s ⟨⟨hCS, hV, hSC⟩, hInfl, _, hCo, _, hAct⟩
    have hisLbl : is_eYes lbl := by simp only [is_eYes, hAct]
    have hPay : eYes_payload_of lbl = param := eYes_payload_of_spec lbl param hAct
    exact ⟨hCS, hSC, hCo, hV.2, by have := hV.1 lbl hisLbl hInfl.1; rwa [hPay] at this⟩
  · intro _ s h; exact h
  apply triple_bind
    (cut := fun _ s => commit_sent s ∧ system_config s ∧
      is_Coordinator this.ref s ∧
      (∀ (c : Coordinator), is_Coordinator c.ref s →
        ∀ p : PLean.MachineRef,
          s.containers.Coordinator_yesVotes (c.ref, p) = true → preference p = true) ∧
      preference param.source = true)
  · unfold PLean.markReceived commit_sent commit_sent_implies_all_yes
      system_config participant_set
    pverify
  intro _
  unfold Coordinator.WaitForResponses.eYes_handler
  apply triple_bind
    (cut := fun _ s => commit_sent s ∧ system_config s ∧
      is_Coordinator this.ref s ∧
      (∀ (c : Coordinator), is_Coordinator c.ref s →
        ∀ p : PLean.MachineRef,
          s.containers.Coordinator_yesVotes (c.ref, p) = true → preference p = true) ∧
      preference param.source = true)
  · unfold Coordinator.yesVotes_get commit_sent commit_sent_implies_all_yes
      system_config participant_set
    pverify
  intro _
  -- `+=` get whose value feeds the set: record grown-set members prefer.
  apply triple_bind
    (cut := fun yv s => commit_sent s ∧ system_config s ∧
      is_Coordinator this.ref s ∧
      (∀ (c : Coordinator), is_Coordinator c.ref s →
        ∀ p : PLean.MachineRef,
          s.containers.Coordinator_yesVotes (c.ref, p) = true → preference p = true) ∧
      preference param.source = true ∧ (∀ z, yv z → preference z = true))
  · unfold Coordinator.yesVotes_get commit_sent commit_sent_implies_all_yes
      system_config participant_set
    pverify_step_wp
    rintro s hCS hSC hCo hVP hPref
    exact ⟨hCS, hSC, hCo, hVP, hPref, fun z hz => hVP this hCo z hz⟩
  intro yv
  apply triple_bind
    (cut := fun _ s => commit_sent s ∧ system_config s ∧
      is_Coordinator this.ref s ∧
      (∀ (c : Coordinator), is_Coordinator c.ref s →
        ∀ p : PLean.MachineRef,
          s.containers.Coordinator_yesVotes (c.ref, p) = true → preference p = true))
  · unfold Coordinator.yesVotes_set commit_sent commit_sent_implies_all_yes
      system_config participant_set
    pverify_step_wp
    rintro s hCS hSC hCo hVP hPref hYvPref
    refine ⟨hCS, hSC, ?_, ?_⟩
    · simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
    · intro c hcKind p hmem
      by_cases hct : c.ref = this.ref
      · rw [if_pos hct] at hmem
        simp only [decide_eq_true_eq] at hmem
        rcases hmem with rfl | hold
        · exact hPref
        · exact hYvPref p hold
      · rw [if_neg hct] at hmem; exact hVP c hcKind p hmem
  intro _
  -- Trailing `+=` get whose value `yv` is what the guard ranges over.
  -- Record `system_config` and `∀ z, yv z → preference z` (grown-set
  -- members prefer) so the guard yields unanimous preference directly.
  apply triple_bind
    (cut := fun yv s => commit_sent s ∧ system_config s ∧
      (∀ z, yv z → preference z = true))
  · unfold Coordinator.yesVotes_get commit_sent commit_sent_implies_all_yes
      system_config participant_set
    pverify_step_wp
    rintro s hCS hSC hCo hVP
    exact ⟨hCS, hSC, fun z hz => hVP ⟨this.ref⟩ hCo z hz⟩
  intro yv
  split
  · -- then-branch: guard `hGuard : ∀ p, inParticipants p → yv p`.
    rename_i hGuard
    apply triple_cons
      (pre := fun s => commit_sent s ∧
        (∀ pp : PLean.MachineRef, is_Participant pp s → preference pp = true))
      (post := fun _ => commit_sent)
    · rintro s ⟨hCS, hSC, hMemPref⟩
      refine ⟨hCS, fun pp hpp => ?_⟩
      -- is_Participant pp → inParticipants pp (system_config) → pp ∈ yv
      -- (guard) → preference pp (grown-set members prefer).
      exact hMemPref pp (hGuard pp ((hSC pp).mpr hpp))
    · intro _ s h; exact h
    apply triple_bind
      (cut := fun _ s => commit_sent s ∧
        (∀ pp : PLean.MachineRef, is_Participant pp s → preference pp = true))
    · apply triple_pforeach_with
        (Q := fun s => commit_sent s ∧
          ∀ pp : PLean.MachineRef, is_Participant pp s → preference pp = true)
      intro p
      unfold PLean.send commit_sent commit_sent_implies_all_yes
      pverify_step_wp
      intro x hCS hAllYes
      refine ⟨?_, ?_⟩
      · intro e hisE ha pp hp
        rw [Bool.or_eq_true, decide_eq_true_eq] at ha
        rcases ha with hEq | hOld
        · exact hAllYes pp hp     -- new eCommit: recipient prefers (hAllYes)
        · exact hCS e hisE hOld pp hp
      · intro pp hp
        simp only [is_Participant, Participant_allocated, Participant_kind] at hp ⊢
        exact hAllYes pp hp
    intro _
    unfold PLean.goto commit_sent commit_sent_implies_all_yes
    pverify_step_wp
    intro s hCS _hAllYes e hisE ha pp hp
    rw [Bool.or_eq_true, decide_eq_true_eq] at ha
    rcases ha with hEq | hOld
    · exfalso; rw [hEq] at hisE; unfold is_eCommit at hisE; exact hisE
    · pverify_machine_has_type hppPre : Participant pp from hp
      exact hCS e hisE hOld pp hppPre
  · -- else-branch: pure (); commit_sent transfers.
    apply triple_cons (pre := commit_sent) (post := fun _ => commit_sent)
    · intro s ⟨h, _⟩; exact h
    · intro _ s h; exact h
    unfold commit_sent commit_sent_implies_all_yes
    pverify

/-! ## Safety inductive steps on Coordinator handlers.

Coordinator handlers don't change Participant `currentState`, so
`stateOf p.ref` for a Participant is unchanged and post-state `safety`
follows from pre-state `safety`. -/

@[pverifyProof]
theorem base_of_safety_accepted_implies_all_prefer
    (s : GlobalState Sig) :
    InitConditions s → accepted_implies_all_prefer s := by
  intro hInit p1 hp1 hAccp
  unfold InitConditions at hInit
  have hUndec : ∀ p : Participant,
      is_Participant p.ref s → stateOf p.ref s = Participant.Undecided_st := by
    tauto
  have hp1Undec := hUndec p1 hp1
  rw [hp1Undec] at hAccp
  exact S.noConfusion hAccp

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
  show triple (l := PProp Sig) _ (Coordinator.yesVotes_get this.ref >>= fun _ => _) _
  apply triple_bind
    (cut := fun _ : Set MachineRef =>
      (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
  · unfold Coordinator.yesVotes_get
    pverify_step_wp
    intro s hSafe hCo; exact ⟨hSafe, hCo⟩
  intro _
  apply triple_bind
    (cut := fun _ : Unit => (fun s => safety s ∧ is_Coordinator this.ref s : PProp Sig))
  · apply triple_pforeach_with (Q := fun s => safety s ∧ is_Coordinator this.ref s)
    intro p
    unfold PLean.send
    pverify_step_wp
    intro x hSafe hCo
    refine ⟨hSafe, ?_⟩
    simp only [is_Coordinator, Coordinator_allocated, Coordinator_kind] at hCo ⊢; exact hCo
  intro _
  unfold PLean.goto
  pverify_step_wp
  intro s hSafe hCo p1 hp1 hAccp pp hpp
  pverify_machine_has_type hp1Pre : Participant p1.ref from hp1
  pverify_machine_has_type hppPre : Participant pp from hpp
  have hp1NotThis : p1.ref ≠ this.ref := participant_ne_this this p1.ref s hCo hp1Pre
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
  split
  ·
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
  · pverify_step_wp; grind

end TwoPhaseCommit

set_option pverify.failOnIncomplete true in
set_option maxHeartbeats 1000000 in
#pverify    TwoPhaseCommit
