type tVoteResp = (source: machine);

event eVoteReq;
event eYes: tVoteResp;
event eNo: tVoteResp;
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
    }
    
    state WaitForResponses {
        on eYes do (resp: tVoteResp) {
            var p: machine;
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
        }
        on eNo do (resp: tVoteResp) {
            var p: machine;
            foreach (p in participants()) 
                invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                invariant forall new (e: event) :: e is eAbort;
            {
                send p, eAbort;
            }
            goto Aborted;
        }
    }
    
    state Committed {ignore eYes, eNo;}
    state Aborted {ignore eYes, eNo;}
}

machine Participant {
    start state Undecided {
        on eVoteReq do {
            if (preference(this)) {
                send coordinator(), eYes, (source = this,);
            } else {
                send coordinator(), eNo, (source = this,);
            }
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
pure preference(m: machine) : bool;

// assumptions about how the system is setup and the pure functions above
init-condition forall (m: machine) :: m == coordinator() == m is Coordinator;
init-condition forall (m: machine) :: m in participants() == m is Participant;

// set all the fields to their default values
init-condition forall (c: Coordinator) :: c.yesVotes == default(set[machine]);

// making sure that our assumptions about pure functions are not pulled out from underneath us
Lemma system_config {
    invariant one_coordinator: forall (m: machine) :: m == coordinator() <==> m is Coordinator;
    invariant participant_set: forall (m: machine) :: m in participants() <==> m is Participant;
    invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
    invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && e targets coordinator() ==> !inflight e;
    invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
    invariant never_yes_to_participant: forall (e: event, p: Participant) :: e is eYes && e targets p ==> !inflight e;
    invariant never_yes_to_init: forall (e: event, c: Coordinator) :: e is eYes && e targets c && c is Init ==> !inflight e;
    invariant never_no_to_participant: forall (e: event, p: Participant) :: e is eNo && e targets p ==> !inflight e;
    invariant never_no_to_init: forall (e: event, c: Coordinator) :: e is eNo && e targets c && c is Init ==> !inflight e;
    invariant req_implies_not_init: forall (e: event, c: Coordinator) :: e is eVoteReq && c is Init ==> !inflight e;
}

// the main invariant we care about
invariant safety: forall (p1: Participant) :: p1 is Accepted ==> (forall (p2: Participant) :: preference(p2));

// supporting invariants, based on the Kondo paper
Lemma kondo {
    invariant a1a: forall (e: eYes) :: inflight e ==> e.source in participants();
    invariant a1b: forall (e: eNo)  :: inflight e ==> e.source in participants();
    invariant a2a: forall (e: eYes) :: inflight e ==> preference(e.source);
    invariant a2b: forall (e: eNo)  :: inflight e ==> !preference(e.source);
    invariant a3b: forall (e: eAbort)  :: inflight e ==> coordinator() is Aborted;
    invariant a3a: forall (e: eCommit) :: inflight e ==> coordinator() is Committed;
    invariant  a4: forall (p: Participant) :: p is Accepted ==> coordinator() is Committed;
    invariant  a5: forall (p: Participant, c: Coordinator) :: p in c.yesVotes ==> preference(p);
    invariant  a6: coordinator() is Committed ==> (forall (p: Participant) :: p in participants() ==> preference(p));
}

Proof {
    prove system_config;
    prove kondo using system_config;
    prove safety using kondo;
    prove default using system_config;
}