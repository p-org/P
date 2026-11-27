/-
PLean port of P's Tutorial/Advanced/5_Consensus.

A toy consensus protocol. `RequestVoting`'s entry broadcasts
`eRequestVote` to every configured node; each Node votes at most once
(guarded by `voted`); the `eVote` handler collects voters into `votes`
and moves to `Won` once `isQuorum votes` holds.

`election_safety` (at most one Node wins) follows from the
`quorum_votes` lemma and the quorum-intersection axiom: any two quorum
vote sets share a member, and no voter is counted by two Nodes.
-/
import PLean

open PLean

pmodule Consensus

  system s

  type tRequestPayload = (src : PLean.MachineRef)
  type tVotePayload    = (voter : PLean.MachineRef)

  event eRequestVote : tRequestPayload
  event eVote        : tVotePayload

  -- The configured nodes the broadcast iterates over.
  function nodes : seq[PLean.MachineRef]

  -- P's `isQuorum(s: set[machine]): bool`.
  function isQuorum : set[PLean.MachineRef] → Bool

  -- The configured nodes are machines (a send to `null` fails).
  paxiom nodes_not_null :
    ∀ m : PLean.MachineRef, m ∈ nodes → m ≠ PLean.MachineRef.null

  paxiom quorum_intersect :
    ∀ (q1 q2 : set[PLean.MachineRef]),
      isQuorum q1 = true → isQuorum q2 = true →
      ∃ a : PLean.MachineRef, a ∈ q1 ∧ a ∈ q2

  machine Node {
    var voted : Bool
    var votes : set[PLean.MachineRef]

    start state RequestVoting {
      entry {
        foreach (m in nodes)
          invariant only_requests :
            new_sends e => e is eRequestVote ∧ (eRequestVote_payload_of e).src = this.ref ;
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

  Lemma quorum_votes {
    -- Each Node votes at most once.
    invariant one_vote_per_voter :
      ∀ e1 e2 : eVote, sent e1 s → sent e2 s → e1.voter = e2.voter → e1 = e2

    invariant voted_after_eVote_sent :
      ∀ (e : eVote) (n : Node), sent e s → e.voter = n.ref → n.voted = true

    -- Every collected vote was sent to the collector.
    invariant votes_linked :
      ∀ (n : Node) (a : PLean.MachineRef),
        a ∈ n.votes → ∃ e : eVote, sent e s ∧ e.voter = a ∧ e.target = n.ref

    -- A collected voter's vote went to the collector, so no voter is counted twice.
    invariant vote_owner :
      ∀ (e : eVote) (n : Node), sent e s → e.voter ∈ n.votes → e.target = n.ref

    invariant votes_disjoint :
      ∀ (n1 n2 : Node) (a : PLean.MachineRef), a ∈ n1.votes → a ∈ n2.votes → n1 = n2

    invariant won_implies_quorum_votes :
      ∀ n : Node, stateOf n.ref s = some Node.Won_st → isQuorum n.votes = true
  }

  Theorem election_safety {
    invariant unique_leader :
      ∀ n1 n2 : Node,
        stateOf n1.ref s = some Node.Won_st →
        stateOf n2.ref s = some Node.Won_st →
        n1 = n2
  }
  -- A vote request names a Node: the vote goes back to it.
  Lemma routing {
    invariant request_from_node :
      ∀ e : eRequestVote, sent e s → is_Node e.src s
  }

  Proof {
    prove routing ;
    prove quorum_votes ;
    prove election_safety using quorum_votes ;
  }

end Consensus

#gen_module Consensus
#pwf        Consensus

#pverify Consensus
