/**
We are modelling the chain replication protocol 
**/

event predNode : (pred: id);
event update : (client:id, seqId:int, kv: (key:int, value:int));
event query : (client:id, key:int);
event responsetoquery : (value : int);
event responsetoupdate;
event forwardAck : (seqId:int);
event forwardUpdate : (client:id, seqId:int, kv: (key:int, value:int));
event local;
event done;
machine ChainReplicationServer {
	var keyvalue : map[int, int];
	var history : seq[int];
	var isHead : bool;
	var isTail : bool;
	var succ : id; //NULL for tail
	var pred : id; //NULL for head
	var sent : seq[(seqId:int, kv: (key:int, value:int))];
	var iter : int;
	var removeIndex : int;
	
	
	action InitPred {
		pred = payload.pred;
		raise(local);
	}
	start state Init {
		defer update, query, forwardAck, forwardUpdate;
		entry {
			isHead = (((isHead:bool, isTail:bool, succ:id))payload).isHead;
			isTail = (((isHead:bool, isTail:bool, succ:id))payload).isTail;
			succ = (((isHead:bool, isTail:bool, succ:id))payload).succ;
		}
		on predNode do InitPred;
		on local goto WaitForRequest;
	
	}
	
	state WaitForRequest {
	
		on update goto ProcessUpdate
		{
			assert(isHead);
		};
		
		on query goto WaitForRequest 
		{
			assert(isTail);
			if(((client:id, key:int))payload.key in keyvalue)
			{
				send(((client:id, key:int))payload.client, responsetoquery, (value = keyvalue[((client:id, key:int))payload.key]));
			}
			else
			{
				send(((client:id, key:int))payload.client, responsetoquery, (value = -1));
			}
		};
		on forwardUpdate goto ProcessUpdate
		{
			assert(!isHead);
		};
		on forwardAck goto ProcessAck 
		{
			assert(!isTail);
		};
	}
	state ProcessUpdate {
		entry {
			//Add the update request to sent seq
			sent.insert(0, (seqId = payload.seqId, kv = (key = payload.kv.key, value = payload.kv.value)));
			//Add the update message to keyvalue store
			keyvalue.update(payload.kv.key, payload.kv.value);
			
			if(!isTail)
			{
				//forward the update to the succ
				send(succ, forwardUpdate, (seqId = payload.seqId, client = payload.client, kv = payload.kv));
			}
			else
			{
				//add it to the history seq (represents the successfully serviced requests)
				history.insert(0, payload.seqId);
				//send the response to client
				send(payload.client, responsetoupdate);
				//send ack to the pred
				send(pred, forwardAck, (seqId = payload.seqId));
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
			//add it to the history seq (represents the successfully serviced requests)
			history.insert(0, payload.seqId);
			
			if(!isHead)
			{
				//forward it back to the pred
				send(pred, forwardAck, (seqId = payload.seqId));
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

machine Client {
	var nextSeqId : int;
	var headNode: id;
	var tailNode:id;
	var keyvalue: map[int, int];
	var success: bool;
	var startIn : int;
	start state Init {
		entry {
			nextSeqId = 1;
			new Update_Query_Seq();
			headNode = ((head:id, tail:id, startIn:int))payload.head;
			tailNode = ((head:id, tail:id, startIn:int))payload.tail;
			startIn = ((head:id, tail:id, startIn:int))payload.startIn;
			keyvalue.update(1*startIn,100);
			keyvalue.update(2*startIn,200);
			keyvalue.update(3*startIn,300);
			keyvalue.update(4*startIn,400);
			success = true;
			raise(local);
		}
		on local goto PumpRequests;
	}
	
	state PumpRequests
	{
		entry {
			
			call(Update_Response);
			call(Query_Response);
			nextSeqId = nextSeqId + 1;
			if(nextSeqId >= 3) // only test for now
			{
				call(RandomQuery);
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on local goto PumpRequests;
	}
	action Return {
		if(trigger == responsetoquery)
		{
			invoke Update_Query_Seq(responsetoquery, (value = ((value:int))payload.value));
			
		}
		
		
		return;
	}
	state Update_Response {
		entry {
			send(headNode, update, (client = this, seqId = nextSeqId, kv = (key = nextSeqId * startIn, value = keyvalue[nextSeqId * startIn])));
			invoke Update_Query_Seq(update, (client = this, seqId = nextSeqId, kv = (key = nextSeqId * startIn, value = keyvalue[nextSeqId * startIn])))
		}
		on responsetoupdate do Return;
	}
	
	state Query_Response {
		entry {
			send(tailNode, query, (client = this, key = nextSeqId * startIn));
		}
		on responsetoquery do Return;
	}
	
	model fun QueryNonDet () {
		//can query any item between 1 to nextSeqId
		if(*)
		{
			send(tailNode, query, (client = this, key = (nextSeqId - 1)* startIn));
			success = true;
		}
		else
		{
			send(tailNode, query, (client = this, key = (nextSeqId + 1) * startIn));
			success = false;
		}
	}
	
	action checkReturn {
			if(success)
			{
				assert(keyvalue[(nextSeqId - 1)* startIn] == ((value:int))payload.value);
			}
			else
			{
				assert(((value:int))payload.value == -1);
			}
	}
	state RandomQuery {
		entry {
			QueryNonDet();
		}
		
		on responsetoquery do checkReturn;
		
	}	
}


main machine TheGodMachine {
	var servers : (one:id, two:id, three:id);
	start state Init {
		entry {
			servers.three = new ChainReplicationServer((isHead = false, isTail = true, succ =null));
			servers.two = new ChainReplicationServer((isHead = false, isTail = false, succ =servers.three));
			servers.one = new ChainReplicationServer((isHead = true, isTail = false, succ =servers.two));
			send(servers.three, predNode, (pred = servers.two));
			send(servers.two, predNode, (pred = servers.one));
			send(servers.one, predNode, (pred = null));
			//create the client and start the game
			new Client((head = servers.one, tail = servers.three, startIn = 1));
			new Client((head = servers.one, tail = servers.three, startIn = 20));
			raise(delete);
		}
	}

}























monitor Update_Query_Seq
{
	var kv :(key:int, value:int);
	start state Init {
		entry {
		}
		on update goto UpdateReq;
	}
	
	state UpdateReq {
		entry {
			kv.key = payload.kv.key;
			kv.value = payload.kv.value;
		}
		on responsetoquery goto Init {
			assert(((value:int))payload.value == kv.value);
		};
	}
}
