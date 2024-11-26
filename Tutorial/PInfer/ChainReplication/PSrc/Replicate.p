type tKey = int;
type tValue = int;
type tRid = int;
type tPos = int;
type tEpoch = int;
type tSequencer = int;
type tTime = int;
type tKVMsg = (source: machine, k: tKey, v: tValue, id: tRid);

event eWriteRequest  : tKVMsg;
event eWriteResponse : (k: tKey, v: tValue, sequence_id: tSequencer);

event eReadRequest   : tKVMsg;
event eReadSuccess: (target: machine, k: tKey, v: tValue, sequence_id: tSequencer);
event eReadFail: (k: tKey);
event eNotifyLog : (epoch: tEpoch, position: tPos, log: seq[tRid]);

machine Replicate {
    var master: Master;
    var replicates: seq[Replicate]; // The view of whole chain replicates
    var position: tPos; // Position in the chain replicates
    var log: seq[tKVMsg]; //local log
    var updates: seq[tRid];
    var kv: map[tKey, tValue]; //local store
    var epoch: tEpoch;
    var sequencer: map[tKey, tSequencer];
    // var time_machine: TimeMachine;

    start state Boot {
        entry {
            updates = default(seq[tRid]);
            sequencer = default(map[tKey, tSequencer]);
        }

        on eNotifyChainShape do (input: (master: Master, replicates: seq[Replicate], position: tPos)) {
            master = input.master;
            replicates = input.replicates;
            position = input.position;
            send master, eAck, this as machine;
            goto Active;
        }

        defer eReadRequest, eWriteRequest;
    }

    state Active {
        on eWriteRequest do (req: tKVMsg) {
            UpdateKVAndLog(req);
            if(!IsTail()) {
                send GetNext(), eWriteRequest, req;
            } else {
                // send time_machine, eRequestTime;
                // receive {
                //     on eResponseTime do (resp: (trueTime: tTime)) {
                //         send req.source, eWriteResponse, req;
                //     }
                // }
                send req.source, eWriteResponse, (k = req.k, v = req.v, sequence_id = sequencer[req.k]);
            }
            if ($) {
                announce eNotifyLog, (epoch = epoch, position = position, log = updates);
            }
        }

        on eReadRequest do (req: tKVMsg) {
            var kvmsg: tKVMsg;
            var status: bool;
            assert IsTail(), "Can only send read requests to the tail of chain.";
            if (req.k in kv) {
                send req.source, eReadSuccess, (target=req.source, k = req.k, v = kv[req.k], sequence_id = sequencer[req.k]);
            } else {
                send req.source, eReadFail, (k=req.k,);
            }
        }

        on eNotifyRecoverReq do {
            goto Recover;
        }

        on eLogReq do {
            send master, eLogResp, (source = this, log = log);
        }

        on eNotifyChainShape do (input: (master: Master, replicates: seq[Replicate], position: int)) {
            master = input.master;
            replicates = input.replicates;
            position = input.position;
            send master, eAck, this as machine;
        }

        on eShutDown do {
            Crash();
        }
    }

    state Recover {
        entry {
            var i: int;
            while (i < sizeof(log)) {
                send log[i].source, eWriteResponse, (k = log[i].k, v = log[i].v, sequence_id = sequencer[log[i].k]);
                i = i + 1; 
            }
            send master, eNotifyRecoverResp;
        }
        
        on eNotifyChainShape do (input: (master: Master, replicates: seq[Replicate], position: int)) {
            master = input.master;
            replicates = input.replicates;
            position = input.position;
            send master, eAck, this as machine;
        }
        
        on eShutDown do {
            Crash();
        }
        defer eWriteRequest, eReadRequest;
        ignore eNotifyRecoverReq; 
    }

    fun GetNext(): Replicate {
        assert !IsTail(), "the tail node has no next machine.";
        return replicates[position + 1];
    }

    fun Crash() {
        send master, eNotifyReplicateCrash, (replicate = this, position = position);
        raise halt;
    } 

    fun IsHead(): bool {
        return (position == 0);
    }

    fun IsTail(): bool {
        return (position == sizeof(replicates) - 1);
    }

    fun UpdateKVAndLog (req: tKVMsg) {
        epoch = epoch + 1;
        kv[req.k] = req.v;
        if (!(req.k in sequencer)) {
            sequencer[req.k] = 0;
        } else {
            sequencer[req.k] = sequencer[req.k] + 1;
        }
        log += (sizeof(log), req);
        updates += (sizeof(updates), req.id);
    }
}