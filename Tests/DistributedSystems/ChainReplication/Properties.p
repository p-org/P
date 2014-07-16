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
// Histj <= Histi forall i<=j
//This is a global monitor

monitor Update_Propagation_Invariant {
	start state Init {
		entry {
		
		}
	
	}
}

