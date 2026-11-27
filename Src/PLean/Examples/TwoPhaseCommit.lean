/-
PLean port of the `Single` variant of
`Tutorial/Advanced/2_TwoPhaseCommitVerification` (parent repo).

One Coordinator broadcasts `eVoteReq` to the participants, collects
`eYes` / `eNo` replies, and broadcasts `eCommit` once every participant
voted yes (`eAbort` on the first no). Safety: a participant that
accepted saw a commit, so every participant preferred yes.
-/
import PLean

open PLean

pmodule TwoPhaseCommit

  system s

  type tVoteResp = (source : PLean.MachineRef)

  event eVoteReq
  event eYes     : tVoteResp
  event eNo      : tVoteResp
  event eAbort
  event eCommit

  function coordinator  : PLean.MachineRef
  function participants : set[PLean.MachineRef]
  function preference   : PLean.MachineRef → Bool

  machine Coordinator {
    var yesVotes : set[PLean.MachineRef]

    start state Init {
      entry {
        foreach (p in participants)
          invariant vote_reqs : new_sends e => e is eVoteReq ∧ e.target ∈ participants ;
        {
          send p, eVoteReq
        }
        goto WaitForResponses
      }
    }

    state WaitForResponses {
      on eYes (resp : tVoteResp) {
        yesVotes += (resp.source)
        if (∀ p, p ∈ participants → p ∈ yesVotes) then do
          foreach (p in participants)
            invariant commits : new_sends e => e is eCommit ∧ e.target ∈ participants ;
          {
            send p, eCommit
          }
          goto Committed
      }

      on eNo (resp : tVoteResp) {
        foreach (p in participants)
          invariant aborts : new_sends e => e is eAbort ∧ e.target ∈ participants ;
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

  -- One Coordinator (named `coordinator`); the participants are exactly the
  -- Participant machines.
  init-holds is_Coordinator coordinator s
  init-holds ∀ c : Coordinator, c.ref = coordinator
  init-holds ∀ (c : Coordinator) (m : PLean.MachineRef), ¬ (m ∈ c.yesVotes)
  init-holds ∀ p : PLean.MachineRef, p ∈ participants ↔ is_Participant p s

  -- P's `system_config`.
  Lemma system_config {
    invariant coordinator_kind : is_Coordinator coordinator s
    invariant participant_set :
      ∀ p : PLean.MachineRef, p ∈ participants ↔ is_Participant p s
  }

  -- Where each kind of message goes.
  Lemma routing {
    invariant vote_req_to_participant :
      ∀ e : eVoteReq, inflight e s → e.target ∈ participants
    invariant commit_to_participant :
      ∀ e : eCommit, inflight e s → e.target ∈ participants
    invariant abort_to_participant :
      ∀ e : eAbort, inflight e s → e.target ∈ participants
    invariant yes_to_coordinator :
      ∀ e : eYes, inflight e s → e.target = coordinator
    invariant no_to_coordinator :
      ∀ e : eNo, inflight e s → e.target = coordinator
  }

  Lemma votes {
    -- Only yes-preferring participants send `eYes`.
    invariant yes_implies_pref :
      ∀ e : eYes, sent e s → preference e.source = true
    -- Every collected vote is from a yes-preferring participant.
    invariant votes_all_prefer :
      ∀ (c : Coordinator) (p : PLean.MachineRef), p ∈ c.yesVotes → preference p = true
  }

  -- A commit is only sent once every participant preferred yes.
  Lemma commit_sent {
    invariant commit_sent_implies_all_yes :
      ∀ e : eCommit, sent e s →
        ∀ p : PLean.MachineRef, is_Participant p s → preference p = true
  }

  Theorem safety {
    invariant accepted_implies_all_prefer :
      ∀ p1 : Participant,
        stateOf p1.ref s = some Participant.Accepted_st →
        ∀ p2 : PLean.MachineRef, is_Participant p2 s → preference p2 = true
  }

  Proof {
    prove system_config ;
    prove routing ;
    prove votes ;
    prove commit_sent using votes, system_config ;
    prove safety using commit_sent ;
  }

end TwoPhaseCommit

#gen_module TwoPhaseCommit
#pwf        TwoPhaseCommit

#pverify TwoPhaseCommit
