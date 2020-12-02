/******************************************************************
* Description:
* This file presents the ChainReplicationNodeMachine that implements the Chain Replication protocol
*******************************************************************/

machine ChainReplicationNodeMachine
sends eBackwardAck, eForwardUpdate, eCRPong, eNewSuccInfo, eSMRReplicatedLeader, eSuccess, eTailChanged, eHeadChanged, eSMRReplicatedMachineOperation, eSMRLeaderUpdated, ePredSucc,
eMonitorHistoryUpdate, eMonitorSentUpdate, eMonitorUpdateForLiveness, eMonitorResponseForLiveness;
 {
	var nextSeqId : int;
	var repSM : SMRReplicatedMachineInterface;
	var history : seq[int];
	var nodeT : NodeType;
	var succ : ChainReplicationNodeInterface; 
	var pred : ChainReplicationNodeInterface; 
	var sent : seq[(seqId: int, smrop: SMROperationType)];
	var client : SMRClientInterface;
	var FT : FaultTolerance;
	var commitId : int;
	start state Init {
		defer eBackwardAck, eForwardUpdate, eCRPing;
		entry (payload: SMRServerConstrutorType){
			var repSMConstArg : data;
			
			client = payload.client;
			FT = payload.ft;

			if(payload.isRoot)
			{
				//update the client about leader
				SendSMRServerUpdate(client, (0, this to SMRServerInterface));
				nodeT = HEAD;
				repSMConstArg = payload.val;
				//create the rest of nodes
				SetUp(repSMConstArg);
			}
			else
			{
				nodeT = (payload.val as (NodeType, data)).0;
				repSMConstArg = (payload.val as (NodeType, data)).1;
			}
			
			

			//create the replicated node 
			repSM = new SMRReplicatedMachineInterface((client = payload.client, val = repSMConstArg));

			if(nodeT == TAIL)
			{
				//tell the replicated machine that it is the leader now
				send repSM, eSMRReplicatedLeader;
			}
			nextSeqId = 0;
			receive {
				case ePredSucc: (payload1: (pred: ChainReplicationNodeInterface, succ: ChainReplicationNodeInterface)) {
					pred = payload1.pred;
					succ = payload1.succ;
				}
			}

			commitId = 0;
			raise local;
		}
		on local push WaitForRequest;

		on eSMROperation do (payload: SMROperationType){
			//all operations are considered as update operations.
			raise eUpdate, payload;
		}
	
	}

	fun SetUp(repSMConstArg: data) {
		var numOfNodes : int;
		var nodes : seq[ChainReplicationNodeInterface];
		var tempNode: ChainReplicationNodeInterface;
		var index : int;
		if(FT == FT1)
			numOfNodes = 2;
		else if (FT == FT2)
			numOfNodes = 3;
		else
			assert(false);

		//create all the nodes
		//first add the current node itself as head
		nodes += (0, this to ChainReplicationNodeInterface);
		
		//create internal nodes
		index = 0;
		while(index < numOfNodes - 2)
		{
			tempNode = new ChainReplicationNodeInterface((client = client, reorder = false, isRoot = false, ft = FT, val = (INTERNAL, repSMConstArg)));
			nodes += (sizeof(nodes), tempNode);
			index = index + 1;
		}

		//create tail
		tempNode = new ChainReplicationNodeInterface((client = client, reorder = false, isRoot = false, ft = FT, val = (TAIL, repSMConstArg)));
		nodes += (sizeof(nodes), tempNode);
		
		//send pred and succ to all
		//head
		send nodes[0], ePredSucc, (pred = nodes[0], succ = nodes[1]);
		//tail
		send nodes[sizeof(nodes)-1], ePredSucc, (pred = nodes[sizeof(nodes)-2], succ = nodes[sizeof(nodes)-1]);
		//internal nodes
		index = 1;
		while(index < numOfNodes - 1)
		{
			send nodes[index], ePredSucc, (pred = nodes[index-1], succ = nodes[index + 1]);
			index = index + 1;
		}

		//create the master node.
		new ChainReplicationMasterInterface((client = client, nodes = nodes));
	}

	fun UpdateSuccessor(payload: (succ : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface, lastUpdateRec: int, lastAckSent: int))
	{
		var tempIndex : int;
		var iter: int;
		tempIndex = -1;
		succ = payload.succ;
		if(sizeof(sent) > 0)
		{
			//send the remaining history
			iter = 0;
			while(iter < sizeof(sent))
			{
				if(sent[iter].seqId > payload.lastUpdateRec)
					send succ, eForwardUpdate, (msg = sent[iter], pred = this to ChainReplicationNodeInterface);
				
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
			while(iter < tempIndex)
			{
				send pred, eBackwardAck, (seqId = sent[0].seqId, );
				sent -= (0);
				iter = iter + 1;
			}
		}
		
		send payload.master, eSuccess;
	}
	
	state WaitForRequest {
		on eCRPing do (payload: ChainReplicationFaultDetectorInterface){
			send payload, eCRPong;
		}

		on eBecomeTail do (payload: ChainReplicationMasterInterface){
			var iter : int;
			if(nodeT != HEAD)
				nodeT = TAIL;
			succ = this to ChainReplicationNodeInterface;
			//send all the events in sent to both client and the predecessor
			iter = 0;
			while(iter < sizeof(sent))
			{				
				//tell the replicated machine that it is the leader now
				send repSM, eSMRReplicatedLeader;

				
				// the backward ack to the pred
				send pred, eBackwardAck, (seqId = sent[iter].seqId, );
				iter = iter + 1;
			}
			send payload, eTailChanged;
		}

		on eBecomeHead do (payload: ChainReplicationMasterInterface){
			nodeT = HEAD;
			pred = this to ChainReplicationNodeInterface;
			send payload, eHeadChanged;
			//update the client about leader
			SendSMRServerUpdate(client, (0, this to SMRServerInterface));
		}

		on eNewPredecessor do (payload: (pred : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface)){
			pred = payload.pred;
			if(sizeof(history) > 0)
			{
				if(sizeof(sent) > 0)
					send payload.master, eNewSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = sent[0].seqId);
				else
					send payload.master, eNewSuccInfo , (lastUpdateRec = history[sizeof(history) - 1], lastAckSent = history[sizeof(history) - 1]);
			}
		}
		on eNewSuccessor do (payload: (succ : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface, lastUpdateRec: int, lastAckSent: int)) {
			UpdateSuccessor(payload);
		}
	
		
		on eUpdate goto ProcessUpdate with 
		{
			nextSeqId = nextSeqId + 1;
			assert(nodeT == HEAD || (nodeT == TAIL && pred == this to ChainReplicationNodeInterface));
			announce eMonitorUpdateForLiveness, (seqId = nextSeqId, );
		}
		
		on eForwardUpdate goto ProcessFwdUpdate;
		on eBackwardAck goto ProcessBackwardAck;
	}
	
	state ProcessUpdate {
		entry (payload: SMROperationType){	
			
			//Send the operation to the replicated SM
			SendSMRRepMachineOperation(repSM, payload, commitId);
			commitId = commitId + 1;

			//add it to the history seq (represents the successfully serviced requests)
			history += (sizeof(history), nextSeqId);
			print "Process Update";
			IsSorted(history);
			//invoke the monitor
			announce eMonitorHistoryUpdate, (node = this to ChainReplicationNodeInterface, history = history);
			
			//Add the update request to sent seq
			sent += (sizeof(sent), (seqId = nextSeqId, smrop = payload));
			
			//call the monitor
			announce eMonitorSentUpdate, (node = this to ChainReplicationNodeInterface, sent = sent);
			//forward the update to the succ
			send succ, eForwardUpdate, (msg = (seqId = nextSeqId, smrop = payload), pred = this to ChainReplicationNodeInterface);
	
			raise(local);
		}
		on local goto WaitForRequest;
	}

	state ProcessFwdUpdate {
		entry (payload: (msg: (seqId: int, smrop: SMROperationType), pred: ChainReplicationNodeInterface)){
			if(payload.pred == pred)
			{
				if(nodeT == INTERNAL)
				{
					//update my nextSeqId
					nextSeqId = payload.msg.seqId;
					//Send the operation to the replicated SM
					SendSMRRepMachineOperation(repSM, payload.msg.smrop, commitId);
					commitId = commitId + 1;

					//add it to the history seq (represents the successfully serviced requests)
					history += (sizeof(history), payload.msg.seqId);
					IsSorted(history);
					//invoke the monitor
					announce eMonitorHistoryUpdate, (node = this to ChainReplicationNodeInterface, history = history);
					//Add the update request to sent seq
					sent += (sizeof(sent), payload.msg);
					//call the monitor
					announce eMonitorSentUpdate, (node = this to ChainReplicationNodeInterface, sent = sent);
					//forward the update to the succ
					send succ, eForwardUpdate, (msg = payload.msg, pred = this to ChainReplicationNodeInterface);
				}
				else
				{
					if(nodeT != HEAD)
					{
						//add it to the history seq (represents the successfully serviced requests)
						history += (sizeof(history), payload.msg.seqId);
						IsSorted(history);
						//update my nextSeqId
						nextSeqId = payload.msg.seqId;
						
						//invoke the monitor
						announce eMonitorResponseForLiveness, (seqId = nextSeqId, commitId = commitId);
						
						//Send the operation to the replicated SM
						SendSMRRepMachineOperation(repSM, payload.msg.smrop, commitId);

						commitId = commitId + 1;

					}
					//send ack to the pred
					send pred, eBackwardAck, (seqId = payload.msg.seqId, );

				}
			}
			goto WaitForRequest;
		}
	}
	
	fun IsSorted(l:seq[int]){
		var iter: int;
        iter = 0;
        while (iter < sizeof(l) - 1) {
		   print "History: {0}\n", l;
           assert(l[iter] < l[iter+1]);
            iter = iter + 1;
        }
	}


	state ProcessBackwardAck
	{
		entry (payload: (seqId: int)){
			//remove the request from sent seq.
			RemoveItemFromSent(payload.seqId);
			
			if(nodeT != HEAD)
			{
				//forward it back to the pred
				send pred, eBackwardAck, (seqId = payload.seqId,);
			}
			goto WaitForRequest;
		}
	}
	
	fun RemoveItemFromSent(req : int) {
		var iter : int;
		iter = sizeof(sent) - 1;
		while(iter >= 0)
		{
			if(req == sent[iter].seqId)
			{
				sent -= iter;
				return;
			}
			iter = iter - 1;
		}
	}
	
}



















