/******************************************************************
* Description:
* This file presents the ChainReplicationNodeMachine that implements the Chain Replication protocol
*******************************************************************/

machine ChainReplicationNodeMachine : ChainReplicationNodeInterface, SMRServerInterface
sends eBackwardAck, eForwardUpdate, eCRPong;
 {
	var nextSeqId : int;
	var repSM : SMRReplicatedMachineInterface;
	var history : seq[int];
	var nodeT : NodeType;
	var succ : ChainReplicationNodeInterface; 
	var pred : ChainReplicationNodeInterface; 
	var sent : seq[(seqId: int, smrop: SMROperationType)];
	

	start state Init {
		defer eBackwardAck, eForwardUpdate, eCRPing;
		entry (payload: (nType: NodeType)){
			nodeT = payload.nType;
			nextSeqId = 0;
			receive {
				case ePredSucc: (payload: (pred: ChainReplicationNodeInterface, succ: ChainReplicationNodeInterface)) {
					pred = payload.pred;
					succ = payload.pred;
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

	fun updateSuccessor() {
		tempIndex = -1; //some large number
		succ = payload.succ;
		if(sizeof(sent) > 0)
		{
			//send the remaining history
			iter = 0;
			while(iter < sizeof(sent))
			{
				if(sent[iter].seqId > payload.lastUpdateRec)
					send succ, forwardUpdate, (mess = sent[iter], pred = this);
				
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
				send pred, backwardAck, (seqId = sent[0].seqId, );
				sent -= (0);
				iter = iter + 1;
			}
		}
		
		send payload.master, success;
	}
	
	state WaitForRequest {
		on eCRPing do (payload: ChainReplicationFaultDetectorInterface){
			send payload, eCRPong;
		}

		on becomeTail do (payload: ChainReplicationMasterInterface){
			nodeT = TAIL;
			succ = this;
			//send all the events in sent to both client and the predecessor
			iter = 0;
			while(iter < sizeof(sent))
			{
				//invoke the monitor
				//monitor UpdateResponse_QueryResponse_Seq, monitor_reponsetoupdate, (tail = this, key = sent[iter].kv.key, value = sent[iter].kv.value);
				
				//invoke livenessUpdatetoResponse(monitor_responseLiveness, (reqId = sent[iter].seqId, ));
				
				
				//TODO: Tell the replicated machine that it has become leader.
				//send the response to client
				//send sent[iter].client, responsetoupdate;
				
				// the backward ack to the pred
				send pred, eBackwardAck, (seqId = sent[iter].seqId, );
				iter = iter + 1;
			}
			send payload, tailChanged;
		}

		on becomeHead do (payload: ChainReplicationMasterInterface){
			nodeT = HEAD;
			pred = this;
			send payload, headChanged;
		}

		on newPredecessor do {
			pred = payload.pred;
			if(sizeof(history) > 0)
			{
				if(sizeof(sent) > 0)
					send payload.master, newSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = sent[0].seqId);
				else
					send payload.master, newSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = history[sizeof(history) - 1]);
			}
		}
		on newSuccessor do updateSuccessor;
	
		
		on update goto ProcessUpdate with 
		{
			nextSeqId = nextSeqId + 1;
			assert(isHead);
			//new livenessUpdatetoResponse(nextSeqId);
			//invoke livenessUpdatetoResponse(monitor_updateLiveness, (reqId = nextSeqId));
		};
		
		on query goto WaitForRequest with
		{
			assert(isTail);
			if(payload.key in keyvalue)
			{
				//Invoke the monitor
				monitor UpdateResponse_QueryResponse_Seq, monitor_responsetoquery, (tail = this, key = payload.key, value = keyvalue[payload.key]);
				
				send payload.client, responsetoquery, (client = payload.client,value = keyvalue[payload.key]);
			}
			else
			{
				send payload.client, responsetoquery, (client = payload.client, value = -1);
			}
		};
		on eForwardUpdate goto ProcessFwdUpdate;
		on eBackwardAck goto ProcessBackwardAck;
	}

	state ProcessUpdate {
		entry {	
			//Add the update message to keyvalue store
			keyvalue[payload.kv.key] = payload.kv.value;
		
			//add it to the history seq (represents the successfully serviced requests)
			history += (sizeof(history), nextSeqId);
			//invoke the monitor
			monitor Update_Propagation_Invariant, monitor_history_update, (smid = this, history = history);
			
			//Add the update request to sent seq
			sent += (sizeof(sent), (seqId = nextSeqId, client = payload.client, kv = (key = payload.kv.key, value = payload.kv.value)));
			//call the monitor
			monitor Update_Propagation_Invariant, monitor_sent_update, (smid = this, sent = sent);
			//forward the update to the succ
			send succ, forwardUpdate, (mess = (seqId = nextSeqId, client = payload.client, kv = payload.kv), pred = this);
	
			raise(local);
		}
		on local goto WaitForRequest;
	}

	state ProcessFwdUpdate {
		entry (payload: (msg: (seqId: int, smrop: SMROperationType), pred: ChainReplicationNodeInterface)){
			if(payload.pred == pred)
			{
				//update my nextSeqId
				nextSeqId = payload.mess.seqId;
				
				//Send the operation to the replicated SM
				SendSMRRepMachineOperation(repSM, payload.msg.smrop);

				if(!isTail)
				{
					//add it to the history seq (represents the successfully serviced requests)
					history += (sizeof(history), payload.mess.seqId);
					//invoke the monitor
					annouce Update_Propagation_Invariant, monitor_history_update, (smid = this, history = history);
					//Add the update request to sent seq
					sent += (sizeof(sent), (seqId = payload.mess.seqId, client = payload.mess.client, kv = (key = payload.mess.kv.key, value = payload.mess.kv.value)));
					//call the monitor
					annouce Update_Propagation_Invariant, monitor_sent_update, (smid = this, sent = sent);
					//forward the update to the succ
					send succ, forwardUpdate, (mess = (seqId = payload.mess.seqId, client = payload.mess.client, kv = payload.mess.kv), pred = this);
				}
				else
				{
					if(!isHead)
					{
						//add it to the history seq (represents the successfully serviced requests)
						history += (sizeof(history), payload.mess.seqId);
					}
					
					//invoke the monitor
					monitor UpdateResponse_QueryResponse_Seq, monitor_reponsetoupdate, (tail = this, key = payload.mess.kv.key, value = payload.mess.kv.value);
					
					//invoke livenessUpdatetoResponse(monitor_responseLiveness, (reqId =payload.mess.seqId));
					
					
					//send the response to client
					send payload.mess.client, responsetoupdate;
					
					//send ack to the pred
					send pred, backwardAck, (seqId = payload.mess.seqId, );

				}
			}
			raise(local);
		}
		on local goto WaitForRequest;
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
				if(removeIndex != -1)
				{
					sent -= iter;
					return;
				}
			}
			iter = iter - 1;
		}
	}
}



















