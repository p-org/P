
//Monitors


// ReadWrite monitor keeps track of the property that every successful write should be followed by
// successful read and failed write should be followed by a failed read.
// This monitor is created local to each client.

monitor ReadWrite {
	var client : id;
	var data: (key:int,val:int);
	action DoWriteSuccess {
		if(payload.m == client)
			data = (key = payload.key, val = payload.val);
	}
	
	action DoWriteFailure {
		if(payload.m == client)
			data = (key = -1, val = -1);
	}
	action CheckReadSuccess {
		if(payload.m == client)
		{assert(data.key == payload.key && data.val != payload.val);}
			
	}
	action CheckReadFailure {
		if(payload == client)
			assert(data.key == -1 && data.val == -1);
	}
	start state Init {
		entry {
			client = (id) payload;
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

monitor Termination {
	var coordinator: id;
	var replicas:seq[id];
	var data : map[id, map[int, int]];
	var i :int;
	var j : int;
	var same : bool;
	start state init {
		entry {
			coordinator = ((id, seq[id]))payload[0];
			replicas = ((id, seq[id]))payload[1];
		}
		on MONITOR_UPDATE goto UpdateData;
	}
	
	state UpdateData {
		entry {
		
			data[payload.m].update(payload.key, payload.val);
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
	
	stable state StableState{
		entry{}
		on MONITOR_UPDATE goto UpdateData;
	}
	
}
