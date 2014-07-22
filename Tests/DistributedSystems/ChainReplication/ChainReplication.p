/**
We are modelling the chain replication protocol 
**/

event predSucc : (pred: id, succ:id);
event update : (client:id, kv: (key:int, value:int));
event query : (client:id, key:int);
event responsetoquery : (client: id, value : int);
event responsetoupdate;
event backwardAck : (seqId:int);
event forwardUpdate : (seqId:int, client:id, kv: (key:int, value:int));
event local;
event done;


machine ChainReplicationServer {
	var nextSeqId : int;
	var keyvalue : map[int, int];
	var history : seq[int];
	var isHead : bool;
	var isTail : bool;
	var succ : id; //NULL for tail
	var pred : id; //NULL for head
	var sent : seq[(seqId:int, client : id, kv: (key:int, value:int))];
	var iter : int;
	var tempIndex : int;
	var removeIndex : int;
	var myId : int;
	
	action InitPred {
		pred = payload.pred;
		succ = payload.succ;
		raise(local);
	}
	
	action SendPong {
		send((id)payload, CR_Pong);
	}
	
	start state Init {
		defer update, query, backwardAck, forwardUpdate, CR_Ping;
		entry {
			isHead = (((isHead:bool, isTail:bool, smId:int))payload).isHead;
			isTail = (((isHead:bool, isTail:bool, smId:int))payload).isTail;
			myId = (((isHead:bool, isTail:bool, smId:int))payload).smId;
			nextSeqId = 0;
		}
		on predSucc do InitPred;
		on local goto WaitForRequest;
	
	}
	action becomeTailAction {
		isTail = true;
		succ = this;
		//send all the events in sent to both client and the predecessor
		iter = 0;
		while(iter < sizeof(sent))
		{
			//invoke the monitor
			invoke UpdateResponse_QueryResponse_Seq(monitor_reponsetoupdate, sent[iter].kv);
			//send the response to client
			send(sent[iter].client, responsetoupdate);
			
			// the backward ack to the pred
			send(pred, backwardAck, (seqId = sent[iter].seqId));
			iter = iter + 1;
		}
		
		send((id)payload, tailChanged);
		
	}
	
	action becomeHeadAction {
		isHead = true;
		pred = this;
		
		send((id)payload, headChanged);
		
	}
	
	action updatePredecessor {
		pred = payload.pred;
		if(sizeof(history) > 0)
		{
			if(sizeof(sent) > 0)
				send(payload.master, newSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = sent[0].seqId - 1));
			else
				send(payload.master, newSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = history[sizeof(history) - 1]));
		}
		
	}
	
	action updateSuccessor {
		tempIndex = 1000000; //some large number
		succ = payload.succ;
		if(sizeof(sent) > 0)
		{
			//send the remaining history
			iter = 0;
			while(iter < sizeof(sent))
			{
				if(sent[iter].seqId > payload.lastUpdateRec)
					send(succ, forwardUpdate, sent[iter]);
				
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
			while(iter <= tempIndex)
			{
				send(pred, backwardAck, sent[0].seqId);
				sent.remove(0);
				iter = iter + 1;
			}
		}
		send(payload.master, success);
	}
	
	state WaitForRequest {
		
		/***********fault tolerance****************/
		on CR_Ping do SendPong;
		on becomeTail do becomeTailAction;
		on becomeHead do becomeHeadAction;
		on newPredecessor do updatePredecessor;
		on newSuccessor do updateSuccessor;
		/***************************/
		
		on update goto ProcessUpdate
		{
			nextSeqId = nextSeqId + 1;
			assert(isHead);
		};
		
		on query goto WaitForRequest 
		{
			assert(isTail);
			if(((client:id, key:int))payload.key in keyvalue)
			{
				//Invoke the monitor
				invoke UpdateResponse_QueryResponse_Seq(monitor_responsetoquery, (key = ((client:id, key:int))payload.key, value = keyvalue[((client:id, key:int))payload.key]));
				
				send(((client:id, key:int))payload.client, responsetoquery, (client = ((client:id, key:int))payload.client,value = keyvalue[((client:id, key:int))payload.key]));
			}
			else
			{
				send(((client:id, key:int))payload.client, responsetoquery, (client = ((client:id, key:int))payload.client, value = -1));
			}
		};
		on forwardUpdate goto ProcessfwdUpdate;
		on backwardAck goto ProcessAck;
	}
	state ProcessUpdate {
		entry {	
			//Add the update message to keyvalue store
			keyvalue.update(payload.kv.key, payload.kv.value);
		
			//add it to the history seq (represents the successfully serviced requests)
			history.insert(sizeof(history), nextSeqId);
			//invoke the monitor
			invoke Update_Propagation_Invariant(monitor_history_update, (smId = myId, history = history));
			
			//Add the update request to sent seq
			sent.insert(sizeof(sent), (seqId = nextSeqId, client = payload.client, kv = (key = payload.kv.key, value = payload.kv.value)));
			//call the monitor
			invoke Update_Propagation_Invariant(monitor_sent_update, (smId = myId, sent = sent));
			//forward the update to the succ
			send(succ, forwardUpdate, (seqId = nextSeqId, client = payload.client, kv = payload.kv));
	
			raise(local);
		}
		on local goto WaitForRequest;
	}
	state ProcessfwdUpdate {
		entry {
			//update my nextSeqId
			nextSeqId = payload.seqId;
			
			//Add the update message to keyvalue store
			keyvalue.update(payload.kv.key, payload.kv.value);
			
			if(!isTail)
			{
				//add it to the history seq (represents the successfully serviced requests)
				history.insert(sizeof(history), payload.seqId);
				//invoke the monitor
				invoke Update_Propagation_Invariant(monitor_history_update, (smId = myId, history = history));
				//Add the update request to sent seq
				sent.insert(sizeof(sent), (seqId = payload.seqId, client = payload.client, kv = (key = payload.kv.key, value = payload.kv.value)));
				//call the monitor
				invoke Update_Propagation_Invariant(monitor_sent_update, (smId = myId, sent = sent));
				//forward the update to the succ
				send(succ, forwardUpdate, (seqId = payload.seqId, client = payload.client, kv = payload.kv));
			}
			else
			{
				if(!isHead)
				{
					//add it to the history seq (represents the successfully serviced requests)
					history.insert(sizeof(history), payload.seqId);
				}
				
				//invoke the monitor
				invoke UpdateResponse_QueryResponse_Seq(monitor_reponsetoupdate, payload.kv);
				
				//send the response to client
				send(payload.client, responsetoupdate);
				
				//send ack to the pred
				send(pred, backwardAck, (seqId = payload.seqId));

			}
			raise(local);
		}
		on local goto WaitForRequest;
	}
	
	state ProcessAck
	{
		entry {
			//remove the request from sent seq.
			RemoveItemFromSent(payload.seqId);
			
			if(!isHead)
			{
				//forward it back to the pred
				send(pred, backwardAck, (seqId = payload.seqId));
			}
			raise(local);
		}
		on local goto WaitForRequest;
	}
	
	fun RemoveItemFromSent(req : int) {
		removeIndex = -1;
		iter = sizeof(sent) - 1;
		while(iter >=0)
		{
			if(req == sent[iter].seqId)
				removeIndex = iter;
			
			iter = iter - 1;
		}
		
		if(removeIndex != -1)
		{
			sent.remove(removeIndex);
		}
	}
	
}






















