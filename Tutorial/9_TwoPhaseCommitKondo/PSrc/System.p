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
            foreach (p in participants()) 
                invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                invariant forall new (e: event) :: e is eVoteReq;
            {
                send p, eVoteReq;
            }
            goto WaitForResponses;
        }
        ignore eVoteResp; // TODO: remove this and prove it
    }
    
    state WaitForResponses {
        on eVoteResp do (resp: tVoteResp) {
            var p: machine;
            
            if (resp.vote == YES) {
                yesVotes += (resp.source);
                
                if (yesVotes == participants()) {
                    foreach (p in participants()) 
                        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                        invariant forall new (e: event) :: e is eCommit;
                    {
                        send p, eCommit;
                    }
                    goto Committed;
                }
                
            } else {
                foreach (p in participants()) 
                    invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                    invariant forall new (e: event) :: e is eAbort;
                {
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

// using these to avoid initialization
pure participants(): set[machine];
pure coordinator(): machine;
pure preference(m: machine) : Vote;

// assumptions about how the system is setup and the pure functions above
assume on start one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
assume on start participant_set: forall (m: machine) :: m in participants() == m is Participant;

// making sure that our assumptions about pure functions are not pulled out from underneath us
invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;

// make sure we never get a response that we're not expecting
invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !flying e;
invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && e targets coordinator() ==> !flying e;
invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !flying e;
invariant never_resp_to_participant: forall (e: event, p: Participant) :: e is eVoteResp && e targets p ==> !flying e;

// the main invariant we care about
// invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2) == YES);

// supporting invariants
// invariant  a1: forall (e: eVoteResp) :: flying e ==> e.source in participants();
// invariant  a2: forall (e: eVoteResp) :: flying e ==> e.vote == preference(e.source);
invariant a3b: forall (e: eAbort)    :: flying e ==> coordinator() is Aborted;
// invariant a3a: forall (e: eCommit)   :: flying e ==> coordinator() is Committed;
// // bug in their a4? their version should only hold if the network is append only?
// invariant  a4: forall (p: Participant) :: p is Accepted ==> coordinator() is Committed;
// invariant  a5: forall (m: machine, c: Coordinator) :: m in c.yesVotes ==> preference(m) == YES;
// invariant  a6: coordinator() is Committed ==> (forall (m: machine) :: m in participants() ==> preference(m) == YES);