event CR_Ping:id assume 1;
event CR_Pong assume 1;
event faultCorrected : (newconfig:seq[id]);
event faultDetected : id;
event startTimer;
event cancelTimer;
event cancelTimerSuccess;
event serverFailed;
event headFailed;
event headChanged;
event tailChanged;
event tailFailed;
event success;
event timeout;
event becomeHead;
event becomeTail;
event newPredecessor : id;
event newSuccessor : id;
event updateHeadTail : (head : id, tail : id);

machine ChainReplicationMaster {
	var clients : seq[id];
	var servers : seq[id]; // note that in this seq the first node is the head node and the last node is the tail node
	var faultMonitor : id;
	var head : id;
	var tail : id;
	var iter : int;
	var faultyNodeIndex : int;
	start state Init {
		entry {
			clients = ((clients:seq[id], servers: seq[id]))payload.clients;
			servers = ((clients:seq[id], servers: seq[id]))payload.servers;
			faultMonitor = new ChainReplicationFaultDetection((master = this, servers = servers));
			
			head = servers[0];
			tail = servers[sizeof(servers) - 1];
			raise(local);
		}
		on local goto WaitforFault;
	}
	
	state WaitforFault {
		entry {
			
		}
		on faultDetected do CheckWhichNodeFailed;
		on headFailed goto CorrectHeadFailure;
		on tailFailed goto CorrectTailFailure;
		on serverFailed goto CorrectServerFailure;
	}
	
	action Return {
		return;
	}
	
	action CheckWhichNodeFailed {
		if(sizeof(servers) == 1)
		{
			assert(false); // all nodes have failed
		}
		else
		{
			if(head == (id)payload)
			{
				raise(headFailed);
			}
			else if(tail == (id)payload)
			{
				raise(tailFailed);
			}
			else
			{
				iter = sizeof(servers) - 1;
				while(iter >= 0)
				{
					if(servers[iter] == (id)payload)
					{
						faultyNodeIndex = iter;
					}
					iter = iter + 1;
				}
				raise(serverFailed);
			}
		}
	}
	
	state CorrectHeadFailure {
		entry {
			//make successor the head node
			servers.remove(0);
			head = servers[0];
			send(head, becomeHead);
		}
		on headChanged do UpdateClients;
		on done goto WaitforFault
		{
			send(faultMonitor, faultCorrected, (newconfig = servers));
		};
	}
	
	state CorrectTailFailure {
		entry {
			
			//make successor the head node
			servers.remove(sizeof(servers) - 1);
			tail = servers[sizeof(servers) - 1];
			send(tail, becomeTail);
		}
		on tailChanged do UpdateClients;
		on done goto WaitforFault
		{
			send(faultMonitor, faultCorrected, (newconfig = servers));
		};
		
	}
	
	state CorrectServerFailure {
		entry {
				call(FixSuccessor);
				call(FixPredecessor);
				servers.remove(faultyNodeIndex);
				raise(done);
			}
			on done goto WaitforFault
			{
				send(faultMonitor, faultCorrected, (newconfig = servers));
			};
		
	}
	
	state FixSuccessor {
		entry {
			send(servers[faultyNodeIndex + 1], newPredecessor, servers[faultyNodeIndex - 1]);
		}
		on success do Return;
	}
	
	state FixPredecessor {
		entry {
			send(servers[faultyNodeIndex - 1], newSuccessor, servers[faultyNodeIndex + 1]);
		}
		on success do Return;
	}
	
	
	action UpdateClients {
		iter = 0;
		while(iter < sizeof(clients)) {
			send(clients[iter], updateHeadTail, (head = head, tail = tail));
			iter = iter + 1;
		}
		raise(done);
	}

}

machine ChainReplicationFaultDetection {
	var servers : seq[id];
	var master : id;
	var checkNode : int;
	var timerM : mid;
	start state Init{
		entry {
			checkNode = 0;
			timerM = new Timer(this);
			master = ((master: id, servers : seq[id]))payload.master;
			servers = ((master: id, servers : seq[id]))payload.servers;
			raise(local);
		}
		on local goto StartMonitoring;
	}
	
	state StartMonitoring {
		entry {
			//start Timer
			send(timerM, startTimer);
			send(servers[checkNode], CR_Ping, this);
		}
		on CR_Pong goto StartMonitoring
		{
			//stop timer
			call(CancelTimer);
			checkNode = checkNode + 1;
			if(checkNode == sizeof(servers))
			{
				checkNode = 0;
			}
		};
		on timeout goto HandleFailure;
	}
	
	state CancelTimer {
		entry {
			send(timerM, cancelTimer);
		}
		on timeout do Return;
		on cancelTimerSuccess do Return;
	}
	
	action Return {
		return;
	}
	
	state HandleFailure {
		ignore CR_Pong;
		entry {
			send(master, faultDetected, servers[checkNode]);
		}
		on faultCorrected goto StartMonitoring {
			checkNode = 0;
			servers = ((newconfig:seq[id]))payload.newconfig;
		};
	}
}

model machine Timer {
	var target: id;
	start state Init {
		entry {
			target = (id)payload;
			raise(local);
		}
		on local goto Loop;
	}

	state Loop {
		ignore cancelTimer;
		on startTimer goto TimerStarted;
	}

	state TimerStarted {
		entry {
			if (*) {
				send(target, timeout);
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop
		{
			send(target, cancelTimerSuccess);
		};
	}
}






















