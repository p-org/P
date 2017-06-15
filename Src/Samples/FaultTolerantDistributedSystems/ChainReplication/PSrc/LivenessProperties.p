/*
We will check 3 liveness properties 
1 -> In the absence of failures all client update request should be followed eventually by a response.
2 -> In the presence of n nodes and n-1 failures and the head node does not fail then all client requests should be followed eventually by a response.
3 -> In the presence of n nodes and n-1 failures where even the tail node can fail then all client query requests should be followed eventually by a response.
*/
event monitor_updateLiveness : (reqId : int);
event monitor_responseLiveness : (reqId : int);
event monitor_queryLiveness : (reqId : int);

monitor livenessUpdatetoResponse {
	var myRequestId : int;
	start state Init {
		entry {
			myRequestId = (int) payload;
			raise(local);
		}
		on local goto WaitForUpdateRequest;
	}
	fun checkIfMine (){
		if(payload.reqId == myRequestId)
			raise(monitor_success);
	}
	
	fun assertNotMine (){
		assert(myRequestId != payload.reqId);
	}
	hot state WaitForUpdateRequest {
		entry {
			
		}
		on monitor_updateLiveness do checkIfMine;
		on monitor_responseLiveness do assertNotMine;
		on monitor_success goto WaitForResponse;
	}
	
	hot state WaitForResponse {
		entry {
		
		}
		on monitor_updateLiveness do assertNotMine;
		on monitor_responseLiveness do checkIfMine;
		on monitor_success goto DoneMoveToStableState;
	}
	
	state DoneMoveToStableState {
		ignore monitor_updateLiveness, monitor_responseLiveness;
	}
	
	
}

monitor livenessQuerytoResponse {
	var myRequestId : int;
	start state Init {
		entry {
			myRequestId = payload;
			raise(local);
		}
		on local goto WaitForQueryRequest;
	}
	fun checkIfMine (){
		if(payload.reqId == myRequestId)
			raise(monitor_success);
	}
	
	fun assertNotMine (){
		assert(myRequestId != payload.reqId);
	}
	hot state WaitForQueryRequest {
		entry {
			
		}
		on monitor_queryLiveness do checkIfMine;
		on monitor_responseLiveness do assertNotMine;
		on monitor_success goto WaitForResponse;
	}
	
	hot state WaitForResponse {
		entry {
		
		}
		on monitor_queryLiveness do assertNotMine;
		on monitor_responseLiveness do checkIfMine;
		on monitor_success goto DoneMoveToStableState;
	}
	
	state DoneMoveToStableState {
		ignore monitor_queryLiveness, monitor_responseLiveness;
	}
	
	
}