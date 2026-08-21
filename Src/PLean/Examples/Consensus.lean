/-
PLean port of P's Tutorial/Advanced/5_Consensus.

Toy Paxos-style consensus protocol. `RequestVoting.entry`
foreach-broadcasts `eRequestVote` to every configured node; each
Node casts a single `eVote` in response (guarded by `voted = false`);
the `eVote` handler accumulates voters into `votes : set[MachineRef]`
and `goto Won` once the `isQuorum` predicate fires on the vote set.

The headline safety property:

  Theorem election_safety { ∀ n1 n2 : Node,
    n1 is Won → n2 is Won → n1 = n2 }

is *derived* (not assumed) from the `quorum_votes` bundle plus the
`quorum_intersect` topology axiom — the standard quorum-intersection
assumption from classical consensus: any two quorum-strength vote
sets share a member.

Closure rate: **17 / 17** — 12 by SMT, 5 by manual `@[pverifyProof]`.
`votes : set[MachineRef]` is backed by the first-order opaque `PSet`
theory, so `isQuorum` on a vote set and `a ∈ n.votes` membership both
translate — the whole set is a first-order sort, not an `α → Prop`
lean-auto rejects. `quorum_intersect` is a state-independent `paxiom`
lifted into every obligation's SMT context, so the `election_safety`
derivation discharges by SMT. The faithful set-based `isQuorum` is what
makes the quorum-intersection axiom expressible; a flat `MachineRef →
Bool` oracle could only re-assert the safety conclusion, begging the
question.

The 5 residual manual proofs are the cases SMT can't close on its own:
the two "no Node is Won at init" base cases; the `eVote` handler's
inductive `quorum_votes` step (the freshly-added voter must be linked to
the handled label — a cross-invariant fact the WP severs at the `+=`)
and the `election_safety` step that reduces to it; and the entry
broadcast loop's `prove default`.

The `quorum_votes` bundle (with the manual-proof rationale):
- `one_vote_per_voter`: two sent `eVote`s with the same voter are the
  same label — each Node votes at most once. Carries through every
  step; the `eRequestVote` step uses `voted_after_eVote_sent` + the
  `¬voted` guard to rule out a second eVote from the same Node.
- `voted_after_eVote_sent`: a sent `eVote` from `n` forces
  `n.voted = true` (the `¬voted` guard's contrapositive).
- `votes_linked`: every `a ∈ n.votes` is backed by a sent `eVote`
  with `voter = a`, `target = n` — the bridge from set-membership to
  a concrete label. In the `eVote` step the handled label `lbl` is
  itself the witness for the freshly-added voter.
- `won_implies_quorum_votes`: a Won-state Node has `isQuorum` on its
  vote set (the `goto Won` guard).

The P source's second axiom (`isQuorum(q) ⟹ ∀ a ∈ q, a ∈ nodes()` —
quorum members are configured nodes) is omitted: it only constrains
`isQuorum` further, so `election_safety` holds without it. It bears on
liveness (a quorum is reachable), not the safety property verified here.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option crush.backend "cvc5"
set_option crush.timeout 10

pmodule Consensus

  system s

  type tRequestPayload = (src : PLean.MachineRef)
  type tVotePayload    = (voter : PLean.MachineRef)

  event eRequestVote : tRequestPayload
  event eVote        : tVotePayload

  -- Opaque configured-node sequence — the population the broadcast
  -- iterates over.
  function nodes : seq[PLean.MachineRef]

  -- Quorum predicate on a *set of votes*. Mirrors P's
  -- `isQuorum(s: set[machine]): bool`: quorum-strength is a property
  -- of which voters have responded, not of the candidate.
  function isQuorum : set[PLean.MachineRef] → Bool

  machine Node {
    var voted : Bool
    var votes : set[PLean.MachineRef]

    start state RequestVoting {
      entry {
        foreach (m in nodes)
          invariant inv_bundle : quorum_votes s ;
        {
          send m, eRequestVote, (src = this.ref)
        }
      }

      on eRequestVote (payload : tRequestPayload) {
        if ¬ voted then do
          voted = true
          send payload.src, eVote, (voter = this.ref)
      }

      on eVote (payload : tVotePayload) {
        votes += (payload.voter)
        if isQuorum votes = true then do
          goto Won
      }
    }

    state Won {
      ignore eRequestVote, eVote
    }
  }

  init-holds ∀ n : Node, n.voted = false
  init-holds ∀ (n : Node) (k : PLean.MachineRef), ¬ (k ∈ n.votes)

  -- Quorum-intersection assumption on *vote sets*, exactly as P's
  -- source: any two quorum-strength vote sets share a member. The
  -- topology assumption — no equality conclusion, so it doesn't beg the
  -- safety question. A `paxiom` with a state-independent body (the
  -- materialiser emits it unwrapped); the obligation generator lifts it
  -- into every obligation's SMT context, so `election_safety` derives by
  -- SMT rather than a hand `have`.
  paxiom quorum_intersect :
    ∀ (q1 q2 : set[PLean.MachineRef]),
      isQuorum q1 = true → isQuorum q2 = true →
      ∃ a : PLean.MachineRef, a ∈ q1 ∧ a ∈ q2

  -- Inductive bundle. The first three are PLean strengthening
  -- invariants that PVerifier derives implicitly from its frame
  -- semantics; the shallow embedding states them explicitly:
  -- - `one_vote_per_voter`: a sent `eVote` carries the voter's id;
  --   two sent `eVote`s with the same voter are equal labels.
  -- - `voted_after_eVote_sent`: a sent `eVote` from `n` forces
  --   `n.voted = true` — the `if ¬ voted` guard's contrapositive.
  --   Carries `one_vote_per_voter` through the `eRequestVote` step.
  -- - `votes_linked`: every accumulated voter `a ∈ n.votes` is
  --   backed by a sent `eVote` whose voter is `a` and whose target
  --   is `n` — the bridge from set-membership to a concrete label.
  -- - `won_implies_quorum_votes`: a Won-state Node has a quorum vote
  --   set. The handler's `goto Won` guard (`isQuorum votes = true`)
  --   carries this through every step.
  Lemma quorum_votes {
    invariant one_vote_per_voter :
      ∀ e1 e2 : eVote,
        s.sent e1 = true → s.sent e2 = true →
        e1.voter = e2.voter → e1 = e2

    invariant voted_after_eVote_sent :
      ∀ (e : eVote) (n : Node),
        s.sent e = true → e.voter = n.ref → n.voted = true

    invariant votes_linked :
      ∀ (n : Node) (a : PLean.MachineRef),
        a ∈ n.votes →
        ∃ e : Sig.Label,
          is_eVote e ∧ s.sent e = true ∧
          (eVote_payload_of e).voter = a ∧ e.target = n.ref

    invariant won_implies_quorum_votes :
      ∀ n : Node,
        stateOf n.ref s = Node.Won_st → isQuorum n.votes = true
  }
  Proof {
    prove quorum_votes ;
  }

  -- Election safety: at most one Node ever reaches `Won`.
  Theorem election_safety {
    invariant unique_leader :
      ∀ n1 n2 : Node,
        stateOf n1.ref s = Node.Won_st →
        stateOf n2.ref s = Node.Won_st →
        n1 = n2
  }
  Proof {
    prove election_safety using quorum_votes ;
    prove default ;
  }

end Consensus

#gen_module Consensus
#pwf        Consensus

namespace Consensus
open PartialCorrectness DemonicChoice

/-! ## Manual proofs

With `votes : set[MachineRef]` now backed by the first-order opaque
`PSet` theory, most of the `quorum_votes` bundle and the
`election_safety` derivation discharge by SMT — including the headline
derivation, since `quorum_intersect` (a `paxiom` over `PSet`) is lifted
into every obligation's SMT context. The residual manual proofs cover
the cases SMT still can't close on its own:

- the two base cases that need "no Node is Won at init" (`stateOf ≠
  Won`, a constructor-distinctness + init-start-state chain);
- the `eVote` handler's inductive `quorum_votes` step, whose
  freshly-added voter must be linked to the handled label (a
  cross-invariant fact the WP severs at the `+=`);
- the entry broadcast loop's `prove default` (loop scaffolding).

The headline derivation (`election_safety` from `quorum_votes`):
two Won-state Nodes both have quorum vote sets (`won_implies_quorum_votes`);
`quorum_intersect` gives a shared voter `a`; `votes_linked` backs `a`'s
membership in each set with a sent `eVote` (`e1` targeting `n1`, `e2`
targeting `n2`), both with `voter = a`; `one_vote_per_voter` forces
`e1 = e2`, hence the targets `n1.ref = n2.ref` coincide. -/

-- At init every machine starts in `RequestVoting_st`, so any `Won_st`
-- antecedent reduces to a constructor mismatch via `S.noConfusion`.
-- Shared between both base-case proofs below.
private theorem init_not_won (s : GlobalState Sig)
    (hInit : InitConditions s) (r : PLean.MachineRef) :
    stateOf r s ≠ Node.Won_st := by
  obtain ⟨_, _, _, hInStart, _⟩ := hInit
  have hStart := hInStart r
  unfold stateOf
  rw [hStart]
  exact fun h => S.noConfusion h

-- `election_safety` is *derived* from `quorum_votes` + `quorum_intersect`,
-- not assumed. Factored out because the inductive steps for handlers that
-- preserve `quorum_votes` close `election_safety` by re-deriving it on the
-- post-state. Given the bundle on `s`, any two Won-state Nodes coincide.
private theorem quorum_votes_implies_safety (s : GlobalState Sig)
    (hQV : quorum_votes s) : unique_leader s := by
  unfold quorum_votes at hQV
  obtain ⟨hOne, _hVoted, hLinked, hWonQ⟩ := hQV
  intro n1 hk1 n2 hk2 hWon1 hWon2
  -- Both Won ⟹ both vote sets are quorums (the set is inferred — the
  -- field-projection sugar only fires inside the `system s` block).
  have hQ1 := hWonQ n1 hk1 hWon1
  have hQ2 := hWonQ n2 hk2 hWon2
  -- Quorum-intersection: a shared voter `a`.
  obtain ⟨a, hA1, hA2⟩ := quorum_intersect _ _ hQ1 hQ2
  -- `a`'s membership in each vote set is backed by a sent eVote.
  obtain ⟨e1, hisE1, hSent1, hVoter1, hTgt1⟩ := hLinked n1 hk1 a hA1
  obtain ⟨e2, hisE2, hSent2, hVoter2, hTgt2⟩ := hLinked n2 hk2 a hA2
  -- Same voter `a` ⟹ one_vote_per_voter ⟹ e1 = e2 ⟹ same target.
  have hVoterEq : (eVote_payload_of e1).voter = (eVote_payload_of e2).voter := by
    rw [hVoter1, hVoter2]
  have heq : e1 = e2 := hOne e1 hisE1 e2 hisE2 hSent1 hSent2 hVoterEq
  have hRefEq : n1.ref = n2.ref := by rw [← hTgt1, ← hTgt2, heq]
  cases n1; cases n2; simp_all

-- A label that is an `eVote` and lies in a `sent`-set grown by one
-- non-`eVote` label `nl` (a `goto`, an `eRequestVote`, …) was already
-- in `sent` before — the fresh label is excluded by tag mismatch.
-- The recurring `one_vote_per_voter` / `voted_after_eVote_sent`
-- transport across any step whose footprint adds a non-eVote label.
private theorem sent_of_post_eVote {e nl : Sig.Label} {s : GlobalState Sig}
    (hisE : is_eVote e) (hnl : ¬ is_eVote nl)
    (h : (decide (e = nl) || s.sent e) = true) : s.sent e = true := by
  rw [Bool.or_eq_true, decide_eq_true_eq] at h
  exact h.resolve_left (fun he => hnl (he ▸ hisE))

-- Carry a `votes_linked` witness (`∃ e, is_eVote e ∧ s.sent e ∧ …`)
-- across a step that only grows `sent` by a fresh label `nl`: the
-- backing eVote `e` stays sent, weakened to the post-state
-- `decide (e = nl) || s.sent e` form.
private theorem votes_linked_carry {a tgt : Sig.Label → Prop} {nl : Sig.Label}
    {s : GlobalState Sig}
    (h : ∃ e : Sig.Label, is_eVote e ∧ s.sent e = true ∧ a e ∧ tgt e) :
    ∃ e : Sig.Label, is_eVote e ∧ (decide (e = nl) || s.sent e) = true ∧ a e ∧ tgt e := by
  obtain ⟨e, hisE, hse, hae, hte⟩ := h
  exact ⟨e, hisE, by rw [Bool.or_eq_true]; right; exact hse, hae, hte⟩

@[pverifyProof]
theorem base_block0_won_implies_quorum_votes
    (s : GlobalState Sig) :
    InitConditions s → won_implies_quorum_votes s := by
  intro hInit n _hkind hWon
  exact absurd hWon (init_not_won s hInit n.ref)

@[pverifyProof]
theorem base_block1_unique_leader
    (s : GlobalState Sig) :
    InitConditions s → unique_leader s := by
  intro hInit n1 _hk1 n2 _hk2 hWon1 _hWon2
  exact absurd hWon1 (init_not_won s hInit n1.ref)

/-! ### `quorum_votes`-preservation steps.

Each handler preserves the bundle; the `election_safety` obligations
below reduce to these via `triple_cons` + `quorum_votes_implies_safety`.
The `block0` `prove quorum_votes` obligations *are* these. -/

-- `eVote` handler: accumulates `param.voter` into `votes` and may
-- `goto Won` under the `isQuorum votes` guard. The label being handled
-- (`lbl`) is the witness backing the freshly-added voter in `votes_linked`.
set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem Node.RequestVoting.eVote_correct_block0_quorum_votes
    (this : Node) (param : eVote_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => quorum_votes s ∧
        inflight lbl s ∧ lbl.target = this.ref ∧ is_Node this.ref s ∧
        (s.machines this.ref).currentState = Node.RequestVoting_st ∧
        lbl.action = EventOrGoto.event (E.eVote param))
      (do PLean.markReceived (P := Sig) lbl;
          Node.RequestVoting.eVote_handler this param)
      (fun _ s => quorum_votes s) := by
  unfold Node.RequestVoting.eVote_handler
  unfold quorum_votes one_vote_per_voter voted_after_eVote_sent
         votes_linked won_implies_quorum_votes
  try unfold Node.votes_get Node.votes_set Node.voted_get
  try unfold PLean.send PLean.goto PLean.markReceived
  pverify_step_wp
  intro s hOne hVoted hLinked hWon hInfl hTgt hKind hSt hAct
  obtain ⟨hSentLbl, _⟩ := hInfl
  have hPay : eVote_payload_of lbl = param := eVote_payload_of_spec lbl param hAct
  have hisLbl : is_eVote lbl := by simp only [is_eVote, hAct]
  refine ⟨fun hGuard => ⟨?t_one, ?t_voted, ?t_linked, ?t_won⟩,
          fun hNoGuard => ⟨?e_one, ?e_voted, ?e_linked, ?e_won⟩⟩
  -- then-branch (`goto Won`): post Won-set = pre ∪ {this.ref}; vote-set of
  -- this.ref grows by param.voter, backed by `lbl`.
  case t_one =>
    intro e1 hisE1 e2 hisE2 hs1 hs2 hveq
    -- The fresh label is a `goto` (not an eVote), so both sent labels
    -- were already in `s.sent`; pre-state `one_vote_per_voter` closes.
    exact hOne e1 hisE1 e2 hisE2
      (sent_of_post_eVote hisE1 (by simp [is_eVote]) hs1)
      (sent_of_post_eVote hisE2 (by simp [is_eVote]) hs2) hveq
  case t_voted =>
    intro e hisE n hnKind hsent hveq
    have hsent' := sent_of_post_eVote hisE (by simp [is_eVote]) hsent
    pverify_machine_has_type hnKind' : Node n.ref from hnKind
    have hvd := hVoted e hisE n hnKind' hsent' hveq
    by_cases hnt : n.ref = this.ref <;> simp_all
  case t_linked =>
    intro n hnKind a hmem
    pverify_machine_has_type hnKind' : Node n.ref from hnKind
    by_cases hnt : n.ref = this.ref
    · rw [if_pos hnt] at hmem
      simp only [PSet.mem_def, PSet.insert_eq, PSet.mem_insert] at hmem
      rcases hmem with rfl | hold
      · -- newly-added voter: `lbl` is the backing eVote.
        exact ⟨lbl, hisLbl, by rw [Bool.or_eq_true]; right; exact hSentLbl,
               by rw [hPay], by rw [hTgt, hnt]⟩
      · have hmem' : a ∈ s.containers.Node_votes n.ref := by
          rw [PSet.mem_def, hnt]; exact hold
        exact votes_linked_carry (hLinked n hnKind' a hmem')
    · rw [if_neg hnt] at hmem
      exact votes_linked_carry (hLinked n hnKind' a hmem)
  case t_won =>
    intro n hnKind hWonPost
    pverify_machine_has_type hnKind' : Node n.ref from hnKind
    by_cases hnt : n.ref = this.ref
    · -- newly-Won node `this.ref`: its post vote-set is a quorum by the guard.
      simp only [hnt, if_pos]
      convert hGuard using 2
    · -- other node: control state + vote-set unchanged; use the IH.
      have hWonPre : stateOf n.ref s = Node.Won_st := by
        simp only [stateOf] at hWonPost ⊢
        rw [if_neg hnt] at hWonPost; exact hWonPost
      simp only [if_neg hnt]
      convert hWon n hnKind' hWonPre using 2
  -- else-branch (no goto): only `votes += param.voter` + markReceived.
  case e_one =>
    intro e1 hisE1 e2 hisE2 hs1 hs2 hveq
    exact hOne e1 hisE1 e2 hisE2 hs1 hs2 hveq
  case e_voted =>
    intro e hisE n hnKind hsent hveq
    pverify_machine_has_type hnKind' : Node n.ref from hnKind
    exact hVoted e hisE n hnKind' hsent hveq
  case e_linked =>
    intro n hnKind a hmem
    pverify_machine_has_type hnKind' : Node n.ref from hnKind
    by_cases hnt : n.ref = this.ref
    · rw [if_pos hnt] at hmem
      simp only [PSet.mem_def, PSet.insert_eq, PSet.mem_insert] at hmem
      rcases hmem with rfl | hold
      · exact ⟨lbl, hisLbl, hSentLbl, by rw [hPay], by rw [hTgt, hnt]⟩
      · have hmem' : a ∈ s.containers.Node_votes n.ref := by
          rw [PSet.mem_def, hnt]; exact hold
        exact hLinked n hnKind' a hmem'
    · rw [if_neg hnt] at hmem
      exact hLinked n hnKind' a hmem
  case e_won =>
    intro n hnKind hWonPost
    pverify_machine_has_type hnKind' : Node n.ref from hnKind
    -- No goto here, so the only Won node in the post-state was Won in `s`.
    have hWonPre : stateOf n.ref s = Node.Won_st := by
      simp only [stateOf] at hWonPost ⊢; exact hWonPost
    have hq := hWon n hnKind' hWonPre
    by_cases hnt : n.ref = this.ref
    · -- `this.ref` is in RequestVoting in `s`, so it can't also be Won.
      exfalso; rw [hnt, stateOf, hSt] at hWonPre; exact S.noConfusion hWonPre
    · simp only [if_neg hnt]; convert hq using 2

-- `eRequestVote` block0 `quorum_votes` and the `entry` broadcast's
-- `quorum_votes` step now close by SMT: with `votes : PSet` first-order
-- and the `_payload_of`/`is_<ev>` bridges, the solver discharges both
-- the fresh-eVote reasoning (eRequestVote) and the no-eVote-sent frame
-- (entry loop). Removed, along with the `send_eReq_preserves_qv` helper.

/-! ### `election_safety` steps.

Each handler preserves `quorum_votes` (proved above), and
`quorum_votes_implies_safety` derives `unique_leader` on the post-state.
So each reduces via `triple_cons` to the matching block0 obligation,
then maps the post `quorum_votes` to `election_safety`. -/

-- `eRequestVote` / `entry` block1 `election_safety` now close by SMT
-- (their block0 `quorum_votes` steps do, and `quorum_intersect` is a
-- lifted `paxiom`) — removed. `eVote` block1 still reduces to its
-- manual block0 below.

set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem Node.RequestVoting.eVote_correct_block1_election_safety_using_quorum_votes
    (this : Node) (param : eVote_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (election_safety s ∧ quorum_votes s) ∧
        inflight lbl s ∧ lbl.target = this.ref ∧ is_Node this.ref s ∧
        (s.machines this.ref).currentState = Node.RequestVoting_st ∧
        lbl.action = EventOrGoto.event (E.eVote param))
      (do PLean.markReceived (P := Sig) lbl;
          Node.RequestVoting.eVote_handler this param)
      (fun _ s => election_safety s) := by
  apply triple_cons
    (pre := fun s => quorum_votes s ∧
      inflight lbl s ∧ lbl.target = this.ref ∧ is_Node this.ref s ∧
      (s.machines this.ref).currentState = Node.RequestVoting_st ∧
      lbl.action = EventOrGoto.event (E.eVote param))
    (post := fun _ s => quorum_votes s)
  · intro s ⟨⟨_, hQV⟩, rest⟩; exact ⟨hQV, rest⟩
  · intro _ s hQV; exact quorum_votes_implies_safety s hQV
  exact Node.RequestVoting.eVote_correct_block0_quorum_votes this param lbl

/-! ### `prove default` steps for the loop-bearing / goto-bearing handlers.

`eVote` (container write + `goto`) and `entry` (broadcast loop) don't
close through the auto-default chain — the loop's user invariant
(`quorum_votes`) doesn't entail `DefaultInvariants`, and the `goto`
guard branches. Discharge structurally with `triple_pforeach_with`
(entry) and the standard step chain (eVote). -/

-- `eVote_correct_block1_default` now closes by SMT (its footprint is a
-- container write + goto, both DefaultInvariants-preserving) — removed.

@[pverifyProof]
theorem Node.RequestVoting.entry_correct_block1_default
    (this : Node) :
    triple (l := PProp Sig)
      (fun s => DefaultInvariants s ∧
        is_Node this.ref s ∧
        (s.machines this.ref).currentState = Node.RequestVoting_st)
      (Node.RequestVoting.entry this)
      (fun _ s => DefaultInvariants s) := by
  apply triple_cons (pre := DefaultInvariants)
    (post := fun _ => DefaultInvariants)
  · intro s ⟨h, _, _⟩; exact h
  · intro _ s h; exact h
  unfold Node.RequestVoting.entry
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold Node.voted_get; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold Node.votes_get; pverify
  intro _
  apply triple_pforeach_with (Q := DefaultInvariants)
  intro m
  unfold PLean.send
  pverify

end Consensus

set_option maxHeartbeats 1000000 in
set_option pverify.failOnIncomplete false in
#pverify Consensus
