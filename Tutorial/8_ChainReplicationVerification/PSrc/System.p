type tWriteRequest  = (source: machine, k: int, v: int);
type tReadRequest   = (source: machine, k: int);
type tReadResponse  = (source: machine, k: int, v: int, status: bool);

event eWriteRequest  : tWriteRequest;
event eWriteResponse : tWriteRequest;

event ePropagateWrite: tWriteRequest;

event eReadRequest   : tReadRequest;
event eReadResponse  : tReadResponse;

machine Head {
    var kv: map[int, int];
    
    start state Receiving {
        on eWriteRequest do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send next_(this), ePropagateWrite, (source = req.source, k = req.k, v = req.v);
        }
    }
}

machine Body {
    var kv: map[int, int];
    
    start state Forwarding {
        on ePropagateWrite do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send next_(this), ePropagateWrite, (source = req.source, k = req.k, v = req.v);
        }
    }
}


machine Tail {
    var kv: map[int, int];
    
    start state Replying {
        on ePropagateWrite do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send req.source, eWriteResponse, (source = req.source, k = req.k, v = req.v);
        }

        on eReadRequest do (req: tReadRequest) {
            if (req.k in kv) {
                send req.source, eReadResponse, (source = req.source, k = req.k, v = kv[req.k], status = true);
            } else {
                send req.source, eReadResponse, (source = req.source, k = req.k, v = -1, status = false);
            }
        }
    }
}

machine Client {
    start state Loop {
        entry {
            var k: int;
            var v: int; 
            
            k = RandomInt();
            
            if ($) {
                v = RandomInt();
                send head(), eWriteRequest, (source = this, k = k, v = v);
            } else {
                send tail(), eReadRequest, (source = this, k = k);
            }
            
            goto Loop;
        }
        ignore eWriteResponse, eReadResponse; // not actually doing anything with these in the client
    }
}

fun RandomInt(): int;

spec StrongConsistency observes eReadResponse, eWriteResponse {
    var kv : map[int, int];
    start state WaitForEvents {
        on eWriteResponse do (resp: tWriteRequest) {
            kv[resp.k] = resp.v;
        }
        on eReadResponse do (resp: tReadResponse) {
            if (resp.k in kv) {
                assert resp.status == true;
                assert resp.v == kv[resp.k];
            } else {
                assert resp.status == false;
            }
        }
    }
}

// begin proof
pure head(): machine;
pure tail(): machine;
pure next_(m: machine): machine;

// assume the pure functions match the state at the start
init forall (m: machine) :: m == head() == m is Head;
init forall (m: machine) :: m == tail() == m is Tail;
init forall (m: machine) :: next_(m) is Body || next_(m) is Tail;
// ensure that the pure functions continue to match the state as time goes on
invariant next_1: forall (m: machine) :: m == head() == m is Head;
invariant next_2: forall (m: machine) :: m == tail() == m is Tail;
invariant next_3: forall (m: machine) :: next_(m) is Body || next_(m) is Tail;

// Aux invariants for P obligations 
invariant only_head_receives_writes:           forall (m: machine, e: event) :: (inflight e && e is eWriteRequest && e targets m)   ==> (m is Head);
invariant only_tail_receives_reads:            forall (m: machine, e: event) :: (inflight e && e is eReadRequest && e targets m)    ==> (m is Tail);
invariant only_client_receives_read_response:  forall (m: machine, e: event) :: (inflight e && e is eReadResponse && e targets m)   ==> (m is Client);
invariant only_client_receives_write_response: forall (m: machine, e: event) :: (inflight e && e is eWriteResponse && e targets m)  ==> (m is Client);
invariant only_body_or_tail_receive_props:     forall (m: machine, e: event) :: (inflight e && e is ePropagateWrite && e targets m) ==> (m is Tail || m is Body);
invariant source_is_always_client_1: forall (e: eWriteRequest)   :: inflight e ==> e.source is Client;
invariant source_is_always_client_2: forall (e: eReadRequest)    :: inflight e ==> e.source is Client;
invariant source_is_always_client_3: forall (e: ePropagateWrite) :: inflight e ==> e.source is Client;
invariant source_is_always_client_4: forall (e: eReadResponse)   :: inflight e ==> e.source is Client;
invariant source_is_always_client_5: forall (e: eWriteResponse)  :: inflight e ==> e.source is Client;

// Aux invariant for spec machine
invariant tail_sync: forall (t: Tail) :: t.kv == StrongConsistency.kv;
