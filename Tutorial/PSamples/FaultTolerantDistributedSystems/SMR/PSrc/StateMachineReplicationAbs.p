machine LinearizabilityAbs
receives eSMROperation;
sends eSMRReplicatedMachineOperation, eSMRLeaderUpdated, eSMRReplicatedLeader;
{
	var replicatedSM : SMRReplicatedMachineInterface;
	var doReordering : bool;
	var myId: int;
	var client : SMRClientInterface;
	var respId : int;
	start state Init {
		entry (payload: SMRServerConstrutorType){
			var i : int;
			i = 0;
			respId = 0;
			client = payload.client;
			//create the replicated machine
			doReordering = payload.reorder;
			myId = payload.val as int;
			replicatedSM = new SMRReplicatedMachineInterface((client = payload.client, val = (payload.val as int, true)));
			
			send replicatedSM, eSMRReplicatedLeader;
			//for the specification case send the current 
			SendSMRServerUpdate(client, (myId, this as SMRServerInterface));
			
			if(doReordering)
				goto DoReOrdering;
			else
				goto DoNoReOrdering;
		}
	}
	//we have created a parameterized linearizability abstraction
	//since some cases messages can be 
	//reordered while in some cases the SMR implementation provides a guarantee of inorder responses.
	var pending: seq[SMROperationType];	
	state DoReOrdering {
		entry {
			while(sizeof(pending) >0)
			{
				SendSMRRepMachineOperation(replicatedSM, pending[0], respId);
				pending -= 0;
				respId = respId + 1;
				if($)
					return;
			}
		}
		on eSMROperation goto DoReOrdering with (payload: SMROperationType){
			var index : int;
			index = chooseIndex();
			pending += (index, payload);
		}
	}
	
	fun chooseIndex() : int
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
			SendSMRRepMachineOperation(replicatedSM, payload, respId);
			respId = respId + 1;
		}
	}
}