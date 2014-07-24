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

event monitor_history_update: (smId : id, history: seq[int]);
event monitor_sent_update: (smId : id, sent : seq[(seqId:int, client : id, kv: (key:int, value:int))]);
event monitor_update_servers : (servers: seq[id]);

monitor Update_Propagation_Invariant {
	var servers : seq[id];
	var histMap : map[id, seq[int]];
	var sentMap : map[id, seq[int]];
	var tempSeq : seq[int];
	var next : id;
	var prev : id;
	var iter1 :int;
	var iter2 :int;
	start state Init {
		entry {
			servers = (seq[id])payload;
			raise(local);
		}
		on local goto WaitForUpdateMessage;
		
	}
	
	action UpdateServers {
		servers = payload.servers;
	}
	
	state WaitForUpdateMessage {
		
		on monitor_sent_update do CheckInvariant_2;
		on monitor_history_update do CheckInvariant_1;
		on monitor_update_servers do UpdateServers;
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
	fun GetNext(curr:id){
		next = null;
		iter1 = 1;
		while(iter1 < sizeof(servers) - 1)
		{
			if(servers[iter1 - 1] == curr)
				next = servers[iter1];
				
			iter1 = iter1 + 1;
		}
	}
	
	fun GetPrev(curr:id) {
		prev = null;
		iter1 = 1;
		while(iter1 < sizeof(servers) - 1)
		{
			if(servers[iter1] == curr)
				prev = servers[iter1 - 1];
				
			iter1 = iter1 + 1;
		}
	}
	
	action CheckInvariant_1 {
		IsSorted(payload.history);
		//update the history
		histMap.update(payload.smId, payload.history);
		
		//histsmid+1 <= histsmid
		GetNext(payload.smId);
		if(next in histMap) {
			checklessthan(histMap[next], histMap[payload.smId]);
		}
		
		//histsmId <= histsmId-1
		GetPrev(payload.smId);
		if(prev in histMap) {
			checklessthan(histMap[payload.smId], histMap[prev]);
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
		
		GetNext(payload.smId);
		//histsmid = hist(smid+1) + sentsmid
		if(next in histMap)
		{
			mergeSeq(histMap[next], sentMap[payload.smId]);
			checkequal(histMap[payload.smId], tempSeq);
		}
		
		clearTempSeq();
		
		GetPrev(payload.smId);
		//histsmid-1 = hist(smid) + sentsmid-1	
		if(prev in sentMap)
		{
			mergeSeq(histMap[payload.smId], sentMap[prev]);
			checkequal(histMap[prev], tempSeq);
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
