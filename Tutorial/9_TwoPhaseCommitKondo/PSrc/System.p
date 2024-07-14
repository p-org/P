enum Vote {YES, NO}
type tVoteResp = (source: machine, vote: Vote);

event eVoteReq;
event eVoteResp: tVoteResp;
event eAbort;
event eCommit;

machine Coordinator
{
    var yesVotes: set[machine];
    
    start state Init {
        entry {
            var p: machine;
            foreach (p in participants()) {
                send p, eVoteReq;
            }
            goto WaitForResponses;
        }
        ignore eVoteResp;
    }
    
    state WaitForResponses {
        on eVoteResp do (resp: tVoteResp) {
            var p: machine;
            
            if (resp.vote == YES) {
                yesVotes += (resp.source);
                
                if (yesVotes == participants()) {
                    foreach (p in participants()) {
                        send p, eCommit;
                    }
                    goto Committed;
                }
                
            } else {
                foreach (p in participants()) {
                    send p, eAbort;
                }
                goto Aborted;
            }
        }
    }
    
    state Committed {ignore eVoteResp;}
    state Aborted {ignore eVoteResp;}
}

machine Participant {
    start state Undecided {
        on eVoteReq do {
            send coordinator(), eVoteResp, (source = this, vote = preference(this));
        }
        
        on eCommit do {
            goto Accepted;
        }
        
        on eAbort do {
            goto Rejected;
        }
    }
    
    state Accepted {ignore eVoteReq, eCommit, eAbort;}
    state Rejected {ignore eVoteReq, eCommit, eAbort;}
}

// imports
pure target(e: event): machine;
pure inflight(e: event): bool;

// Using these to avoid initialization
pure participants(): set[machine];
pure coordinator(): machine;
pure preference(m: machine) : Vote;

assume on start one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
assume on start participant_set: forall (m: machine) :: m in participants() == m is Participant;

invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;

// make sure we never get a response that we're not expecting
invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && target(e) == coordinator() ==> !inflight(e);
invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && target(e) == coordinator() ==> !inflight(e);
invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && target(e) == coordinator() ==> !inflight(e);
invariant never_resp_to_participant: forall (e: event, p: Participant) :: e is eVoteResp && target(e) == p ==> !inflight(e);

invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2) == YES);