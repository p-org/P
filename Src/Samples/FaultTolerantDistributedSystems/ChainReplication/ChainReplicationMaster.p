event CR_Ping  assume 1 :machine;
event CR_Pong assume 1;
event faultCorrected : (newconfig:seq[machine]);
event faultDetected : machine;
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
event becomeHead : machine;
event becomeTail : machine;
event newPredecessor : (pred : machine, master : machine);
event newSuccessor : (succ : machine, master : machine, lastUpdateRec: int, lastAckSent: int);
event updateHeadTail : (head : machine, tail : machine);
event newSuccInfo : (lastUpdateRec : int, lastAckSent : int);

machine ChainReplicationMaster {
	var clients : seq[machine];
	// note that in this seq the first node is the head node and the last node is the tail node
	var servers : seq[machine]; 
	var faultMonitor : machine;
	var head : machine;
	var tail : machine;
	var iter : int;
	var faultyNodeIndex : int;
	var lastUpdateReceivedSucc : int;
	var lastAckSent : int;
	start state Init {
		entry {
			clients = (payload as (clients:seq[machine], servers: seq[machine])).clients;
			servers = (payload as (clients:seq[machine], servers: seq[machine])).servers;
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
	
	fun Return (){
		return;
	}
	
	fun CheckWhichNodeFailed (){
		if(sizeof(servers) == 1)
		{
			assert(false); // all nodes have failed
		}
		else
		{
			if(head == payload)
			{
				raise(headFailed);
			}
			else if(tail == payload)
			{
				
				raise(tailFailed);
			}
			else
			{
				iter = sizeof(servers) - 1;
				while(iter >= 0)
				{
					if(servers[iter] == payload)
					{
						faultyNodeIndex = iter;
					}
					iter = iter - 1;
				}
				raise(serverFailed);
			}
		}
	}
	
	state CorrectHeadFailure {
		entry {
			//make successor the head node
			servers -= (0);
			//Update the monitor
			monitor Update_Propagation_Invariant, monitor_update_servers, (servers = servers, );
			monitor UpdateResponse_QueryResponse_Seq, monitor_update_servers, (servers = servers, );
			
			head = servers[0];
			send head, becomeHead, this;
		}
		on headChanged do UpdateClients;
		on done goto WaitforFault with
		{
			send faultMonitor, faultCorrected, (newconfig = servers, );
		};
	}
	
	state CorrectTailFailure {
		entry {
			
			//make successor the head node
			servers -= (sizeof(servers) - 1);
			//Update the monitor
			monitor Update_Propagation_Invariant, monitor_update_servers, (servers = servers,);
			monitor UpdateResponse_QueryResponse_Seq, monitor_update_servers, (servers = servers,);
			
			
			tail = servers[sizeof(servers) - 1];
			send tail, becomeTail, this;
		}
		on tailChanged do UpdateClients;
		on done goto WaitforFault with 
		{
			send faultMonitor, faultCorrected, (newconfig = servers, );
		};
		
	}
	
	state CorrectServerFailure {
		entry {
				servers -= (faultyNodeIndex);
				//Update the monitor
				monitor Update_Propagation_Invariant, monitor_update_servers, (servers = servers, );
				monitor UpdateResponse_QueryResponse_Seq, monitor_update_servers, (servers = servers, );
		
				push FixSuccessor;
				push FixPredecessor;
				
				raise(done);
			}
			on done goto WaitforFault with
			{
				send faultMonitor, faultCorrected, (newconfig = servers, );
			};
		
	}
	fun SetLastUpdateAndReturn (){
		
		lastUpdateReceivedSucc = payload.lastUpdateRec;
		lastAckSent = payload.lastAckSent;
		pop;
		
	}
	
	state FixSuccessor {
		entry {
			send servers[faultyNodeIndex], newPredecessor, (pred = servers[faultyNodeIndex - 1], master = this);
		}
		on newSuccInfo do SetLastUpdateAndReturn;
	}
	
	state FixPredecessor {
		entry {
			send servers[faultyNodeIndex - 1], newSuccessor, (succ = servers[faultyNodeIndex], master = this, lastUpdateRec = lastUpdateReceivedSucc, lastAckSent = lastAckSent);
		}
		on success do Return;
	}
	
	
	fun UpdateClients (){
		iter = 0;
		while(iter < sizeof(clients)) {
			send clients[iter], updateHeadTail, (head = head, tail = tail);
			iter = iter + 1;
		}
		raise(done);
	}

}

machine ChainReplicationFaultDetection {
	var servers : seq[machine];
	var master : machine;
	var checkNode : int;
	var timerM : machine;
	start state Init{
		entry {
			checkNode = 0;
			//timerM = new Timer(this);  //1
			master = (payload as (master: machine, servers : seq[machine])).master;
			servers = (payload as (master: machine, servers : seq[machine])).servers;
			raise(local);
		}
		on local goto StartMonitoring;
	}
	
	model fun BoundedFailureInjection () {
		if(sizeof(servers) > 1)
		{
			if($)
			{
				send this, timeout;
			}
			else
			{
				send this, CR_Pong;
			}
		}
		else
		{
			send this, CR_Pong;
		}
	}
	
	state StartMonitoring {
		entry {
			//start Timer
			//send(timerM, startTimer); //2
			//send(servers[checkNode], CR_Ping, this); //3
			BoundedFailureInjection ();
		}
		on CR_Pong goto StartMonitoring with
		{
			//stop timer
			//call(CancelTimer); //4
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
			send timerM, cancelTimer;
		}
		on timeout do Return;
		on cancelTimerSuccess do Return;
	}
	
	fun Return (){
		pop;
	}
	
	state HandleFailure {
		ignore CR_Pong;
		entry {
			send master, faultDetected, servers[checkNode];
		}
		on faultCorrected goto StartMonitoring with {
			checkNode = 0;
			servers = payload.newconfig;
		};
	}
}























