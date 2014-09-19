/**
We are modelling the chain replication protocol 
**/

event predNode : (pred: id);
event update : (client:id, kv: (key:int, value:int));
event query : (client:id, key:int);
event responsetoquery : (client: id, value : int);
event responsetoupdate;
event forwardAck : (seqId:int);
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
	var sent : seq[(seqId:int, kv: (key:int, value:int))];
	var iter : int;
	var removeIndex : int;
	var myId : int;
	
	action InitPred {
		pred = payload.pred;
		raise(local);
	}
	start state Init {
		defer update, query, forwardAck, forwardUpdate;
		entry {
			isHead = (((isHead:bool, isTail:bool, succ:id, smId:int))payload).isHead;
			isTail = (((isHead:bool, isTail:bool, succ:id, smId:int))payload).isTail;
			succ = (((isHead:bool, isTail:bool, succ:id, smId:int))payload).succ;
			myId = (((isHead:bool, isTail:bool, succ:id, smId:int))payload).smId;
			nextSeqId = 1;
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
				send(((client:id, key:int))payload.client, responsetoquery, (client = ((client:id, key:int))payload.client, value = keyvalue[((client:id, key:int))payload.key]));
			}
			else
			{
				send(((client:id, key:int))payload.client, responsetoquery, (client = ((client:id, key:int))payload.client, value = -1));
			}
		};
		on forwardUpdate goto ProcessfwdUpdate
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
		
			nextSeqId = nextSeqId + 1;
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
				//add it to the history seq (represents the successfully serviced requests)
				history.insert(sizeof(history), payload.seqId);
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
	var next : int;
	var headNode: id;
	var tailNode:id;
	var keyvalue: map[int, int];
	var success: bool;
	var startIn : int;
	start state Init {
		entry {
			next = 1;
			new Update_Query_Seq(this);
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
			if(next >= 2) // only test for now
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
			invoke Update_Query_Seq(responsetoquery, payload);
			
		}
		return;
	}
	
	state Update_Response {
		entry {
			send(headNode, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn])));
			invoke Update_Query_Seq(update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn])))
		}
		on responsetoupdate do Return;
	}
	
	state Query_Response {
		entry {
			send(tailNode, query, (client = this, key = next * startIn));
		}
		on responsetoquery do Return;
	}
	
	model fun QueryNonDet () {
		//can query any item between 1 to nextSeqId
		if(*)
		{
			send(tailNode, query, (client = this, key = (next - 1)* startIn));
			success = true;
		}
		else
		{
			send(tailNode, query, (client = this, key = (next + 1) * startIn));
			success = false;
		}
	}
	
	action checkReturn {
			if(success)
			{
				assert(keyvalue[(next - 1)* startIn] == ((client:id, value:int))payload.value);
			}
			else
			{
				assert(((client:id, value:int))payload.value == -1);
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
			//Global Monitor
			new Update_Propagation_Invariant();
			
			servers.three = new ChainReplicationServer((isHead = false, isTail = true, succ =null, smId = 3));
			servers.two = new ChainReplicationServer((isHead = false, isTail = false, succ =servers.three, smId = 2));
			servers.one = new ChainReplicationServer((isHead = true, isTail = false, succ =servers.two, smId = 1));
			send(servers.three, predNode, (pred = servers.two));
			send(servers.two, predNode, (pred = servers.one));
			send(servers.one, predNode, (pred = null));
			//create the client and start the game
			new Client((head = servers.one, tail = servers.three, startIn = 1));
			new Client((head = servers.one, tail = servers.three, startIn = 1));
			raise(delete);
		}
	}

}


machine Master {
	var clients : seq[id];
	var servers : seq[id]; // note that in this seq the first node is the head node and the last node is the tail node
	
	start state Init {
	
	
	}


}























event monitor_success : any;

// This is a simple monitor which checks that a update(x, y) followed immediately by a query for query(x) should return y;
// This monitor is created one per client, and can be used to check update-query sequences

monitor Update_Query_Seq
{
	var kv :(key:int, value:int);
	var myId : id;
	start state Init {
		entry {
			myId = (id)payload;
			raise(local);
		}
		on local goto Wait;
		
	}
	
	action assertcheck {
		if(trigger == update)
		{
			if(((client:id, kv: (key:int, value:int)))payload.client == myId)
			{
				assert(false);
			}
		}
		else if(trigger == responsetoquery)
		{
			if(((client: id, value : int))payload.client == myId)
			{
				assert(false);
			}
		}
	}
	state Wait {
		entry{
			
		}
		on responsetoquery do assertcheck;
		on update do CheckOperation;
		on monitor_success goto UpdateReq;
	}
	
	action CheckOperation {
		if(trigger == update)
		{
			if(((client:id, kv: (key:int, value:int)))payload.client == myId)
			{
				raise(monitor_success, payload);
			}
		}
		else if(trigger == responsetoquery)
		{
			if(((client: id, value : int))payload.client == myId)
			{
				raise(monitor_success, payload);
			}
		}
		else
		{
			assert(false);
		}
	}
	
	
	state UpdateReq {
		entry {
			kv.key = ((client:id, kv: (key:int, value:int)))payload.kv.key;
			kv.value = ((client:id, kv: (key:int, value:int)))payload.kv.value;
		}
		on update do assertcheck;
		on responsetoquery do CheckOperation;
		on monitor_success goto Wait {
			assert(((client: id, value : int))payload.value == kv.value);
		};
	}
}

/*************************************************************************************
* Invariants described in the paper
*************************************************************************************/

// This monitor checks the Update Propagation Invariant 
// Histj <= Histi forall i<=j --- Invariant 1
// Histi = Histj + Senti -- Invariant 2
//This is a global monitor

event monitor_history_update: (smId : int, history: seq[int]);
event monitor_sent_update: (smId : int, sent : seq[(seqId:int, kv: (key:int, value:int))]);

monitor Update_Propagation_Invariant {
	var histMap : map[int, seq[int]];
	var sentMap : map[int, seq[int]];
	var tempSeq : seq[int];
	var iter1 :int;
	var iter2 :int;
	start state Init {
		entry {
			raise(local);
		}
		on local goto WaitForUpdateMessage;
		
	}
	
	state WaitForUpdateMessage {
		
		on monitor_sent_update do CheckInvariant_2;
		on monitor_history_update do CheckInvariant_1;
	}
	
	fun checklessthan(s1 : seq[int], s2 : seq[int]) {
		assert(sizeof(s1) <= sizeof(s2));
		iter2 = sizeof(s1) - 1;
		while(iter2 >= 0)
		{
			assert(s1[iter2] == s2[iter2]);
			iter2 = iter2 - 1;
		}
	
	}
	
	action CheckInvariant_1 {
		IsSorted(payload.history);
		//update the history
		histMap.update(payload.smId, payload.history);
		
		//histsmid+1 <= histsmid
		if((payload.smId+1) in histMap) {
			checklessthan(histMap[(payload.smId+1)], histMap[payload.smId]);
		}
		
		//histsmId <= histsmId-1
		if((payload.smId-1) in histMap) {
			checklessthan(histMap[(payload.smId)], histMap[payload.smId -1]);
		}
	}
	
	
	
	fun extractSeqId(s : seq[(seqId:int, kv: (key:int, value:int))]) {
		
		iter1 = sizeof(s) - 1;
		while(iter1 >= 0)
		{
			tempSeq.insert(0, s[iter1].seqId);
			iter1 = iter1 - 1;
		}
	}
	
	fun mergeSeq(s1 : seq[int], s2 : seq[int])
	{
		IsSorted(s1);
		
		iter1 = 0;
		if(sizeof(s1) == 0)
			tempSeq = s2;
		else if(sizeof(s2) == 0)
			tempSeq = s1;
			
		while(iter1 <= sizeof(s1) - 1)
		{
			if(s1[iter1] < s2[0])
			{
				tempSeq.insert(sizeof(tempSeq), s1[iter1]);
			}	
			iter1 = iter1 + 1;
		}
		iter1 = 0;
		while(iter1 <= sizeof(s2) - 1)
		{
			tempSeq.insert(sizeof(tempSeq), s2[iter1]);
			iter1 = iter1 + 1;
		}
		
	}
	
	fun checkequal(s1 : seq[int], s2 : seq[int]) {
		assert(sizeof(s1) == sizeof(s2));
		iter2 = sizeof(s1) - 1;
		while(iter2 >= 0)
		{
			assert(s1[iter2] == s2[iter2]);
			iter2 = iter2 - 1;
		}
	
	}
	fun clearTempSeq()  {
		//clear tempSeq
		iter1 = sizeof(tempSeq) - 1;
		while(iter1 >= 0)
		{
			tempSeq.remove(iter1);
			iter1 = iter1 - 1;
		}
		assert(sizeof(tempSeq) == 0);
	}
	
	fun IsSorted(l:seq[int]){
        iter1 = 0;
        while (iter1 < sizeof(l) - 1) {
           assert(l[iter1] < l[iter1+1]);
            iter1 = iter1 + 1;
        }
	}
	
	action CheckInvariant_2 {
	
		clearTempSeq();
		
		//update the sent map
		extractSeqId(payload.sent);
		IsSorted(tempSeq);
		
		sentMap.update(payload.smId, tempSeq);
		clearTempSeq();
		
		/*
		//histsmid = hist(smid+1) + sentsmid
		if((payload.smId + 1) in histMap)
		{
			mergeSeq(histMap[payload.smId + 1], sentMap[payload.smId]);
			checkequal(histMap[payload.smId], tempSeq);
		}*/
		
		clearTempSeq();
		
		//histsmid-1 = hist(smid) + sentsmid-1	
		if((payload.smId - 1) in sentMap)
		{
			mergeSeq(histMap[payload.smId], sentMap[payload.smId - 1]);
			IsSorted(tempSeq);
			checkequal(histMap[payload.smId - 1], tempSeq);
		}
		
		clearTempSeq();
		
	}
}


