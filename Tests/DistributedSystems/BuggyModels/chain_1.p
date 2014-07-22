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
		succ = payload.succ;
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
event monitor_sent_update: (smId : int, sent : seq[(seqId:int, client : id, kv: (key:int, value:int))]);

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
		IsSorted(s1);
		IsSorted(s2);
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
	
	
	fun extractSeqId(s : seq[(seqId:int, client : id, kv: (key:int, value:int))]) {
		clearTempSeq();
		iter1 = sizeof(s) - 1;
		while(iter1 >= 0)
		{
			tempSeq.insert(0, s[iter1].seqId);
			iter1 = iter1 - 1;
		}
		IsSorted(tempSeq);
	}
	
	fun mergeSeq(s1 : seq[int], s2 : seq[int])
	{
		clearTempSeq();
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
		IsSorted(tempSeq);
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
		assert(sizeof(tempSeq) <= 6);
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
		sentMap.update(payload.smId, tempSeq);
		clearTempSeq();
		
		
		//histsmid = hist(smid+1) + sentsmid
		if((payload.smId + 1) in histMap)
		{
			mergeSeq(histMap[payload.smId + 1], sentMap[payload.smId]);
			checkequal(histMap[payload.smId], tempSeq);
		}
		
		clearTempSeq();
		
		//histsmid-1 = hist(smid) + sentsmid-1	
		if((payload.smId - 1) in sentMap)
		{
			mergeSeq(histMap[payload.smId], sentMap[payload.smId - 1]);
			checkequal(histMap[payload.smId - 1], tempSeq);
		}
		
		clearTempSeq();
		
	}
}

/*
A more generic monitor that checks the Update_Query_Seq in the presence of failure of nodes including the
tail node.

It is a global monitor !

*/
event monitor_reponsetoupdate : (key :int, value: int);
event monitor_responsetoquery : (key : int, value : int);

monitor UpdateResponse_QueryResponse_Seq {
	var lastUpdateReponse : map[int, int];
	start state Init {
		entry {
			raise(local);
		}
		on local goto Wait;
	}
	
	state Wait {
		on monitor_reponsetoupdate goto Wait {
			lastUpdateReponse.update (((key : int, value : int))payload.key, ((key : int, value : int))payload.value);
		}; 
		on monitor_responsetoquery goto Wait {
			assert(((key : int, value : int))payload.value == lastUpdateReponse[((key : int, value : int))payload.key]);
		};
	}
}

/*
The client machine checks that for a configuration of 3 nodes 
An update(k,v) is followed by a successful query(k) == v
Also a random query is performed in the end.

*/

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
		on local goto PumpUpdateRequests;
	}
	
	state PumpUpdateRequests
	{
		ignore responsetoupdate;
		entry {
			send(headNode, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn])));

			if(next >= 3) // only test for now
			{
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on done goto PumpQueryRequests
		{
			next = 1;
		};
		on local goto PumpUpdateRequests
		{
			next = next + 1;
		};
	}
	
	state end {
		entry {
			raise(delete);
		}
	}

	
	
	state PumpQueryRequests {
		ignore responsetoquery;
		entry {
			send(tailNode, query, (client = this, key = next * startIn));
			if(next >= 3) // only test for now
			{
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on local goto PumpQueryRequests
		{
			next = next + 1;
		};
		
		on done goto end;
	}
	
}

main machine TheGodMachine {
	var servers : seq[id];
	var clients : seq[id];
	var temp : id;
	start state Init {
		entry {
			//Global Monitor
			new Update_Propagation_Invariant();
			new UpdateResponse_QueryResponse_Seq();
			
			
			temp = new ChainReplicationServer((isHead = false, isTail = true, smId = 3));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = false, isTail = false, smId = 2));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = true, isTail = false, smId = 1));
			servers.insert(0, temp);
			send(servers[2], predSucc, (pred = servers[1], succ = servers[2]));
			send(servers[1], predSucc, (pred = servers[0], succ = servers[2]));
			send(servers[0], predSucc, (pred = servers[0], succ = servers[1]));
			//create the client and start the game
			temp = new Client((head = servers[0], tail = servers[2], startIn = 1));
			clients.insert( 0, temp);
			temp = new Client((head = servers[0], tail = servers[2], startIn = 100));
			clients.insert( 0, temp);
			
			new ChainReplicationMaster((servers = servers, clients = clients));
			raise(delete);
		}
	}

}

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
event becomeHead : id;
event becomeTail : id;
event newPredecessor : (pred : id, master : id);
event newSuccessor : (succ : id, master : id, lastUpdateRec: int, lastAckSent: int);
event updateHeadTail : (head : id, tail : id);
event newSuccInfo : (lastUpdateRec : int, lastAckSent : int);

machine ChainReplicationMaster {
	var clients : seq[id];
	var servers : seq[id]; // note that in this seq the first node is the head node and the last node is the tail node
	var faultMonitor : id;
	var head : id;
	var tail : id;
	var iter : int;
	var faultyNodeIndex : int;
	var lastUpdateReceivedSucc : int;
	var lastAckSent : int;
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
			send(head, becomeHead, this);
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
			send(tail, becomeTail, this);
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
	action SetLastUpdateAndReturn{
		
		lastUpdateReceivedSucc = payload.lastUpdateRec;
		lastAckSent = payload.lastAckSent;
		return;
		
	}
	
	state FixSuccessor {
		entry {
			send(servers[faultyNodeIndex + 1], newPredecessor, (pred = servers[faultyNodeIndex - 1], master = this));
		}
		on newSuccInfo do SetLastUpdateAndReturn;
	}
	
	state FixPredecessor {
		entry {
			send(servers[faultyNodeIndex - 1], newSuccessor, (succ = servers[faultyNodeIndex + 1], master = this, lastAckSent = lastAckSent, lastUpdateRec = lastUpdateReceivedSucc));
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























