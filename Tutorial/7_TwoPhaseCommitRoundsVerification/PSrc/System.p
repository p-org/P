enum Vote {YES, NO}
type Round = int;
type tVoteResp = (source: machine, round: Round, vote: Vote);
type tVoteReq  = (round: Round);

event eVoteReq: tVoteReq;
event eVoteResp: tVoteResp;
event eAbort: tVoteReq;
event eCommit: tVoteReq;

machine Coordinator
{
    var yesVotes: map[Round, set[machine]];
    var commited: set[Round];
    var aborted: set[Round];
    
    start state Init {
        entry {
            var p: machine;
            var r: Round;
            assume !(r in yesVotes);
            
            yesVotes[r] = default(set[machine]);
            
            foreach (p in participants()) 
                invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                invariant forall new (e: event) :: e is eVoteReq;
                invariant forall new (e: eVoteReq) :: e.round == r;
            {
                send p, eVoteReq, (round = r,);
            }
            
            goto WaitForResponses;
        }
        
        defer eVoteResp;
    }
    
    state WaitForResponses {
        on eVoteResp do (resp: tVoteResp) {
            var p: machine;
            
            if (resp.vote == YES) {
                yesVotes[resp.round] += (resp.source);
                
                if (yesVotes[resp.round] == participants()) {
                    foreach (p in participants()) 
                        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                        invariant forall new (e: event) :: e is eCommit;
                        invariant forall new (e: eCommit) :: e.round == resp.round;
                    {
                        send p, eCommit, (round = resp.round,);
                    }
                    
                    commited += (resp.round);
                    goto Init;
                }
                
            } else {
                foreach (p in participants()) 
                    invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                    invariant forall new (e: event) :: e is eAbort;
                    invariant forall new (e: eAbort) :: e.round == resp.round;
                {
                    send p, eAbort, (round = resp.round,);
                }
                
                aborted += (resp.round);
                goto Init;
            }
        }
    }
}

machine Participant {
    var commited: set[Round];
    var aborted: set[Round];

    start state Undecided {
        on eVoteReq do (req: tVoteReq) {
            send coordinator(), eVoteResp, (source = this, round = req.round, vote = preference(this, req.round));
        }
        
        on eCommit do (req: tVoteReq) {
            commited += (req.round);
        }
        
        on eAbort do (req: tVoteReq) {
            aborted += (req.round);
        }
    }
}

// using these to avoid initialization
pure participants(): set[machine];
pure coordinator(): machine;
pure preference(m: machine, r: Round) : Vote;

// assumptions about how the system is setup and the pure functions above
init forall (m: machine) :: m == coordinator() == m is Coordinator;
init forall (m: machine) :: m in participants() == m is Participant;

// making sure that our assumptions about pure functions are not pulled out from underneath us
invariant one_coordinator: forall (m: machine) :: m == coordinator() == m is Coordinator;
invariant participant_set: forall (m: machine) :: m in participants() == m is Participant;

// make sure we never get a message that we're not expecting
invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && e targets coordinator() ==> !inflight e;
invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
invariant never_resp_to_participant: forall (e: event, p: Participant) :: e is eVoteResp && e targets p ==> !inflight e;

// the main invariant we care about 
invariant safety: forall (c: Coordinator, p1: Participant, r: Round) :: (r in p1.commited ==> (forall (p2: Participant) :: preference(p2, r) == YES));

// supporting invariants from non-round proof
invariant  a1: forall (e: eVoteResp) :: sent e ==> e.source is Participant;
invariant  a2: forall (e: eVoteResp) :: sent e ==> e.vote == preference(e.source, e.round);
invariant a3a: forall (c: Coordinator, e: eCommit) :: sent e ==> e.round in c.commited;
invariant a3b: forall (c: Coordinator, e: eAbort)  :: sent e ==> e.round in c.aborted;
invariant  a4: forall (c: Coordinator, r: Round, p1: Participant) :: r in p1.commited ==> r in c.commited;
invariant  a5: forall (p: Participant, c: Coordinator, r: Round) :: (r in c.yesVotes && p in c.yesVotes[r]) ==> preference(p, r) == YES;
invariant  a6: forall (c: Coordinator, r: Round) :: r in c.commited ==> (forall (p2: Participant) :: preference(p2, r) == YES);

// make sure that votes have been initialized
invariant a7a: forall (c: Coordinator, e: eVoteReq)  :: sent e ==> e.round in c.yesVotes;
invariant a7b: forall (c: Coordinator, e: eVoteResp) :: sent e ==> e.round in c.yesVotes;


