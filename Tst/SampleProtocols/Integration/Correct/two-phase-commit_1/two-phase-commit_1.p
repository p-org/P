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
event MONITOR_WRITE_SUCCESS:(m: machine, key:int, val:int);
event MONITOR_WRITE_FAILURE:(m: machine, key:int, val:int);
event MONITOR_READ_SUCCESS:(m: machine, key:int, val:int);
event MONITOR_READ_FAILURE:machine;
event MONITOR_UPDATE:(m:machine, key:int, val:int);
event goEnd;
event final;

/*
All the external APIs which are called by the protocol
*/
model Timer {
	var target: machine;
	start state Init {
		entry {
			target = payload as machine;
			raise(Unit);
		}
		on Unit goto Loop;
	}

	state Loop {
		ignore CancelTimer;
		on StartTimer goto TimerStarted;
	}

	state TimerStarted {
		entry {
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
		};
	}
}

machine Replica {
	var coordinator: machine;
    var data: map[int,int];
	var pendingWriteReq: (seqNum: int, key: int, val: int);
	var lastSeqNum: int;

    start state Init {
	    entry {
		    coordinator = payload as machine;
			lastSeqNum = 0;
			raise(Unit);
		}
		on Unit goto Loop;
	}

	fun HandleReqReplica() {
		pendingWriteReq = (payload as (seqNum:int, key:int, val:int));
		assert (pendingWriteReq.seqNum > lastSeqNum);
		if (ShouldCommitWrite()) {
			send coordinator, RESP_REPLICA_COMMIT, pendingWriteReq.seqNum;
		} else {
			send coordinator, RESP_REPLICA_ABORT, pendingWriteReq.seqNum;
		}
	}

	fun HandleGlobalAbort() {
		assert (pendingWriteReq.seqNum >= payload);
		if (pendingWriteReq.seqNum == payload) {
			lastSeqNum = payload;
		}
	}

	fun HandleGlobalCommit() {
		assert (pendingWriteReq.seqNum >= payload);
		if (pendingWriteReq.seqNum == payload) {
			data[pendingWriteReq.key] = pendingWriteReq.val;
			//invoke Termination(MONITOR_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
			lastSeqNum = payload;
		}
	}

	fun ReadData(){
		if(payload in data)
			send coordinator, REP_READ_SUCCESS, data[payload];
		else
			send coordinator, REP_READ_FAIL;
	}
	
	state Loop {
		on GLOBAL_ABORT do HandleGlobalAbort;
		on GLOBAL_COMMIT do HandleGlobalCommit;
		on REQ_REPLICA do HandleReqReplica;
		on READ_REQ_REPLICA do ReadData;
	}

	model fun ShouldCommitWrite(): bool 
	{
		return $;
	}
}

machine Coordinator {
	var data: map[int,int];
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
		entry {
			numReplicas = payload as int;
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
		entry {
			client = payload.client;
			key = payload.key;
			push PerformRead;
			if(readResult.0)
				raise(READ_FAIL);
			else
				raise READ_SUCCESS, readResult.1;
		}
		on READ_FAIL goto Loop with
		{
			send client, READ_FAIL;
		};
		on READ_SUCCESS goto Loop with
		{	
			send client, READ_SUCCESS, payload;
		};
	}
	
	model fun ChooseReplica()
	{
			if($) 
				send replicas[0], READ_REQ_REPLICA, key;
			else
				send replicas[sizeof(replicas) - 1], READ_REQ_REPLICA, key;
				
	}
	
	state PerformRead {
		entry{ ChooseReplica(); }
		on REP_READ_FAIL do ReturnResult;
		on REP_READ_SUCCESS do ReturnResult;
		
	}
	
	fun ReturnResult() {
		if(trigger == REP_READ_FAIL)
			readResult = (true, -1);
		else
			readResult = (false, payload as int);
		
		return;
	}
	fun DoWrite (){
		pendingWriteReq = payload;
		currSeqNum = currSeqNum + 1;
		i = 0;
		while (i < sizeof(replicas)) {
			send replicas[i], REQ_REPLICA, (seqNum=currSeqNum, key=pendingWriteReq.key, val=pendingWriteReq.val);
			i = i + 1;
		}
		send timer, StartTimer, 100;
		raise(Unit);
	}

	state Loop {
		on WRITE_REQ do DoWrite;
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
		entry {
			if (i == 0) {
				while (i < sizeof(replicas)) {
					send replicas[i], GLOBAL_COMMIT, currSeqNum;
					i = i + 1;
				}
				data[pendingWriteReq.key] = pendingWriteReq.val;
				//invoke Termination(MONITOR_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
				send pendingWriteReq.client, WRITE_SUCCESS;
				send timer, CancelTimer;
				raise(Unit);
			}
		}
		defer WRITE_REQ, READ_REQ;
		on RESP_REPLICA_COMMIT goto CountVote with {
			if (currSeqNum == payload) {
				i = i - 1;
			}
		};
		on RESP_REPLICA_ABORT do HandleAbort;
		on Timeout goto Loop with {
			DoGlobalAbort();
		};
		on Unit goto WaitForCancelTimerResponse;
	}

	fun HandleAbort() {
		if (currSeqNum == payload) {
			DoGlobalAbort();
			send timer, CancelTimer;
			raise(Unit);
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

model Client {
    var coordinator: machine;
	var mydata : int;
	var counter : int;
   
	
	start state Init {
	    entry {
	        coordinator = (payload as (machine, int)).0;
			mydata = (payload as (machine, int)).1;
			counter = 0;
			new ReadWrite(this);
			raise(Unit);
		}
		on Unit goto DoWrite;
	}
	
	
	state DoWrite {
	    entry {
			mydata = mydata + 1; 
			counter = counter + 1;
			if(counter == 3)
				raise(goEnd);
			send coordinator, WRITE_REQ, (client=this, key=mydata, val=mydata);
		}
		on WRITE_FAIL goto DoRead with 
		{
			monitor ReadWrite, MONITOR_WRITE_FAILURE, (m = this, key=mydata, val = mydata);
		};
		on WRITE_SUCCESS goto DoRead with 
		{
			monitor ReadWrite, MONITOR_WRITE_SUCCESS, (m = this, key=mydata, val = mydata);
		};
		on goEnd goto End;
	}

	state DoRead {
	    entry {
			send coordinator, READ_REQ, (client=this, key=mydata);
		}
		on READ_FAIL goto DoWrite with 
		{
			monitor ReadWrite, MONITOR_READ_FAILURE, this;
		};
		on READ_SUCCESS goto DoWrite with 
		{
			monitor ReadWrite, MONITOR_READ_SUCCESS, (m = this, key = mydata, val = payload);
		};
	}

	state End { }

}

//Monitors


// ReadWrite monitor keeps track of the property that every successful write should be followed by
// successful read and failed write should be followed by a failed read.
// This monitor is created local to each client.

monitor ReadWrite {
	var client : machine;
	var data: (key:int,val:int);
	fun DoWriteSuccess() {
		if(payload.m == client)
			data = (key = payload.key, val = payload.val);
	}
	
	fun DoWriteFailure() {
		if(payload.m == client)
			data = (key = -1, val = -1);
	}
	fun CheckReadSuccess() {
		if(payload.m == client)
		{assert(data.key == payload.key && data.val == payload.val+100);}
			
	}
	fun CheckReadFailure() {
		if(payload == client)
			assert(data.key == -1 && data.val == -1);
	}
	start state Init {
		entry {
			client = payload as machine;
		}
		on MONITOR_WRITE_SUCCESS do DoWriteSuccess;
		on MONITOR_WRITE_FAILURE do DoWriteFailure;
		on MONITOR_READ_SUCCESS do CheckReadSuccess;
		on MONITOR_READ_FAILURE do CheckReadFailure;
	}
}

main model TwoPhaseCommit {
    var coordinator: machine;
    start state Init {
	    entry {
	        coordinator = new Coordinator(2);
			new Client((coordinator, 100));
			new Client((coordinator, 200));
		}
	}
}