event monitor_success : any;

// This is a simple monitor which checks that a update(x, y) followed immediately by a query for query(x) should return y;
// This monitor is created one per client, and can be used to check update-query sequences

monitor Update_Query_Seq
{
	var kv :(key:int, value:int);
	var myid : machine;
	start state Init {
		entry {
			myid = payload as machine;
			raise(local);
		}
		on local goto Wait;
		
	}
	
	fun assertcheck() {
		if(trigger == update)
		{
			if((payload as (client:machine, kv: (key:int, value:int))).client == myid)
			{
				assert(false);
			}
		}
		else if(trigger == responsetoquery)
		{
			if((payload as (client: machine, value : int)).client == myid)
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
	
	fun CheckOperation() {
		if(trigger == update)
		{
			if((payload as (client:machine, kv: (key:int, value:int))).client == myid)
			{
				raise monitor_success, payload;
			}
		}
		else if(trigger == responsetoquery)
		{
			if((payload as (client: machine, value : int)).client == myid)
			{
				raise monitor_success, payload;
			}
		}
		else
		{
			assert(false);
		}
	}
	
	
	state UpdateReq {
		entry {
			kv.key = (payload as (client:machine, kv: (key:int, value:int))).kv.key;
			kv.value = (payload as (client:machine, kv: (key:int, value:int))).kv.value;
		}
		on update do assertcheck;
		on responsetoquery do CheckOperation;
		on monitor_success goto Wait with {
			assert((payload as (client: machine, value : int)).value == kv.value);
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

event monitor_history_update: (smid : machine, history: seq[int]);
event monitor_sent_update: (smid : machine, sent : seq[(seqmachine:int, client : machine, kv: (key:int, value:int))]);
event monitor_update_servers : (servers: seq[machine]);

monitor Update_Propagation_Invariant {
	var servers : seq[machine];
	var histMap : map[machine, seq[int]];
	var sentMap : map[machine, seq[int]];
	var tempSeq : seq[int];
	var next : machine;
	var prev : machine;
	var iter1 :int;
	var iter2 :int;
	start state Init {
		entry {
			servers = payload as seq[machine];
			raise(local);
		}
		on local goto WaitForUpdateMessage;
		
	}
	
	fun UpdateServers() {
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
	fun GetNext(curr:machine){
		next = null;
		iter1 = 1;
		while(iter1 < sizeof(servers) - 1)
		{
			if(servers[iter1 - 1] == curr)
				next = servers[iter1];
				
			iter1 = iter1 + 1;
		}
	}
	
	fun GetPrev(curr:machine) {
		prev = null;
		iter1 = 1;
		while(iter1 < sizeof(servers) - 1)
		{
			if(servers[iter1] == curr)
				prev = servers[iter1 - 1];
				
			iter1 = iter1 + 1;
		}
	}
	
	fun CheckInvariant_1() {
		IsSorted(payload.history);
		//update the history
		histMap[payload.smid] = payload.history;
		
		//histsmid+1 <= histsmid
		GetNext(payload.smid);
		if(next in histMap) {
			checklessthan(histMap[next], histMap[payload.smid]);
		}
		
		//histsmid <= histsmid-1
		GetPrev(payload.smid);
		if(prev in histMap) {
			checklessthan(histMap[payload.smid], histMap[prev]);
		}
	}
	
	
	fun extractSeqmachine(s : seq[(seqmachine:int, client : machine, kv: (key:int, value:int))]) {
		clearTempSeq();
		iter1 = sizeof(s) - 1;
		while(iter1 >= 0)
		{
			tempSeq += (0, s[iter1].seqmachine);
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
				tempSeq += (sizeof(tempSeq), s1[iter1]);
			}	
			iter1 = iter1 + 1;
		}
		iter1 = 0;
		while(iter1 <= sizeof(s2) - 1)
		{
			tempSeq += (sizeof(tempSeq), s2[iter1]);
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
			tempSeq -= (iter1);
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
	
	fun CheckInvariant_2 (){
	
		clearTempSeq();
		
		//update the sent map
		extractSeqmachine(payload.sent);
		sentMap[payload.smid] = tempSeq;
		clearTempSeq();
		
		GetNext(payload.smid);
		//histsmid = hist(smid+1) + sentsmid
		if(next in histMap)
		{
			mergeSeq(histMap[next], sentMap[payload.smid]);
			checkequal(histMap[payload.smid], tempSeq);
		}
		
		clearTempSeq();
		
		GetPrev(payload.smid);
		//histsmid-1 = hist(smid) + sentsmid-1	
		if(prev in sentMap)
		{
			mergeSeq(histMap[payload.smid], sentMap[prev]);
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
event monitor_reponsetoupdate : (tail:machine, key :int, value: int);
event monitor_responsetoquery : (tail: machine, key : int, value : int);

monitor UpdateResponse_QueryResponse_Seq {
	var lastUpdateReponse : map[int, int];
	var servers : seq[machine];
	var iter : int;
	var returnVal : bool;
	
	start state Init {
		entry {
			raise(local);
		}
		on local goto Wait;
	}
	
	fun UpdateServers() {
		servers = payload.servers;
	}
	
	fun Contains(s: seq[machine], item:machine){
		iter = 0;
		while(iter < sizeof(servers))
		{
			if(s[iter] == item)
			{
				returnVal = true;
			}
			iter = iter + 1;
		}
		returnVal = false;
	}
	
	state Wait {
		on monitor_update_servers do UpdateServers;
		on monitor_reponsetoupdate goto Wait with {
			Contains(servers, payload.tail);
			if(returnVal)
			{
				lastUpdateReponse[payload.key] = payload.value;
			}
		}; 
		on monitor_responsetoquery goto Wait with {
			Contains(servers, payload.tail);
			if(returnVal)
			{
				assert(payload.value == lastUpdateReponse[payload.key]);
			}
		};
	}
}
