//Monitors

//We need two properties 
//1) Atomicity of transaction. If a transaction is either committed on all participants or aborted on all.
//2) In presence of fault-tolerance, a transaction should eventually be committed if its a valid transaction.



/*
monitor ReadWrite observes MONITOR_WRITE_SUCCESS, MONITOR_READ_SUCCESS{
	var client : machine;
	var data: (key:int,val:int);
	fun DoWriteSuccess() {
		if(payload.m == client)
			data = (key = payload.key, val = payload.val);
	}
	
	fun DoWriteFailure() {
		if(payload.m == client)
			data = (key = -1, val = -1);
	}
	fun CheckReadSuccess (){
		if(payload.m == client)
		{assert(data.key == payload.key && data.val == payload.val);}
			
	}
	fun CheckReadFailure() {
		if(payload == client)
			assert(data.key == -1 && data.val == -1);
	}
	start state Init {
		entry {
			client = payload as machine;
		}
		on MONITOR_WRITE_SUCCESS do DoWriteSuccess;
		on MONITOR_WRITE_FAILURE do DoWriteFailure;
		on MONITOR_READ_SUCCESS do CheckReadSuccess;
		on MONITOR_READ_FAILURE do CheckReadFailure;
	}
}

//
// The termination monitor checks the eventual consistency property. Eventually logs on all the machines 
// are the same (coordinator, replicas).
//

monitor ProgressGuarantee {
	var coordinator: machine;
	var replicas:seq[machine];
	var data : map[machine, map[int, int]];
	var i :int;
	var j : int;
	var same : bool;
	start state init {
		entry {
			coordinator = (payload as (machine, seq[machine])).0;
			replicas = (payload as (machine, seq[machine])).1;
		}
		on MONITOR_UPDATE goto UpdateData;
	}
	
	hot state UpdateData {
		entry {
		
			data[payload.m] [payload.key] = payload.val;
			if(sizeof(data[coordinator]) == sizeof(data[replicas[0]]))
			{
				i = sizeof(replicas) - 1;
				same = true;
				while(i >= 0)
				{
					if(sizeof(data[replicas[i]]) == sizeof(data[replicas[0]]) && same)
						same = true;
					else
						same = false;
						
					i = i - 1;
				}
			}
			if(same)
			{
				i = sizeof(data[coordinator]) - 1; 
				
				same = true;
				while(i>=0)
				{
					j = sizeof(replicas) - 1;
					while(j>=0)
					{
						assert(keys(data[coordinator])[i] in data[replicas[j]]);
						j = j - 1;
					}
				}
				
				raise(final);
			}
		
		}
		
		on final goto StableState;
		on MONITOR_UPDATE goto UpdateData;
		
	}
	
	state StableState{
		entry{}
		on MONITOR_UPDATE goto UpdateData;
	}
	
}
*/