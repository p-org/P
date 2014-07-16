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
			if(((client:id, seqId:int, kv: (key:int, value:int)))payload.client == myId)
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
			if(((client:id, seqId:int, kv: (key:int, value:int)))payload.client == myId)
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
			kv.key = ((client:id, seqId:int, kv: (key:int, value:int)))payload.kv.key;
			kv.value = ((client:id, seqId:int, kv: (key:int, value:int)))payload.kv.value;
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
	var histMap : map[id, seq[int]];
	var sentMap : map[id, seq[int]];
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
		//update the history
		histMap.update(payload.smId, payload.history);
		iter1 = 0;
		while(iter1 < sizeof(histMap) - 1)
		{
			checklessthan(histMap[iter1], histMap[iter1 + 1]);
			iter1 = iter1 + 1;
		}
	}
	
	
	
	fun extractSeqId(s : seq[(seqId:int, kv: (key:int, value:int))]) {
		//clear temp
		iter1 = sizeof(tempSeq) - 1;
		while(iter1 >= 0)
		{
			tempSeq.remove(iter1);
			iter1 = iter1 - 1;
		}
		assert(sizeof(tempSeq) == 0);
		
		iter1 = sizeof(s) - 1;
		while(iter1 >= 0)
		{
			tempSeq.insert(0, s[iter1].seqId);
			iter1 = iter1 - 1;
		}
	}
	
	fun mergeSeq(s1 : seq[int], s2 : seq[int])
	{
		//clear tempSeq
		iter1 = sizeof(tempSeq) - 1;
		while(iter1 >= 0)
		{
			tempSeq.remove(iter1);
			iter1 = iter1 - 1;
		}
		assert(sizeof(tempSeq) == 0);
		
		iter1 = 0;
		iter2 = 0;
		while(iter1 <= sizeof(s1) - 1)
		{
			if(s1[iter1] < s2[0])
			{
				tempSeq.insert(iter2, s1[iter1]);
				iter2 = iter2 + 1;
			}	
			iter1 = iter1 + 1;
		}
		iter1 = 0;
		while(iter1 <= sizeof(s2))
		{
			tempSeq.insert(iter2, s2[iter1]);
			iter2 = iter2 + 1;
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
	
	action CheckInvariant_2 {
		//update the sent map
		extractSeqId(payload.sent);
		sentMap.update(payload.smId, tempSeq);
		
		while(iter1 < sizeof(histMap) - 1)
		{
			mergeSeq(histMap[iter1 + 1], sent[iter1]);
			checkequal(histMap[iter1], tempSeq);
			iter1 = iter1 + 1;
		}
		
		//clear tempSeq
		iter1 = sizeof(tempSeq) - 1;
		while(iter1 >= 0)
		{
			tempSeq.remove(iter1);
			iter1 = iter1 - 1;
		}
		assert(sizeof(tempSeq) == 0);
		
	}
}

