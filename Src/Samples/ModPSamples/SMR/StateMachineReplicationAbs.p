machine LinearizibilityAbs : SMRServerInterface
receives eSMROperation;
sends eSMRReplicatedMachineOperation, eSMRLeaderUpdated;
{
	var replicatedSM : SMRReplicatedMachineInterface;
	var doReordering : bool;
	var myId: int;
	var client : SMRClientInterface;
	
	start state Init {
		entry (payload: (client: SMRClientInterface, reorder: bool, id: int)){
			var i : int;
			i = 0;
			client = payload.client;
			//create the replicated machine
			doReordering = payload.reorder;
			myId = payload.id;
			replicatedSM = new SMRReplicatedMachineInterface(myId);
			
			//for the specification case send the current 
			send client, eSMRLeaderUpdated, (myId, this as SMRServerInterface);
			
			if(doReordering)
				goto DoReOrdering;
			else
				goto DoNoReOrdering;
		}
	}
	//we have created a parameterized linearizibility abstraction
	//since some cases messages cannot be 
	//reordered and for the case of like in replicated hash table they can be reordered.
	var pending: seq[SMROperationType];	
	state DoReOrdering {
		entry {
			while(sizeof(pending) >0)
			{
				send replicatedSM, eSMRReplicatedMachineOperation, pending[0];
				pending -= 0;
				if($)
					return;
			}
		}
		on eSMROperation goto DoReOrdering with (payload: SMROperationType){
			pending += (chooseIndex(), payload);
		}
	}
	
	fun chooseIndex() : int
	[pure = null]
	{
		var i: int;
		i = 0;
		while(i <sizeof(pending))
		{
			if($)
				return i;
			i = i + 1;
		}
	}
	
	state DoNoReOrdering {
		on eSMROperation do (payload: SMROperationType){
			send replicatedSM, eSMRReplicatedMachineOperation, payload;
		}
	}
}