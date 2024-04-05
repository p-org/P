event REQ_REPLICA:(seqNum:int, idx:int, val:int);
event RESP_REPLICA_COMMIT:int;
event RESP_REPLICA_ABORT:int;
event GLOBAL_ABORT:int;
event GLOBAL_COMMIT:int;
event WRITE_REQ:(client:machine, idx:int, val:int);
event WRITE_FAIL;
event WRITE_SUCCESS;
event READ_REQ:(client:machine, idx:int);
event READ_FAIL;
event READ_UNAVAILABLE;
event READ_SUCCESS:int;
event Unit;
event Timeout;
event StartTimer:int;
event CancelTimer;
event CancelTimerFailure;
event CancelTimerSuccess;
event announce_WRITE:(idx:int, val:int);
event announce_READ_SUCCESS:(idx:int, val:int);
event announce_READ_UNAVAILABLE:int;

machine Timer {
	var target: machine;
	start state Init {
		entry (payload : machine){
			target = payload;
			raise Unit;
		}
		on Unit goto Loop;
	}

	state Loop {
		ignore CancelTimer;
		on StartTimer goto TimerStarted;
	}

	state TimerStarted {
		entry (payload: int) {
			if ($) {
				send target, Timeout;
				raise Unit;
			}
		}
		on Unit goto Loop;
		on CancelTimer goto Loop with {
			if ($) {
				send target, CancelTimerFailure;
				send target, Timeout;
			} else {
				send target, CancelTimerSuccess;
			}		
		}
	}
}

machine Replica {
	var coordinator: machine;
    var dataValues: map[int,int];
	var pendingWriteReq: (seqNum: int, idx: int, val: int);
	var shouldCommit: bool;
	var lastSeqNum: int;

    start state Init {
	    entry (payload : machine){
		  coordinator = payload;
			lastSeqNum = 0;
			raise Unit;
		}
		on Unit goto Loop;
	}

	fun HandleReqReplica(payload :(seqNum:int, idx:int, val:int)) {
		pendingWriteReq = payload;
		assert (pendingWriteReq.seqNum > lastSeqNum);
		shouldCommit = ShouldCommitWrite();
		if (shouldCommit) {
			send coordinator, RESP_REPLICA_COMMIT, pendingWriteReq.seqNum;
		} else {
			send coordinator, RESP_REPLICA_ABORT, pendingWriteReq.seqNum;
		}
	}


	state Loop {
		on GLOBAL_ABORT do (payload: int) {
			assert (pendingWriteReq.seqNum >= payload);
			if (pendingWriteReq.seqNum == payload) {
				lastSeqNum = payload;
			}
		}
		on GLOBAL_COMMIT do (payload:int) {
			assert (pendingWriteReq.seqNum >= payload);
			if (pendingWriteReq.seqNum == payload) {
				dataValues[pendingWriteReq.idx] = pendingWriteReq.val;
				lastSeqNum = payload;
			}
		}
		
		on REQ_REPLICA do (payload :(seqNum:int, idx:int, val:int)) { HandleReqReplica(payload); }
	}

	fun ShouldCommitWrite(): bool
	{
		return $;
	}
}

machine Coordinator {
	var dataValues: map[int,int];
	var replicas: seq[machine];
	var numReplicas: int;
	var i: int;
	var pendingWriteReq: (client: machine, idx: int, val: int);
	var replica: machine;
	var currSeqNum:int;
	var timer: machine;

	start state Init {
		entry (payload : int){
			numReplicas = payload;
			assert (numReplicas > 0);
			i = 0;
			while (i < numReplicas) {
				replica = new Replica(this);
				replicas += (i, replica);
				i = i + 1;
			}
			currSeqNum = 0;
			timer = new Timer(this);
			raise Unit;
		}
		on Unit goto Loop;
	}

	fun DoRead(payload: (client:machine, idx:int)) {
		if (payload.idx in dataValues) {
			announce announce_READ_SUCCESS, (idx=payload.idx, val=dataValues[payload.idx]);
			send payload.client, READ_SUCCESS, dataValues[payload.idx];
		} else {
			announce announce_READ_UNAVAILABLE, payload.idx;
			send payload.client, READ_UNAVAILABLE;
		}
	}

	fun DoWrite(payload : (client:machine, idx:int, val:int)) {
		pendingWriteReq = payload;
		currSeqNum = currSeqNum + 1;
		i = 0;
		while (i < sizeof(replicas)) {
			send replicas[i], REQ_REPLICA, (seqNum=currSeqNum, idx=pendingWriteReq.idx, val=pendingWriteReq.val);
			i = i + 1;
		}
		send timer, StartTimer, 100;
		raise Unit;
	}

	state Loop {
		on WRITE_REQ do (payload : (client:machine, idx:int, val:int)) {DoWrite(payload);}
		on Unit goto CountVote;
		on READ_REQ do (payload : (client:machine, idx:int)) {DoRead(payload);}
		ignore RESP_REPLICA_COMMIT, RESP_REPLICA_ABORT;
	}

	fun DoGlobalAbort() {
		i = 0;
		while (i < sizeof(replicas)) {
			send replicas[i], GLOBAL_ABORT, currSeqNum;
			i = i + 1;
		}
		send pendingWriteReq.client, WRITE_FAIL;
	}

	state CountVote {
		entry (payload: any) {
			if (i == 0) {
				while (i < sizeof(replicas)) {
					send replicas[i], GLOBAL_COMMIT, currSeqNum;
					i = i + 1;
				}
				dataValues[pendingWriteReq.idx] = pendingWriteReq.val;
				announce announce_WRITE, (idx=pendingWriteReq.idx, val=pendingWriteReq.val);
				send pendingWriteReq.client, WRITE_SUCCESS;
				send timer, CancelTimer;
				raise Unit;
			}
		}
		defer WRITE_REQ;
		on READ_REQ do (payload : (client:machine, idx:int)) { DoRead(payload);}
		on RESP_REPLICA_COMMIT goto CountVote with (payload : int){
			if (currSeqNum == payload) {
				i = i - 1;
			}
		}
		on RESP_REPLICA_ABORT do (payload : int) { HandleAbort(payload); }
		on Timeout goto Loop with {
			DoGlobalAbort();
		}
		on Unit goto WaitForCancelTimerResponse;
	}

	fun HandleAbort(payload : int) {
		if (currSeqNum == payload) {
			DoGlobalAbort();
			send timer, CancelTimer;
			raise Unit;
		}
	}

	state WaitForCancelTimerResponse {
		defer WRITE_REQ, READ_REQ;
		ignore RESP_REPLICA_COMMIT, RESP_REPLICA_ABORT;
		on Timeout, CancelTimerSuccess goto Loop;
		on CancelTimerFailure goto WaitForTimeout;
	}

	state WaitForTimeout {
		defer WRITE_REQ, READ_REQ;
		ignore RESP_REPLICA_COMMIT, RESP_REPLICA_ABORT;
		on Timeout goto Loop;
	}
}

machine Client {
    var coordinator: machine;
    start state Init {
	    entry (payload : machine) {
	        coordinator = payload;
			raise Unit;
		}
		on Unit goto DoWrite;
	}
	var idx: int;
	var val: int;
	state DoWrite {
	    entry {
			idx = ChooseIndex();
			val = ChooseValue();
			send coordinator, WRITE_REQ, (client=this, idx=idx, val=val);
		}
		on WRITE_FAIL goto End;
		on WRITE_SUCCESS goto DoRead;
	}

	state DoRead {
	    entry {
			send coordinator, READ_REQ, (client=this, idx=idx);
		}
		on READ_FAIL goto End;
		on READ_SUCCESS goto End;
	}

	state End { }

	fun ChooseIndex(): int
	{
		if ($) {
			return 0;
		} else {
			return 1;
		}
	}

	fun ChooseValue(): int
	{
		if ($) {
			return 0;
		} else {
			return 1;
		}
	}
}

spec M observes announce_WRITE, announce_READ_SUCCESS, announce_READ_UNAVAILABLE {
	var dataValues: map[int,int];

	start state Init {
		on announce_WRITE do (payload: (idx:int, val:int)) { dataValues[payload.idx] = payload.val; }
		on announce_READ_SUCCESS do (payload : (idx:int, val:int)) {
			assert(payload.idx in dataValues);
			assert(dataValues[payload.idx] == payload.val);
		}
		on announce_READ_UNAVAILABLE do (payload: int) {
			assert(!(payload in dataValues));
		}
	}
}

machine Main {
    var coordinator: machine;
	var client: machine;
    start state Init {
	    entry {
	        coordinator = new Coordinator(2);
			client = new Client(coordinator);
			client = new Client(coordinator);
		}
	}
}
