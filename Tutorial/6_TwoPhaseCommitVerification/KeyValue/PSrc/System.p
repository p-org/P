enum tVote {YES, NO}

type tRound = int;
type tKey   = int;
type tValue = int;

type tVoteResp  = (key: tKey, value: tValue, origin: machine, round: tRound, vote: tVote, source: machine);
type tVoteReq   = (key: tKey, value: tValue, origin: machine, round: tRound);
type tWriteReq  = (key: tKey, value: tValue, origin: machine);
type tWriteResp = (key: tKey, value: tValue, vote: tVote);
type tReadReq   = (key: tKey, origin: machine);
type tReadResp  = (key: tKey, value: tValue);

event eVoteReq : tVoteReq;
event eVoteResp: tVoteResp;
event eAbort   : tVoteReq;
event eCommit  : tVoteReq;

event eWriteReq : tWriteReq;
event eWriteResp: tWriteResp;
event eReadReq  : tReadReq;
event eReadResp : tReadResp;

fun RandomInt(): int;

machine Client {
    start state Loop {
        entry {
            var k: int;
            var v: int; 
            
            k = RandomInt();
            
            if ($) {
                v = RandomInt();
                send coordinator(), eWriteReq, (key = k, value = v, origin = this);
            } else {
                send coordinator(), eReadReq, (key = k, origin = this);
            }
            
            goto Loop;
        }
        ignore eWriteResp, eReadResp;
    }
}

machine Coordinator
{
    var yesVotes: map[tRound, set[machine]];
    var commited: set[tRound];
    var aborted: set[tRound];
    var kv: map[tKey, tValue];
    
    start state Serving {
        on eReadReq do (req: tReadReq) {
            send req.origin, eReadResp, (key = req.key, value = kv[req.key]);
        }
    
        on eWriteReq do (req: tWriteReq) {
            var p: machine;
            var r: tRound;
            assume !(r in yesVotes);
            
            yesVotes[r] = default(set[machine]);
            
            foreach (p in participants()) 
                invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                invariant forall new (e: event) :: e is eVoteReq;
                invariant forall new (e: eVoteReq) :: e.round == r && e.key == req.key && e.value == req.value && e.origin == req.origin;
            {
                send p, eVoteReq, (key = req.key, value = req.value, origin = req.origin, round = r);
            }
        }
        
        on eVoteResp do (resp: tVoteResp) {
            var p: machine;
            
            if (resp.vote == YES) {
                yesVotes[resp.round] += (resp.source);
                
                if (yesVotes[resp.round] == participants()) {
                    foreach (p in participants()) 
                        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                        invariant forall new (e: event) :: e is eCommit;
                        invariant forall new (e: eCommit) :: e.round == resp.round && e.key == resp.key && e.value == resp.value && e.origin == resp.origin;
                    {
                        send p, eCommit, (key = resp.key, value = resp.value, origin = resp.origin, round = resp.round);
                    }
                    
                    commited += (resp.round);
                    kv[resp.key] = resp.value;
                    send resp.origin, eWriteResp, (key = resp.key, value = kv[resp.key], vote = YES);
                    goto Serving;
                }
                
            } else {
                foreach (p in participants()) 
                    invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                    invariant forall new (e: event) :: e is eAbort;
                    invariant forall new (e: eAbort) :: e.round == resp.round && e.key == resp.key && e.value == resp.value && e.origin == resp.origin;
                {
                    send p, eAbort, (key = resp.key, value = resp.value, origin = resp.origin, round = resp.round);
                }
                
                aborted += (resp.round);
                send resp.origin, eWriteResp, (key = resp.key, value = kv[resp.key], vote = NO);
                goto Serving;
            }
        }
    }
}

machine Participant {
    var commited: set[tRound];
    var aborted: set[tRound];
    var kv: map[tKey, tValue];

    start state Acting {
        on eVoteReq do (req: tVoteReq) {
            send coordinator(), eVoteResp, (key = req.key, value = req.value, origin = req.origin, round = req.round, vote = preference(this, req.round), source = this);
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
pure preference(m: machine, r: tRound) : tVote;

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
invariant never_writeReq_to_participant: forall (e: event, p: Participant) :: e is eWriteReq && e targets p ==> !inflight e;
invariant never_readReq_to_participant: forall (e: event, p: Participant) :: e is eReadReq && e targets p ==> !inflight e;
invariant never_writeResp_to_participant: forall (e: event, p: Participant) :: e is eWriteResp && e targets p ==> !inflight e;
invariant never_readResp_to_participant: forall (e: event, p: Participant) :: e is eReadResp && e targets p ==> !inflight e;
invariant never_writeResp_to_coordinator: forall (e: event, c: Coordinator) :: e is eWriteResp && e targets c ==> !inflight e;
invariant never_readResp_to_coordinator: forall (e: event, c: Coordinator) :: e is eReadResp && e targets c ==> !inflight e;
invariant never_voteReq_to_client: forall (e: event, c: Client) :: e is eVoteReq && e targets c ==> !inflight e;
invariant never_voteResp_to_client: forall (e: event, c: Client) :: e is eVoteResp && e targets c ==> !inflight e;
invariant never_abort_to_client: forall (e: event, c: Client) :: e is eAbort && e targets c ==> !inflight e;
invariant never_commit_to_client: forall (e: event, c: Client) :: e is eCommit && e targets c ==> !inflight e;
invariant never_writeReq_to_client: forall (e: event, c: Client) :: e is eWriteReq && e targets c ==> !inflight e;
invariant never_readReq_to_client: forall (e: event, c: Client) :: e is eReadReq && e targets c ==> !inflight e;
invariant readReq_origin_is_client: forall (e: eReadReq) :: inflight e ==> e.origin is Client;
invariant writeReq_origin_is_client: forall (e: eWriteReq) :: inflight e ==> e.origin is Client;
invariant voteReq_origin_is_client: forall (e: eVoteReq) :: inflight e ==> e.origin is Client;
invariant voteResp_origin_is_client: forall (e: eVoteResp) :: inflight e ==> e.origin is Client;

// the main invariant we care about 
invariant safety: forall (c: Coordinator, p1: Participant, r: tRound) :: (r in p1.commited ==> (forall (p2: Participant) :: preference(p2, r) == YES));

// supporting invariants from non-round proof
invariant  a1: forall (e: eVoteResp) :: sent e ==> e.source is Participant;
invariant  a2: forall (e: eVoteResp) :: sent e ==> e.vote == preference(e.source, e.round);
invariant a3a: forall (c: Coordinator, e: eCommit) :: sent e ==> e.round in c.commited;
invariant a3b: forall (c: Coordinator, e: eAbort)  :: sent e ==> e.round in c.aborted;
invariant  a4: forall (c: Coordinator, r: tRound, p1: Participant) :: r in p1.commited ==> r in c.commited;
invariant  a5: forall (p: Participant, c: Coordinator, r: tRound) :: (r in c.yesVotes && p in c.yesVotes[r]) ==> preference(p, r) == YES;
invariant  a6: forall (c: Coordinator, r: tRound) :: r in c.commited ==> (forall (p2: Participant) :: preference(p2, r) == YES);

// make sure that votes have been initialized
invariant a7a: forall (c: Coordinator, e: eVoteReq)  :: sent e ==> e.round in c.yesVotes;
invariant a7b: forall (c: Coordinator, e: eVoteResp) :: sent e ==> e.round in c.yesVotes;


