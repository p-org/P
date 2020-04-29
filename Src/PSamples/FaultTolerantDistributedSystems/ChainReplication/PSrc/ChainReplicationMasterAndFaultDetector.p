machine ChainReplicationMasterMachine
sends eBecomeHead, eBecomeTail, eFaultCorrected, eNewPredecessor, eNewSuccessor, eMonitorUpdateNodes;
{
	var client : SMRClientInterface;
	// note that in this seq the first node is the head node and the last node is the tail node
	var nodes : seq[ChainReplicationNodeInterface]; 
	var faultMonitor : ChainReplicationFaultDetectorInterface;
	var head : ChainReplicationNodeInterface;
	var tail : ChainReplicationNodeInterface;
	var faultyNodeIndex : int;

	start state Init {
		entry (payload: (client: SMRClientInterface, nodes: seq[ChainReplicationNodeInterface])){
			client = payload.client;
			nodes = payload.nodes;
			faultMonitor = new ChainReplicationFaultDetectorInterface((master = this to ChainReplicationMasterInterface, nodes = nodes));
			head = nodes[0];
			tail = nodes[sizeof(nodes) - 1];
			announce eMonitorUpdateNodes, (nodes = nodes,);
			goto WaitforFault;
		}
	}
	
	state WaitforFault {
		on eFaultDetected do (payload: ChainReplicationNodeInterface) {
			var iter : int;
			if(sizeof(nodes) == 1)
			{
				//assert(false); // all nodes have failed
			}
			else
			{
				if(head == payload)
				{
					goto CorrectHeadFailure;
				}
				else if(tail == payload)
				{
					goto CorrectTailFailure;
				}
				else
				{
					iter = sizeof(nodes) - 1;
					while(iter >= 0)
					{
						if(nodes[iter] == payload)
						{
							faultyNodeIndex = iter;
						}
						iter = iter - 1;
					}
					goto CorrectServerFailure;
				}
			}
		}
	}

	
	state CorrectHeadFailure {
		entry {
			//make successor the head node
			nodes -= (0);
			//Update the monitor
			//monitor Update_Propagation_Invariant, monitor_update_servers, (servers = servers, );
			//monitor UpdateResponse_QueryResponse_Seq, monitor_update_servers, (servers = servers, );
			
			head = nodes[0];
			send head, eBecomeHead, this to ChainReplicationMasterInterface;
		}
		on eHeadChanged goto WaitforFault with
		{
			send faultMonitor, eFaultCorrected, (newconfig = nodes, );
			announce eMonitorUpdateNodes, (nodes = nodes,);
		}
	}
	
	state CorrectTailFailure {
		entry {
			
			//make successor the head node
			nodes -= (sizeof(nodes) - 1);
			//Update the monitor
			//monitor Update_Propagation_Invariant, monitor_update_servers, (servers = servers,);
			//monitor UpdateResponse_QueryResponse_Seq, monitor_update_servers, (servers = servers,);
			
			
			tail = nodes[sizeof(nodes) - 1];
			send tail, eBecomeTail, this to ChainReplicationMasterInterface;
		}
		on eTailChanged goto WaitforFault with 
		{
			send faultMonitor, eFaultCorrected, (newconfig = nodes, );
			announce eMonitorUpdateNodes, (nodes = nodes,);
		}
	}
	
	state CorrectServerFailure {
		entry {
				var lastUpdateReceivedSucc : int;
				var lastAckSent : int;
				nodes -= (faultyNodeIndex);
				//Update the monitor
				//monitor Update_Propagation_Invariant, monitor_update_servers, (servers = servers, );
				//monitor UpdateResponse_QueryResponse_Seq, monitor_update_servers, (servers = servers, );
		
				send nodes[faultyNodeIndex], eNewPredecessor, (pred = nodes[faultyNodeIndex - 1], master = this to ChainReplicationMasterInterface);
				receive {
					case eNewSuccInfo: (payload: (lastUpdateRec : int, lastAckSent : int)) {
						lastUpdateReceivedSucc = payload.lastUpdateRec;
						lastAckSent = payload.lastAckSent;
					}
				}
				
				send nodes[faultyNodeIndex - 1], eNewSuccessor, (succ = nodes[faultyNodeIndex], master = this to ChainReplicationMasterInterface, lastUpdateRec = lastUpdateReceivedSucc, lastAckSent = lastAckSent);
				
				receive {
					case eSuccess: {}
				}
				
				send faultMonitor, eFaultCorrected, (newconfig = nodes, );
				announce eMonitorUpdateNodes, (nodes = nodes,);

				goto WaitforFault;
			}
	}
}


machine ChainReplicationFaultDetectionMachine
sends eCRPing, eFaultDetected,  eStartTimer, eCancelTimer, halt;
{
	var nodes : seq[ChainReplicationNodeInterface]; 
	var master : ChainReplicationMasterInterface;
	var checkNode : int;
	var timer : TimerPtr;
	start state Init{
		entry (payload: (master: ChainReplicationMasterInterface, nodes: seq[ChainReplicationNodeInterface])){
			checkNode = 0;
			//create timer
			timer = CreateTimer(this to ITimerClient);
			master = payload.master;
			nodes = payload.nodes;
			goto StartMonitoring;
		}
	}
	
	state StartMonitoring {
		entry {
			//start timer 
			StartTimer(timer, 100);
			send nodes[checkNode], eCRPing, this to ChainReplicationFaultDetectorInterface;
		}
		on eCRPong goto StartMonitoring with
		{
			//stop timer
			CancelTimer(timer);
			checkNode = checkNode + 1;
			if(checkNode == sizeof(nodes))
			{
				checkNode = 0;
			}
		}
		on eTimeOut goto HandleFailure;
	}
	
	state HandleFailure {
		ignore eCRPong;
		entry {
			//send nodes[checkNode], halt;
			//send master, eFaultDetected, nodes[checkNode];
		}
		on eFaultCorrected goto StartMonitoring with (payload: (newconfig: seq[ChainReplicationNodeInterface])) {
			checkNode = 0;
			nodes = payload.newconfig;
		}
	}
	
}























