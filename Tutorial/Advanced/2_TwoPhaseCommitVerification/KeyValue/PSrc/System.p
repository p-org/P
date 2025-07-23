/*********************************************************
Clients send read and write requests to a coordinator. On write requests, the coordinator does a two phase commit with 
all participants. On read requests, the coordinator just asks a random participant to respond to the client.

All participants must agree on every commited write. The monitor keeps track of all write responses, which is the ground 
truth for what is commited. Every participant must be maintaining a subset of this. 
**********************************************************/

enum tVote {YES, NO}

type tRound = int;
type tKey   = int;
type tValue = int;

type rvPair    = (round: tRound, value: tValue);
type krvTriple = (key: tKey, round: tRound, value: tValue);

type tVoteResp  = (origin: machine, triple: krvTriple, vote: tVote, source: machine);
type tVoteReq   = (origin: machine, triple: krvTriple);
type tWriteReq  = (origin: machine, key: tKey, value: tValue);
type tReadReq   = (origin: machine, key: tKey);
type tWriteResp = (triple: krvTriple, vote: tVote);
type tReadResp  = (key: tKey, versions: set[rvPair], source: machine);

event eVoteReq : tVoteReq;
event eVoteResp: tVoteResp;
event eAbort   : tVoteReq;
event eCommit  : tVoteReq;

event eWriteReq : tWriteReq;
event eWriteResp: tWriteResp;
event eReadReq  : tReadReq;
event eReadResp : tReadResp;

fun RandomInt(): int;
fun RandomParticipant(s: set[machine]) 
    return (x: machine);
    ensures x in s;

machine Client {
    start state Loop {
        entry {
            var k: int;
            var v: int; 
            
            k = RandomInt();
            v = RandomInt();
            
            if ($) {
                v = RandomInt();
                send coordinator(), eWriteReq, (origin = this, key = k, value = v);
            } else {
                send coordinator(), eReadReq, (origin = this, key = k);
            }
            
            goto Loop;
        }
        ignore eWriteResp, eReadResp;
    }
}

machine Coordinator
{
    var yesVotes: map[krvTriple, set[machine]];
    var commited: set[krvTriple];
    var aborted : set[krvTriple];
    
    start state Serving {
        on eReadReq do (req: tReadReq) {
            var p: machine;
            p = RandomParticipant(participants());
            send p, eReadReq, req;
        }
        
        on eWriteReq do (req: tWriteReq) {
            var p: machine;
            var r: tRound;
            assume (forall (t: krvTriple) :: t in yesVotes ==> t.round != r);
            
            yesVotes[(key = req.key, round = r, value = req.value)] = default(set[machine]);
            
            foreach (p in participants())
                invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                invariant forall new (e: event) :: e is eVoteReq;
                invariant forall new (e: eVoteReq) :: e.triple.round == r && e.triple.key == req.key && e.triple.value == req.value && e.origin == req.origin;
            {
                send p, eVoteReq, (origin = req.origin, triple = (key = req.key, round = r, value = req.value));
            }
        }
        
        on eVoteResp do (resp: tVoteResp) {
            var p: machine;
            
            if (resp.vote == YES) {
                yesVotes[resp.triple] += (resp.source);
                
                if (yesVotes[resp.triple] == participants()) {
                    foreach (p in participants()) 
                        invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                        invariant forall new (e: event) :: e is eCommit;
                        invariant forall new (e: eCommit) :: e.triple == resp.triple && e.origin == resp.origin;
                    {
                        send p, eCommit, (origin = resp.origin, triple = resp.triple);
                    }
                    
                    commited += (resp.triple);
                    send resp.origin, eWriteResp, (triple = resp.triple, vote = resp.vote);
                    goto Serving;
                }
                
            } else {
                foreach (p in participants()) 
                    invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
                    invariant forall new (e: event) :: e is eAbort;
                    invariant forall new (e: eAbort) :: e.triple == resp.triple && e.origin == resp.origin;
                {
                    send p, eAbort, (origin = resp.origin, triple = resp.triple);
                }
                
                aborted += (resp.triple);
                send resp.origin, eWriteResp, (triple = resp.triple, vote = resp.vote);
                goto Serving;
            }
        }
    }
}

machine Participant {
    var commited: set[krvTriple];
    var aborted: set[krvTriple];
    var kv: map[tKey, set[rvPair]];

    start state Acting {
        on eVoteReq do (req: tVoteReq) {
            send coordinator(), eVoteResp, (origin = req.origin, triple = req.triple, vote = preference(this, req.triple), source = this);
        }
        
        on eCommit do (req: tVoteReq) {
            commited += (req.triple);
            if (!(req.triple.key in kv)) {
                kv[req.triple.key] = default(set[rvPair]);
            }
            kv[req.triple.key] += ((round = req.triple.round, value = req.triple.value));
        }
        
        on eAbort do (req: tVoteReq) {
            aborted += (req.triple);
        }
        
        on eReadReq do (req: tReadReq) {
            if (req.key in kv) {
                send req.origin, eReadResp, (key = req.key, versions = kv[req.key], source = this);
            } else {
                send req.origin, eReadResp, (key = req.key, versions = default(set[rvPair]), source = this);
            }
        }
    }
}

spec Consistency observes eReadResp, eWriteResp {
    var kv: map[tKey, set[rvPair]];
    start state WaitForEvents {
        on eWriteResp do (resp: tWriteResp) {
            if (resp.vote == YES) {
                kv[resp.triple.key] += ((round = resp.triple.round, value = resp.triple.value));
            }
        }
        on eReadResp do (resp: tReadResp) {
            // Everything we send back was actually written.
            assert subset(resp.versions, kv[resp.key]);
            // Everyone agrees on what we send back
            assert (forall (t: krvTriple) :: (resp.key == t.key && (round = t.round, value = t.value) in resp.versions) ==> (forall (p: Participant) :: preference(p, t) == YES));
        }
    }
}

// using these to avoid initialization
pure participants(): set[machine];
pure coordinator(): machine;
pure preference(m: machine, triple: krvTriple) : tVote;

pure subset(small: set[rvPair], large: set[rvPair]) : bool = forall (rv: rvPair) :: rv in small ==> rv in large;

// lets assume that there is at least one participant
axiom exists (x: machine) :: x in participants();

// assumptions about how the system is setup and the pure functions above
init-condition coordinator() is Coordinator;
init-condition forall (c1: machine, c2: machine) :: (c1 is Coordinator && c2 is Coordinator) ==> c1 == c2;
init-condition forall (m: machine) :: m in participants() == m is Participant;

// making sure that our assumptions about pure functions are not pulled out from underneath us
invariant c_is_coordinator: coordinator() is Coordinator;
invariant one_coordinator:  forall (c1: machine, c2: machine) :: (c1 is Coordinator && c2 is Coordinator) ==> c1 == c2;
invariant participant_set:  forall (m: machine) :: m in participants() == m is Participant;

// set all the fields to their default values
init-condition forall (c: Coordinator) :: c.yesVotes == default(map[krvTriple, set[machine]]);
init-condition forall (c: Coordinator) :: c.commited == default(set[krvTriple]);
init-condition forall (c: Coordinator) :: c.aborted == default(set[krvTriple]);
init-condition forall (p: Participant) :: p.commited == default(set[krvTriple]);
init-condition forall (p: Participant) :: p.aborted == default(set[krvTriple]);
init-condition forall (p: Participant) :: p.kv == default(map[tKey, set[rvPair]]);
init-condition Consistency.kv == default(map[tKey, set[rvPair]]);

// make sure we never get a message that we're not expecting
invariant never_commit_to_coordinator: forall (e: event) :: e is eCommit && e targets coordinator() ==> !inflight e;
invariant never_abort_to_coordinator: forall (e: event) :: e is eAbort && e targets coordinator() ==> !inflight e;
invariant never_req_to_coordinator: forall (e: event) :: e is eVoteReq && e targets coordinator() ==> !inflight e;
invariant never_resp_to_participant: forall (e: event, p: Participant) :: e is eVoteResp && e targets p ==> !inflight e;
invariant never_writeReq_to_participant: forall (e: event, p: Participant) :: e is eWriteReq && e targets p ==> !inflight e;
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

// aux invariants for consistency monitor first assertion
invariant p_subset_monitor_kv: forall (p: Participant, k: tKey) :: k in p.kv ==> subset(p.kv[k], Consistency.kv[k]);
invariant c_commited_means_in_monitor_kv: forall (c: Coordinator, t: krvTriple) :: t in c.commited ==> ((round = t.round, value = t.value) in Consistency.kv[t.key]);

// aux invariants for consistency monitor second assertion
// essentially the 2pc property, but restated for kv elements
invariant in_kv_means_all_preferred: forall (p1: Participant, t: krvTriple) :: (t.key in p1.kv && (round = t.round, value = t.value) in p1.kv[t.key]) ==> (forall (p2: Participant) :: preference(p2, t) == YES);

// supporting invariants for the 2pc property
invariant  a1: forall (e: eVoteResp) :: sent e ==> e.source is Participant;
invariant  a2: forall (e: eVoteResp) :: sent e ==> e.vote == preference(e.source, e.triple);
invariant a3a: forall (c: Coordinator, e: eCommit) :: sent e ==> e.triple in c.commited;
invariant a3b: forall (c: Coordinator, e: eAbort)  :: sent e ==> e.triple in c.aborted;
invariant  a4: forall (c: Coordinator, t: krvTriple, p1: Participant) :: t in p1.commited ==> t in c.commited;
invariant  a5: forall (p: Participant, c: Coordinator, t: krvTriple) :: (t in c.yesVotes && p in c.yesVotes[t]) ==> preference(p, t) == YES;
invariant  a6: forall (c: Coordinator, t: krvTriple) :: t in c.commited ==> (forall (p2: Participant) :: preference(p2, t) == YES);
invariant a7a: forall (c: Coordinator, e: eVoteReq)  :: sent e ==> e.triple in c.yesVotes;
invariant a7b: forall (c: Coordinator, e: eVoteResp) :: sent e ==> e.triple in c.yesVotes;


