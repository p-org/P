event WRITE_REQ_REPLICA:(seqNum:int, key:int, val:int);
event RESP_REPLICA_COMMIT:int;
event RESP_REPLICA_ABORT:int;
event GLOBAL_ABORT:int;
event GLOBAL_COMMIT:int;
event WRITE_REQ:(client:id, key:int, val:int);
event WRITE_FAIL;
event WRITE_SUCCESS;
event READ_REQ_REPLICA:int;
event READ_REQ:(client:id, key:int);
event READ_FAIL:int;
event READ_SUCCESS:int;
event REP_READ_FAIL;
event REP_READ_SUCCESS:int;
event Unit;
event Timeout;
event StartTimer:int;
event CancelTimer;
event CancelTimerFailure;
event CancelTimerSuccess;
event MONITOR_WRITE_SUCCESS:(m: id, key:int, val:int);
event MONITOR_WRITE_FAILURE:(m: id, key:int, val:int);
event MONITOR_READ_SUCCESS:(m: id, key:int, val:int);
event MONITOR_READ_FAILURE:id;
event MONITOR_UPDATE:(m:id, key:int, val:int);
event goEnd;
event final;

/*
All the external APIs which are called by the protocol
*/
model machine Timer {
	var target: id;
	start state Init {
		entry {
			target = (id)payload;
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
			if (*) {
				send(target, Timeout);
				raise(Unit);
			}
		}
		on Unit goto Loop;
		on CancelTimer goto Loop {
			if (*) {
				send(target, CancelTimerFailure);
				send(target, Timeout);
			} else {
				send(target, CancelTimerSuccess);
			}		
		};
	}
}

machine Replica 
\begin{Replica}
	var coordinator: id;
    var data: map[int,int];
	var pendingWriteReq: (seqNum: int, key: int, val: int);
	var shouldCommit: bool;
	var lastSeqNum: int;
	
	state Init {
		entry {

			coordinator = (id)payload;
			lastSeqNum = 0;
			raise(Unit);
		}
		on Unit goto WaitForRequest;
	}

	state WaitCommitAbort {
		defer READ_REQ_REPLICA, WRITE_REQ_REPLICA; //defer everything else
		entry {
			pendingWriteReq = ((seqNum:int, key:int, val:int))payload;
			assert (pendingWriteReq.seqNum > lastSeqNum);
			shouldCommit = ShouldCommitWrite();
			if (shouldCommit) {
				_SEND(coordinator, RESP_REPLICA_COMMIT, pendingWriteReq.seqNum);
			} else {
				_SEND(coordinator, RESP_REPLICA_ABORT, pendingWriteReq.seqNum);
			}
		}
		on GLOBAL_ABORT do CheckResponse;
		on GLOBAL_COMMIT do CheckResponse;
		on Unit goto WaitForRequest;
	}

	action CheckResponse {
		if(trigger == GLOBAL_ABORT)
		{
			if (pendingWriteReq.seqNum == (int)payload) {
					lastSeqNum = (int)payload;
					raise(Unit);
			}
			else
			{
				//just drop the abort
			}
		}
		else 
		{
			assert (pendingWriteReq.seqNum >= (int)payload);
			if (pendingWriteReq.seqNum == (int)payload) {
				data.update(pendingWriteReq.key, pendingWriteReq.val);
				//invoke Termination(MONITOR_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
				lastSeqNum = (int)payload;
				raise(Unit);
			}
			else
			{
				assert(false); // Not possible to move ahead of me.
			}
		}
	}

	action SendReadData{
		if(payload in data)
			_SEND(coordinator, REP_READ_SUCCESS, data[payload]);
		else
			_SEND(coordinator, REP_READ_FAIL, null);
	}
	
	state WaitForRequest {
		ignore GLOBAL_ABORT;
		on WRITE_REQ_REPLICA goto WaitCommitAbort;
		on READ_REQ_REPLICA do SendReadData;
	}

	model fun ShouldCommitWrite(): bool 
	{
		return *;
	}
\end{Replica}

machine Coordinator 
\begin{Coordinator}
	var data: map[int,int];
	var replicas: seq[id];
	var numReplicas: int;
	var i: int;
	var pendingWriteReq: (client: id, key: int, val: int);
	var replica: id;
	var currSeqNum:int;
	var timer: mid;
	var client:id;
	var key: int;
	var readResult: (bool, int);
	var creatorMachine:id;
	var temp_NM:id;
	
	state Init {
		entry {
			
			numReplicas = (int)payload;
			assert (numReplicas > 0);
			i = 0;
			while (i < numReplicas) {
				temp_NM = _CREATENODE();
				createmachine_param = (nodeManager = temp_NM, typeofmachine = 2, param = receivePort);
				call(_CREATEMACHINE);
				replica = createmachine_return;
				replicas.insert(i, replica);
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
			call(PerformRead);
			if(readResult[0])
				raise(READ_FAIL, readResult[1]);
			else
				raise(READ_SUCCESS, readResult[1]);
		}
		on READ_FAIL goto Loop
		{
			_SEND(client, READ_FAIL, payload);
		};
		on READ_SUCCESS goto Loop
		{	
			_SEND(client, READ_SUCCESS, payload);
		};
	}
	
	model fun ChooseReplica()
	{
			if(*) 
				_SEND(replicas[0], READ_REQ_REPLICA, key);
			else
				_SEND(replicas[sizeof(replicas) - 1], READ_REQ_REPLICA, key);
				
	}
	
	state PerformRead {
		entry{ ChooseReplica(); }
		on REP_READ_FAIL do ReturnResult;
		on REP_READ_SUCCESS do ReturnResult;
		
	}
	
	action ReturnResult {
		if(trigger == REP_READ_FAIL)
			readResult = (true, -1);
		else
			readResult = (false, (int)payload);
		
		return;
	}
	action DoWrite {
		pendingWriteReq = payload;
		currSeqNum = currSeqNum + 1;
		i = 0;
		while (i < sizeof(replicas)) {
			_SEND(replicas[i], WRITE_REQ_REPLICA, (seqNum=currSeqNum, key=pendingWriteReq.key, val=pendingWriteReq.val));
			i = i + 1;
		}
		send(timer, StartTimer, 100);
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
			_SEND(replicas[i], GLOBAL_ABORT, currSeqNum);
			i = i + 1;
		}
		_SEND(pendingWriteReq.client, WRITE_FAIL, null);
	}

	state CountVote {
		entry {
			if (i == 0) {
				while (i < sizeof(replicas)) {
					_SEND(replicas[i], GLOBAL_COMMIT, currSeqNum);
					i = i + 1;
				}
				data.update(pendingWriteReq.key, pendingWriteReq.val);
				//invoke Termination(MONITOR_UPDATE, (m = this, key = pendingWriteReq.key, val = pendingWriteReq.val));
				_SEND(pendingWriteReq.client, WRITE_SUCCESS, null);
				send(timer, CancelTimer);
				raise(Unit);
			}
		}
		defer WRITE_REQ, READ_REQ;
		on RESP_REPLICA_COMMIT goto CountVote {
			if (currSeqNum == (int)payload) {
				i = i - 1;
			}
		};
		on RESP_REPLICA_ABORT do HandleAbort;
		on Timeout goto Loop {
			DoGlobalAbort();
		};
		on Unit goto WaitForCancelTimerResponse;
	}

	action HandleAbort {
		if (currSeqNum == (int)payload) {
			DoGlobalAbort();
			send(timer, CancelTimer);
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
\end{Coordinator}

machine Client 
\begin{Client}
    var coordinator: id;
	var mydata : int;
	var counter : int;

	state Init {
		entry {
			coordinator = ((id,int))payload[0];
			mydata = ((id,int))payload[1];
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
			if(counter == 2)
				raise(goEnd);
			_SEND(coordinator, WRITE_REQ, (client=receivePort, key=mydata, val=mydata));
		}
		on WRITE_FAIL goto DoRead
		{
			invoke ReadWrite(MONITOR_WRITE_FAILURE, (m = this, key=mydata, val = mydata))
		};
		on WRITE_SUCCESS goto DoRead
		{
			invoke ReadWrite(MONITOR_WRITE_SUCCESS, (m = this, key=mydata, val = mydata))
		};
		on goEnd goto End;
	}

	state DoRead {
	    entry {
			_SEND(coordinator, READ_REQ, (client=receivePort, key=mydata));
		}
		on READ_FAIL goto DoWrite
		{
			invoke ReadWrite(MONITOR_READ_FAILURE, this);
		};
		on READ_SUCCESS goto DoWrite
		{
			invoke ReadWrite(MONITOR_READ_SUCCESS, (m = this, key = mydata, val = payload));
		};
	}

	state End {  }

\end{Client}

main machine GodMachine 
\begin{GodMachine}
    var coordinator: id;
	var temp_NM : id;

    start state Init {
	    entry {

			//Let me create my own sender/receiver
			sendPort = new SenderMachine((nodemanager = this, param = 3));
            receivePort = new ReceiverMachine((nodemanager = this, param = null));
            send(receivePort, hostM, this);

			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = 1);
			call(_CREATEMACHINE); // create coordinator
			coordinator = createmachine_return;
			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 3, param = (coordinator, 100));
			call(_CREATEMACHINE);// create client machine
			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 3, param = (coordinator, 200));
			call(_CREATEMACHINE);// create client machine
	    }
	}
\end{GodMachine}