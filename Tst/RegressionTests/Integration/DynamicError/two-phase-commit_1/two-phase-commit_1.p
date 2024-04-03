event REQ_REPLICA:(seqNum:int, key:int, val:int);
event RESP_REPLICA_COMMIT:int;
event RESP_REPLICA_ABORT:int;
event GLOBAL_ABORT:int;
event GLOBAL_COMMIT:int;
event WRITE_REQ:(client:machine, key:int, val:int);
event WRITE_FAIL;
event WRITE_SUCCESS;
event READ_REQ_REPLICA:int;
event READ_REQ:(client:machine, key:int);
event READ_FAIL;
event READ_SUCCESS:int;
event REP_READ_FAIL;
event REP_READ_SUCCESS:int;
event Unit;
event Timeout;
event StartTimer:int;
event CancelTimer;
event CancelTimerFailure;
event CancelTimerSuccess;
event announce_WRITE_SUCCESS:(m: machine, key:int, val:int);
event announce_WRITE_FAILURE:(m: machine, key:int, val:int);
event announce_READ_SUCCESS:(m: machine, key:int, val:int);
event announce_READ_FAILURE:machine;
event announce_UPDATE:(m:machine, key:int, val:int);
event goEnd;
//event final;

/*
All the external APIs which are called by the protocol
*/
machine Timer {
	var target: machine;
	start state Init {
		entry (payload: machine) {
			target = payload;
			raise(Unit);
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
				raise(Unit);
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
	var pendingWriteReq: (seqNum: int, key: int, val: int);
	var lastSeqNum: int;
	var shouldCommit : bool;
	
    start state Init {
	    entry (payload : machine){
			coordinator = payload;
			lastSeqNum = 0;
			raise Unit;
		}
		on Unit goto Loop;
	}

	fun HandleReqReplica(payload :(seqNum:int, key:int, val:int)) {
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
				dataValues[pendingWriteReq.key] = pendingWriteReq.val;
				lastSeqNum = payload;
			}
		}
		
		on REQ_REPLICA do (payload :(seqNum:int, key:int, val:int)) { HandleReqReplica(payload); }
		on READ_REQ_REPLICA do (payload : int) {
			if(payload in dataValues)
				send coordinator, REP_READ_SUCCESS, dataValues[payload];
			else
				send coordinator, REP_READ_FAIL;
		}
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
	var pendingWriteReq: (client: machine, key: int, val: int);
	var replica: machine;
	var currSeqNum:int;
	var timer: machine;
	var client:machine;
	var key: int;
	var readResult: (bool, int);
	start state Init {
		entry (payload : int) {
			numReplicas = payload;
			assert (numReplicas > 0);
			i = 0;
			while (i < numReplicas) {
				replica = new Replica(this);
				replicas += (i, replica);
				i = i + 1;
			}
			currSeqNum = 0;
			//new Termination(this, replicas);
			timer = new Timer(this);
			raise(Unit);
		}
		on Unit goto Loop;
	}

	state DoRead {
		entry (payload :(client:machine, key:int)) {
			client = payload.client;
			key = payload.key;
			
			if($)
				send replicas[0], READ_REQ_REPLICA, key;
			else
				send replicas[sizeof(replicas) - 1], READ_REQ_REPLICA, key;
			receive
			{
				case REP_READ_FAIL : { readResult = (true, -1); }
				case REP_READ_SUCCESS : (payload1 : int) { readResult = (false, payload1); }
			}
			if(readResult.0)
				raise(READ_FAIL);
			else
				raise READ_SUCCESS, readResult.1;
		}
		on READ_FAIL goto Loop with
		{
			send client, READ_FAIL;
		}
		on READ_SUCCESS goto Loop with (payload: int)
		{	
			send client, READ_SUCCESS, payload;
		}
	}


	
	fun DoWrite(pendingWriteReq : (client:machine, key:int, val:int)) {
		currSeqNum = currSeqNum + 1;
		i = 0;
		while (i < sizeof(replicas)) {
			send replicas[i], REQ_REPLICA, (seqNum=currSeqNum, key =pendingWriteReq.key, val=pendingWriteReq.val);
			i = i + 1;
		}
		send timer, StartTimer, 100;
		raise Unit;
	}

	state Loop {
		on WRITE_REQ do (payload : (client:machine, key:int, val:int)) {DoWrite(payload);}
		on Unit goto CountVote;
		on READ_REQ goto DoRead;
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
				dataValues[pendingWriteReq.key] = pendingWriteReq.val;
				//invoke Termination(announce_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
				send pendingWriteReq.client, WRITE_SUCCESS;
				send timer, CancelTimer;
				raise(Unit);
			}
		}
		defer WRITE_REQ, READ_REQ;
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
	var mydata : int;
	var counter : int;

	
	start state Init {
	    entry (payload :(machine, int)){
	        coordinator = payload.0;
			mydata = payload.1;
			counter = 0;
			raise(Unit);
		}
		on Unit goto DoWrite;
	}
	
	
	state DoWrite {
	    entry (payload: any) {
			mydata = mydata + 1;
			counter = counter + 1;
			if(counter == 3)
				raise(goEnd);
			send coordinator, WRITE_REQ, (client=this, key=mydata, val=mydata);
		}
		on WRITE_FAIL goto DoRead;
		on WRITE_SUCCESS goto DoRead;
		on goEnd goto End;
	}

	state DoRead {
	    entry {
			send coordinator, READ_REQ, (client=this, key=mydata);
		}
		on READ_FAIL goto DoWrite;
		on READ_SUCCESS goto DoWrite;
	}

	state End { }

}


machine Main {
    var coordinator: machine;
    start state Init {
	    entry {
	        coordinator = new Coordinator(2);
			new Client((coordinator, 100));
			new Client((coordinator, 200));
		}
	}
}
