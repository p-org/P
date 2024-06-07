type tWriteRequest  = (source: machine, k: int, v: int, id: int);
type tReadRequest   = (source: machine, k: int, id: int);
type tReadResponse  = (source: machine, k: int, v: int, status: bool, id: int);

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
            send next, ePropagateWrite, (source = req.source, k = req.k, v = req.v, id = req.id);
        }
    }

    state Body {
        on ePropagateWrite do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send next, ePropagateWrite, (source = req.source, k = req.k, v = req.v, id = req.id);
        }
    }

    state Tail {
        on ePropagateWrite do (req: tWriteRequest) {
            kv[req.k] = req.v;
            send req.source, eWriteResponse, (source = req.source, k = req.k, v = req.v, id = req.id);
        }

        on eReadRequest do (req: tReadRequest) {
            if (req.k in kv) {
                send req.source, eReadResponse, (source = req.source, k = req.k, v = kv[req.k], status = true, id = req.id);
            } else {
                send req.source, eReadResponse, (source = req.source, 
                    k = req.k, v = -1, status = false, id = req.id);
            }
        }
    }
}