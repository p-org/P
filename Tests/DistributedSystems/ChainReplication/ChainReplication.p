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
	action {
		isTail = true;
		//send all the events in sent to both client and the predecessor
		iter = 0;
		while(iter < sizeof(sent))
		{
			
		}
	}
	state WaitForRequest {
		
		/***********fault tolerance****************/
		on CR_Ping do SendPong;
		on becomeTail do becomeTailAction;
		
		
		
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
				
				send(((client:id, key:int))payload.client, responsetoquery, (client = ((client:id, key:int))payload.client, value = keyvalue[((client:id, key:int))payload.key]));
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
			//add it to the history seq (represents the successfully serviced requests)
			history.insert(sizeof(history), nextSeqId);
			//invoke the monitor
			invoke Update_Propagation_Invariant(monitor_history_update, (smId = myId, history = history));
			
			//Add the update request to sent seq
			sent.insert(sizeof(sent), (seqId = nextSeqId, kv = (key = payload.kv.key, value = payload.kv.value)));
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
				sent.insert(sizeof(sent), (seqId = payload.seqId, kv = (key = payload.kv.key, value = payload.kv.value)));
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
		iter = sizeof(sent) - 1;
		while(iter >=0)
		{
			if(req == sent[iter].seqId)
				removeIndex = iter;
			
			iter = iter - 1;
		}
		sent.remove(removeIndex);
	}
	
}






















