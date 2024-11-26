/* Client.p : send read/write reqest */

machine Client {
    // internal state
    var head : Replicate;
    var tail : Replicate;
    var keyBound : int;
    var numOps : int;
    var opCt : int;
    var valueBound: int;

    // initial state
    start state Init {
        entry (input: (numOps: int, keyBound: int, valueBound: int)) {
            keyBound = input.keyBound;
            numOps = input.numOps;
            valueBound = input.valueBound;
            opCt = 0;
        }
        on eNotifyNewHeadTail do (input: (master: Master, h : Replicate, t: Replicate)) {
            head = input.h;
            tail = input.t;
            send input.master, eAck, this as machine;
            goto Active; 
        }
    }
 
    state Active {
        entry {
            var kvmsg: tKVMsg;

            if (opCt < numOps) {
                opCt = opCt + 1;
                // randomly choose to read or write
                if ($) { 
                    kvmsg = (source = this, k = choose(keyBound), v = choose(valueBound), id = opCt);
                    send 
                        head,      // target machine is the head
                        eWriteRequest,    // message (event) being sent
                        kvmsg;     // message payload
                } else {
                    // get
                    kvmsg = (source = this, k = choose(keyBound), v = -1, id = opCt);
                    send 
                        tail,      // target machine is the tail
                        eReadRequest,    // message (event) being sent
                        kvmsg;     // message payload
                }
                goto WaitForResp;
            } else {
                goto Done;
            }
        }

        on eNotifyNewHeadTail do (input: (master: Master, h : Replicate, t: Replicate)) {
            head = input.h;
            tail = input.t;
            send input.master, eAck, this as machine;
        }
    }

    state WaitForResp {
        on eWriteResponse do (r : (k: tKey, v: tValue, sequence_id: tSequencer)) {
            // for debugging
            print format("WRITE response of. Key: {0}, Value: {1}.", r.k, r.v);
            goto Active;
        }

        on eReadSuccess do (r : (target: machine, k: tKey, v: tValue, sequence_id: tSequencer)) {
            // for debugging
            print format("READ response of. Key: {0}; Value: {1}; Sequencer: {2}.", r.k, r.v, r.sequence_id);
            goto Active;
        }

        on eReadFail do (r : (k: tKey)) {
            goto Active;
        }

        on eNotifyNewHeadTail do (input: (master: Master, h : Replicate, t: Replicate)) {
            head = input.h;
            tail = input.t;
            send input.master, eAck, this as machine;
        }
    }

    state Done {
        ignore eWriteResponse, eReadFail, eReadSuccess, eNotifyNewHeadTail;
    }
}
