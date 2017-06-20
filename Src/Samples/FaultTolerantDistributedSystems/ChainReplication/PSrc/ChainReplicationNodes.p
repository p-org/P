/******************************************************************
* Description:
* This file presents the ChainReplicationNodeMachine that implements the Chain Replication protocol
*******************************************************************/

machine ChainReplicationNodeMachine : ChainReplicationNodeInterface, SMRServerInterface
sends eBackwardAck, eForwardUpdate, eCRPong, eNewSuccInfo, eSMRReplicatedLeader, eSuccess, eTailChanged, eHeadChanged;
 {
	var nextSeqId : int;
	var repSM : SMRReplicatedMachineInterface;
	var history : seq[int];
	var nodeT : NodeType;
	var succ : ChainReplicationNodeInterface; 
	var pred : ChainReplicationNodeInterface; 
	var sent : seq[(seqId: int, smrop: SMROperationType)];
	var client : SMRClientInterface;
	var FT : FaultTolerance;

	start state Init {
		defer eBackwardAck, eForwardUpdate, eCRPing;
		entry (payload: (client: SMRClientInterface, reorder: bool, isRoot : bool, ft : FaultTolerance, val: data)){
			var repSMConstArg : data;
			
			client = payload.client;
			FT = payload.ft;

			

			if(payload.isRoot)
			{
				
				nodeT = HEAD;
				repSMConstArg = payload.val;
				//create the rest of nodes
				SetUp(repSMConstArg);
			}
			else
			{
				nodeT = (payload.val as (NodeType, data)).0;
				repSMConstArg = (payload.val as (NodeType, data)).1;
			}
			
			//create the replicated node 
			repSM = new SMRReplicatedMachineInterface((client = payload.client, val = repSMConstArg));

			if(nodeT == TAIL)
			{
				//tell the replicated machine that it is the leader now
				send repSM, eSMRReplicatedLeader;
			}
			nextSeqId = 0;
			receive {
				case ePredSucc: (payload1: (pred: ChainReplicationNodeInterface, succ: ChainReplicationNodeInterface)) {
					pred = payload1.pred;
					succ = payload1.pred;
				}
			}
			raise local;
		}
		on local push WaitForRequest;

		on eSMROperation do (payload: SMROperationType){
			//all operations are considered as update operations.
			raise eUpdate, payload;
		}
	
	}

	fun SetUp(repSMConstArg: data) {
		var numOfNodes : int;
		var nodes : seq[ChainReplicationNodeInterface];
		var tempNode: ChainReplicationNodeInterface;
		var index : int;
		if(FT == FT1)
			numOfNodes = 2;
		else if (FT == FT2)
			numOfNodes = 3;
		else
			assert(false);

		//create all the nodes
		//first add the current node itself as head
		nodes += (0, this as ChainReplicationNodeInterface);
		
		//create internal nodes
		index = 0;
		while(index < numOfNodes - 2)
		{
			tempNode = new ChainReplicationNodeInterface((client = client, reorder = false, isRoot = false, ft = FT, val = (INTERNAL, repSMConstArg)));
			nodes += (sizeof(nodes), tempNode);
			index = index + 1;
		}

		//create tail
		tempNode = new ChainReplicationNodeInterface((client = client, reorder = false, isRoot = false, ft = FT, val = (TAIL, repSMConstArg)));
		nodes += (sizeof(nodes), tempNode);
		
		//send pred and succ to all
		//head
		send nodes[0], ePredSucc, (pred = nodes[0], succ = nodes[1]);
		//tail
		send nodes[sizeof(nodes)], ePredSucc, (pred = nodes[sizeof(nodes)-1], succ = nodes[sizeof(nodes)]);
		//internal nodes
		index = 0;
		while(index < numOfNodes - 2)
		{
			send nodes[index], ePredSucc, (pred = nodes[index-1], succ = nodes[index + 1]);
			index = index + 1;
		}

		//create the master node.
		new ChainReplicationMasterInterface((client = client, nodes = nodes));
	}

	fun UpdateSuccessor(payload: (succ : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface, lastUpdateRec: int, lastAckSent: int))
	{
		var tempIndex : int;
		var iter: int;
		tempIndex = -1;
		succ = payload.succ;
		if(sizeof(sent) > 0)
		{
			//send the remaining history
			iter = 0;
			while(iter < sizeof(sent))
			{
				if(sent[iter].seqId > payload.lastUpdateRec)
					send succ, eForwardUpdate, (msg = sent[iter], pred = this as ChainReplicationNodeInterface);
				
				iter = iter + 1;
			}
			
			//send the backward acks
			iter = sizeof(sent) - 1;
			while(iter >= 0)
			{
				if(sent[iter].seqId == payload.lastAckSent)
					tempIndex = iter;
				
				iter = iter - 1;
			}
			
			iter = 0;
			while(iter < tempIndex)
			{
				send pred, eBackwardAck, (seqId = sent[0].seqId, );
				sent -= (0);
				iter = iter + 1;
			}
		}
		
		send payload.master, eSuccess;
	}
	
	state WaitForRequest {
		on eCRPing do (payload: ChainReplicationFaultDetectorInterface){
			send payload, eCRPong;
		}

		on eBecomeTail do (payload: ChainReplicationMasterInterface){
			var iter : int;
			nodeT = TAIL;
			succ = this as ChainReplicationNodeInterface;
			//send all the events in sent to both client and the predecessor
			iter = 0;
			while(iter < sizeof(sent))
			{
				//invoke the monitor
				//monitor UpdateResponse_QueryResponse_Seq, monitor_reponsetoupdate, (tail = this, key = sent[iter].kv.key, value = sent[iter].kv.value);
				
				//invoke livenessUpdatetoResponse(monitor_responseLiveness, (reqId = sent[iter].seqId, ));
				
				//tell the replicated machine that it is the leader now
				send repSM, eSMRReplicatedLeader;

				
				// the backward ack to the pred
				send pred, eBackwardAck, (seqId = sent[iter].seqId, );
				iter = iter + 1;
			}
			send payload, eTailChanged;
		}

		on eBecomeHead do (payload: ChainReplicationMasterInterface){
			nodeT = HEAD;
			pred = this as ChainReplicationNodeInterface;
			send payload, eHeadChanged;
		}

		on eNewPredecessor do (payload: (pred : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface)){
			pred = payload.pred;
			if(sizeof(history) > 0)
			{
				if(sizeof(sent) > 0)
					send payload.master, eNewSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = sent[0].seqId);
				else
					send payload.master, eNewSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = history[sizeof(history) - 1]);
			}
		}
		on eNewSuccessor do (payload: (succ : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface, lastUpdateRec: int, lastAckSent: int)) {
			UpdateSuccessor(payload);
		}
	
		
		on eUpdate goto ProcessUpdate with 
		{
			nextSeqId = nextSeqId + 1;
			assert(nodeT == HEAD);
			//new livenessUpdatetoResponse(nextSeqId);
			//invoke livenessUpdatetoResponse(monitor_updateLiveness, (reqId = nextSeqId));
		}
		
		on eForwardUpdate goto ProcessFwdUpdate;
		on eBackwardAck goto ProcessBackwardAck;
	}
	
	state ProcessUpdate {
		entry (payload: SMROperationType){	
			
			//Send the operation to the replicated SM
			SendSMRRepMachineOperation(repSM, payload);

			//add it to the history seq (represents the successfully serviced requests)
			history += (sizeof(history), nextSeqId);
			//invoke the monitor
			//monitor Update_Propagation_Invariant, monitor_history_update, (smid = this, history = history);
			
			//Add the update request to sent seq
			sent += (sizeof(sent), (seqId = nextSeqId, smrop = payload));
			//call the monitor
			//monitor Update_Propagation_Invariant, monitor_sent_update, (smid = this, sent = sent);
			//forward the update to the succ
			send succ, eForwardUpdate, (msg = (seqId = nextSeqId, smrop = payload), pred = this as ChainReplicationNodeInterface);
	
			raise(local);
		}
		on local goto WaitForRequest;
	}

	state ProcessFwdUpdate {
		entry (payload: (msg: (seqId: int, smrop: SMROperationType), pred: ChainReplicationNodeInterface)){
			if(payload.pred == pred)
			{
				//update my nextSeqId
				nextSeqId = payload.msg.seqId;
				
				//Send the operation to the replicated SM
				SendSMRRepMachineOperation(repSM, payload.msg.smrop);

				if(nodeT != TAIL)
				{
					//add it to the history seq (represents the successfully serviced requests)
					history += (sizeof(history), payload.msg.seqId);
					//invoke the monitor
					//annouce Update_Propagation_Invariant, monitor_history_update, (smid = this, history = history);
					//Add the update request to sent seq
					sent += (sizeof(sent), payload.msg);
					//call the monitor
					//annouce Update_Propagation_Invariant, monitor_sent_update, (smid = this, sent = sent);
					//forward the update to the succ
					send succ, eForwardUpdate, (msg = payload.msg, pred = this as ChainReplicationNodeInterface);
				}
				else
				{
					if(nodeT != HEAD)
					{
						//add it to the history seq (represents the successfully serviced requests)
						history += (sizeof(history), payload.msg.seqId);
					}
					
					//invoke the monitor
					//monitor UpdateResponse_QueryResponse_Seq, monitor_reponsetoupdate, (tail = this, key = payload.mess.kv.key, value = payload.mess.kv.value);
					
					//invoke livenessUpdatetoResponse(monitor_responseLiveness, (reqId =payload.mess.seqId));
					
					//send ack to the pred
					send pred, eBackwardAck, (seqId = payload.msg.seqId, );

				}
			}
			goto WaitForRequest;
		}
	}
	
	state ProcessBackwardAck
	{
		entry (payload: (seqId: int)){
			//remove the request from sent seq.
			RemoveItemFromSent(payload.seqId);
			
			if(nodeT != HEAD)
			{
				//forward it back to the pred
				send pred, eBackwardAck, (seqId = payload.seqId,);
			}
			goto WaitForRequest;
		}
	}
	
	fun RemoveItemFromSent(req : int) {
		var iter : int;
		iter = sizeof(sent) - 1;
		while(iter >= 0)
		{
			if(req == sent[iter].seqId)
			{
				sent -= iter;
				return;
			}
			iter = iter - 1;
		}
	}
	
}



















