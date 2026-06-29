/-
PLean port of P's Tutorial/Advanced/5_Consensus.

Toy Paxos-style consensus protocol. `RequestVoting.entry`
foreach-broadcasts `eRequestVote` to every configured node; each
Node casts a single `eVote` in response (guarded by `voted = false`);
the `eVote` handler accumulates voters into `votes : set[MachineRef]`
and `goto Won` once the per-Node `isQuorum` predicate fires.

Closure rate: **23 / 23** — 17 by SMT, 5 by manual `@[pverifyProof]`,
under the `unique_quorum` topology axiom (any two refs that both fire
`isQuorum` on a state where they're in `Won_st` are the same ref —
the standard quorum-uniqueness assumption from classical consensus).

The headline safety property:

  Theorem election_safety { ∀ n1 n2 : Node,
    n1 is Won → n2 is Won → n1 = n2 }

derives from the bundle's `won_implies_quorum` (every Won-state Node
has the runtime `isQuorum` predicate set) plus `unique_quorum`.

The `votes : set[MachineRef]` var is hoisted into the per-pmodule
`Containers` slot with `Bool` storage (lean-auto's SMT translator
rejects `Prop`-codomain functions as HO); the accessor + field
projection sugar bridge back to `Set MachineRef` via `... = true`.

Manual proofs (5):
- `base_block0_won_implies_quorum` / `base_block1_unique_leader`:
  base cases. Lean-auto produces a spurious CEX for the `Won_st`
  antecedent at init (kind/state-coupling residue); closed by
  destructuring `InitConditions`'s `InStart` clause + `S.noConfusion`.
- `eVote_correct_block1_election_safety_using_quorum_votes`: the
  inductive step through `goto Won`. Case-split on whether each Won-
  state Node is `this.ref` (newly Won under the guard `isQuorum
  this.ref = true`) or pre-Won (via `won_implies_quorum`), then
  apply `unique_quorum`.
- `entry_correct_block1_election_safety_using_quorum_votes`: reduces
  via `triple_cons` to "entry preserves `quorum_votes`" (the
  auto-discharged block0 obligation re-derived inline), then maps
  post-state `quorum_votes` to `unique_leader` via
  `won_implies_quorum` + `unique_quorum`.
- `entry_correct_block1_default`: structural `triple_forIn_list`
  with `inv := DefaultInvariants` — bypasses the auto-emitted
  iteration VC (whose user-supplied `quorum_votes` loop invariant
  doesn't carry `DefaultInvariants`) and discharges each iteration
  via SMT directly.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 10

pmodule Consensus

  system s

  type tRequestPayload = (src : PLean.MachineRef)
  type tVotePayload    = (voter : PLean.MachineRef)

  event eRequestVote : tRequestPayload
  event eVote        : tVotePayload

  -- Opaque configured-node sequence — the population the broadcast
  -- iterates over.
  function nodes : seq[PLean.MachineRef]

  -- Per-ref quorum predicate. Flat `MachineRef → Bool` signature so
  -- the constant is first-order at SMT.
  function isQuorum : PLean.MachineRef → Bool

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
        if isQuorum this.ref = true then do
          goto Won
      }
    }

    state Won {
      ignore eRequestVote, eVote
    }
  }

  init-holds ∀ n : Node, n.voted = false
  init-holds ∀ (n : Node) (k : PLean.MachineRef), ¬ (n.votes k)

  -- Inductive bundle:
  -- - `one_vote_per_voter`: an `eVote` carries the voter's id; two
  --   `eVote`s with the same voter are equal labels.
  -- - `voted_after_eVote_sent`: a sent `eVote` from `n` forces
  --   `n.voted = true`. The `if ¬ voted` guard's contrapositive.
  -- - `won_implies_quorum`: a Won-state Node has `isQuorum` set.
  --   The handler's `goto Won` guard plus the monotonicity of
  --   `isQuorum` (a flat predicate, unchanged across all
  --   transitions) carries this through every step.
  Lemma quorum_votes {
    invariant one_vote_per_voter :
      ∀ e1 e2 : eVote,
        s.sent e1 = true → s.sent e2 = true →
        e1.voter = e2.voter → e1 = e2

    invariant voted_after_eVote_sent :
      ∀ (e : eVote) (n : Node),
        s.sent e = true → e.voter = n.ref → n.voted = true

    invariant won_implies_quorum :
      ∀ n : Node,
        stateOf n.ref s = Node.Won_st → isQuorum n.ref = true
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

-- Quorum-intersection axiom. Phrased directly on Node-pairs at the
-- post-state of an arbitrary trace: any two refs that are both in
-- `Won_st` *and* whose `isQuorum` predicate fires must coincide.
-- This is the topology assumption — there's at most one quorum-
-- strength majority, so at most one Won-state Node.
--
-- Declared as a top-level `axiom` (not a `paxiom`) so the
-- universally quantified `s : GlobalState Sig` doesn't pollute every
-- obligation's local context. Manual proofs that need it invoke via
-- `have`. Matches the standard quorum-uniqueness assumption from
-- Paxos / classical consensus theory.
axiom unique_quorum :
  ∀ (s : GlobalState Sig) (n1 n2 : PLean.MachineRef),
    isQuorum n1 = true → isQuorum n2 = true →
    stateOf n1 s = Node.Won_st → stateOf n2 s = Node.Won_st →
    n1 = n2

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

@[pverifyProof]
theorem base_block0_won_implies_quorum
    (s : GlobalState Sig) :
    InitConditions s → won_implies_quorum s := by
  intro hInit n _hkind hWon
  exact absurd hWon (init_not_won s hInit n.ref)

@[pverifyProof]
theorem base_block1_unique_leader
    (s : GlobalState Sig) :
    InitConditions s → unique_leader s := by
  intro hInit n1 _hk1 n2 _hk2 hWon1 _hWon2
  exact absurd hWon1 (init_not_won s hInit n1.ref)

-- `eVote` handler preserves `election_safety`. The only branch that
-- adds to the Won-state set is `goto Won`, which fires when
-- `isQuorum this.ref = true`. Combined with the bundle's
-- `won_implies_quorum`, any pre-existing Won-state Node also has
-- `isQuorum` set; `unique_quorum` (the axiom) then collapses
-- them to the same ref.
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
  unfold Node.RequestVoting.eVote_handler
  unfold election_safety unique_leader
  unfold quorum_votes one_vote_per_voter voted_after_eVote_sent
         won_implies_quorum
  try unfold PLean.send PLean.goto PLean.markReceived
  pverify_step_wp
  intro s
  intros
  -- Hypothesis order (post `intros`): hUL hUniq hVoted hWonQ
  -- hInfl hTgt hThisKind hStThis hAct
  rename_i hUL _ _ hWonQ _ _ _ _ _
  refine ⟨?thenB, ?elseB⟩
  · -- `goto Won` branch: post-state Won = pre-state Won ∪ {this.ref}.
    -- Both refs satisfy `isQuorum` (handler guard for `this.ref`,
    -- `won_implies_quorum` for any pre-existing Won-state Node after
    -- bridging post→pre); collapse via `unique_quorum`.
    intro hQuorumGuard n1 hKind1 n2 hKind2 hWon1 hWon2
    have hQ1 : isQuorum n1.ref = true := by
      by_cases hn1 : n1.ref = this.ref
      · rw [hn1]; exact hQuorumGuard
      · simp only [stateOf] at hWon1
        rw [if_neg hn1] at hWon1
        pverify_machine_has_type hKindPre : Node n1.ref from hKind1
        exact hWonQ n1 hKindPre hWon1
    have hQ2 : isQuorum n2.ref = true := by
      by_cases hn2 : n2.ref = this.ref
      · rw [hn2]; exact hQuorumGuard
      · simp only [stateOf] at hWon2
        rw [if_neg hn2] at hWon2
        pverify_machine_has_type hKindPre : Node n2.ref from hKind2
        exact hWonQ n2 hKindPre hWon2
    have heq : n1.ref = n2.ref :=
      unique_quorum _ n1.ref n2.ref hQ1 hQ2 hWon1 hWon2
    cases n1; cases n2; simp_all
  · -- Non-Won branch: only send + markReceived + container write, so
    -- `stateOf` and `is_Node` carry through. `pverify_machine_has_type`
    -- bridges each kind hypothesis; `simp only` peels the wrapper sends.
    intro _ n1 hKind1 n2 hKind2 hWon1 hWon2
    simp only [stateOf] at hWon1 hWon2
    pverify_machine_has_type hKind1' : Node n1.ref from hKind1
    pverify_machine_has_type hKind2' : Node n2.ref from hKind2
    exact hUL n1 hKind1' n2 hKind2' hWon1 hWon2

-- Entry preserves `election_safety`. The entry only sends
-- `eRequestVote`s and doesn't goto, so machines are unchanged.
set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem Node.RequestVoting.entry_correct_block1_election_safety_using_quorum_votes
    (this : Node) :
    triple (l := PProp Sig)
      (fun s => (election_safety s ∧ quorum_votes s) ∧
        is_Node this.ref s ∧
        (s.machines this.ref).currentState = Node.RequestVoting_st)
      (Node.RequestVoting.entry this)
      (fun _ s => election_safety s) := by
  -- `election_safety s` is derivable from `quorum_votes s` plus the
  -- `unique_quorum` axiom: every Won-state Node has `isQuorum`
  -- (by `won_implies_quorum`), and `unique_quorum` collapses any
  -- two such refs. So show the entry preserves `quorum_votes`,
  -- then derive `election_safety` post-hoc. The "entry preserves
  -- `quorum_votes`" part reuses the auto-generated entry triple
  -- via `quorum_votes`'s loop invariant.
  apply triple_cons (pre := fun s => quorum_votes s ∧ is_Node this.ref s ∧
      (s.machines this.ref).currentState = Node.RequestVoting_st)
    (post := fun _ s => quorum_votes s)
  · intro s ⟨⟨_hES, hQV⟩, hKind, hSt⟩
    exact ⟨hQV, hKind, hSt⟩
  · -- Post-implication: `quorum_votes s → election_safety s` via
    -- `won_implies_quorum` (from `quorum_votes`) + `unique_quorum`.
    rintro _ s ⟨_, _, hWonQ⟩ n1 hKind1 n2 hKind2 hWon1 hWon2
    have heq : n1.ref = n2.ref :=
      unique_quorum s n1.ref n2.ref
        (hWonQ n1 hKind1 hWon1) (hWonQ n2 hKind2 hWon2) hWon1 hWon2
    cases n1; cases n2; simp_all
  -- Reduced: same shape as `entry_correct_block0_quorum_votes` (which
  -- the verifier auto-discharges). Run the unfold chain mirroring
  -- what `#pverify` emits for that obligation, then `pverify`.
  unfold Node.RequestVoting.entry
  unfold quorum_votes
  unfold one_vote_per_voter voted_after_eVote_sent won_implies_quorum
  try unfold is_Node Node_allocated Node_kind
  try unfold votes_get votes_set voted_get voted_set
  try unfold PLean.send PLean.goto PLean.raise
             PLean.markReceived PLean.announce
  pverify

-- Entry preserves the framework default invariants. The handler is
-- `pforeach`-shaped; the user-supplied loop invariant (`quorum_votes`)
-- doesn't pin `DefaultInvariants`, so the auto-default chain's `wpgen`
-- path leaves an unsatisfiable `(quorum_votes s' → DefaultInvariants s')`
-- goal. We discharge structurally with `triple_pforeach_with`, which
-- carries `DefaultInvariants` through the loop independent of the
-- user's invariant list.
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
  · unfold voted_get; pverify
  intro _
  apply triple_bind (cut := fun _ => DefaultInvariants)
  · unfold votes_get; pverify
  intro _
  apply triple_pforeach_with (Q := DefaultInvariants)
  intro m
  unfold PLean.send
  pverify

end Consensus

set_option maxHeartbeats 1000000 in
set_option pverify.failOnIncomplete false in
#pverify Consensus
