type tWriteRequest  = (source: machine, k: int, v: int);
type tReadRequest   = (source: machine, k: int);
type tReadResponse  = (source: machine, k: int, v: int, status: bool);

event eWriteRequest  : tWriteRequest;
event eWriteResponse : tWriteRequest;

event ePropagateWrite: tWriteRequest;

event eReadRequest   : tReadRequest;
event eReadResponse  : tReadResponse;

enum tRole {HEAD, BODY, TAIL}

machine Replicate {
    var next: machine;
    var kv: map[int, int];

    start state Boot {
        entry (input: (role: tRole)) {
            if (input.role == HEAD) {
                goto Head;
            } else if (input.role == BODY) {
                goto Body;
            } else {
                goto Tail;
            }
        }
    }

    state Head {
        on eWriteRequest do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send next, ePropagateWrite, (source = req.source, k = req.k, v = req.v);
        }
    }

    state Body {
        on ePropagateWrite do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send next, ePropagateWrite, (source = req.source, k = req.k, v = req.v);
        }
    }

    state Tail {
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

invariant client_is_never_next: forall (r: Replicate) :: !(r.next is Client);