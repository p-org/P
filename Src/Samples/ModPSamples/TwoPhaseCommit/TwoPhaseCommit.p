
//We implemented a fault-tolerant 2-PC protocol.
//There is a single co-ordinator and 2 participants. The two participants are two different bank accounts.
enum TPCConfig {
	dummy = 0,
	NumOfParticipants = 2
}
machine Coordinator : CoorClientInterface
receives ePrepared, eNotPrepared, eStatusResp, eTimeOut, eCancelSuccess, eCancelFailure, eSMRResponse, eSMRLeaderUpdated;
sends eCommit, eAbort, ePrepare, eStatusQuery, eTransactionFailed, eTransactionSuccess, eTransactionTimeOut, eRespPartStatus, eStartTimer, eCancelTimer, eSMROperation;
{

	var transId : int;
	var participants : map[int, machine];
	var timer : TimerPtr;
	var currentTransaction: TransactionType;
	var isFaultTolerant : bool;
	start state Init {
		entry (payload: (isfaultTolerant: bool)){
			var temp : machine;
			var index : int;
			isFaultTolerant = payload.isfaultTolerant;
			if(isFaultTolerant)
			{
				index = 0;
				while(index < NumOfParticipants)
				{
					temp = new SMRServerInterface((client = this as SMRClientInterface, reorder = false, id = index));
					participants[index] = temp;
					index = index + 1;
				}
			}
			else
			{
				index = 0;
				while(index < NumOfParticipants)
				{
					temp = new ParticipantInterface(this as CoorParticipantInterface, index, false);
					participants[index] = temp;
					index = index + 1;
				}
			}
			
			transId = 0;
			//create timer
			timer = CreateTimer(this as ITimerClient);

			raise local;
		}

		//install common handlers for all states
		on eSMRResponse do (payload: SMRResponseType){
			raise payload.response, payload.val;
		} 

		on eSMRLeaderUpdated do (payload: (int, SMRServerInterface)) {
			participants[payload.0] = payload.1;
		}

		on local push WaitForTransactionReq;
	}
	
	fun SendToParticipant(part: machine, ev: event, payload: data) {
		if(isFaultTolerant)
		{
			send part, eSMROperation, (source = this as SMRClientInterface, operation = ev, val = payload);
		}
		else
		{
			send part, ev, payload;
		}
	}
	
	fun SendToAllParticipants(ev: event, val: any)
	{
		var i : int;
		i = 0;
		while(i < NumOfParticipants)
		{
			SendToParticipant(participants[i], ev, val as data);
			i = i + 1;
		}
	}

	state WaitForTransactionReq {
		ignore eNotPrepared, ePrepared;
		on eTransaction goto ProcessTransaction with (payload : TransactionType){
			currentTransaction = payload;
			transId = transId + 1;
		}
		on eReadPartStatus do (clientReq: (source: ClientInterface, part:int)){
			SendToParticipant(participants[clientReq.part], eStatusQuery, null);
			receive {
				case eStatusResp: (payload: ParticipantStatusType) {
					send clientReq.source, eRespPartStatus, payload;
				}
			}
		}
	}
	
	

	fun AbortCurrentTransaction() {
		SendToAllParticipants(eAbort, (tid = transId,));
		send currentTransaction.source, eTransactionFailed;
		CancelTimer(timer);	
	}
	
	var isPrepared : map[int, bool];
	fun ResetPrepared() {
		var index : int;
		index = 0;
		while(index < NumOfParticipants)
		{
			isPrepared[index] = false;
			index = index + 1;
		}
	}
	fun ReceivedAllPrepare() : bool 
	[pure = null]
	{
		var index : int;
		index = 0;
		print "{0}\n", isPrepared;
		print "Num: {0}\n", NumOfParticipants;
		while(index < NumOfParticipants)
		{
			if(!isPrepared[index])
			{
				return false;
			}
			index = index + 1;
		}
		return true;
	}
	state ProcessTransaction {
		defer eTransaction, eReadPartStatus;
		entry{
			ResetPrepared();
			SendToAllParticipants(ePrepare, (tid = transId, op = currentTransaction.op));
			//start timer 
			StartTimer(timer, 100);
		}
		on eTimeOut do { 
			AbortCurrentTransaction();
			announce eTransactionTimeOut;
			goto ProcessTransaction; 
		}
		
		on eNotPrepared do (payload: (tid:int)){
			if(payload.tid != transId)
				return;
			else
				AbortCurrentTransaction();
		}
		
		on ePrepared do (payload: (tid: int, part: int)){
			var i : int;
			if(payload.tid == transId)
			{
				print "{0}\n", isPrepared;
				print "Num: {0}\n", NumOfParticipants;
				isPrepared[payload.part] = true;
				if(ReceivedAllPrepare())
				{
					SendToAllParticipants(eCommit, (tid = transId,));
					send currentTransaction.source, eTransactionSuccess;
					CancelTimer(timer);
					goto WaitForTransactionReq;
				}
			}
		}
	}
}

eventset esCoordinatorEvents = { ePrepared, eNotPrepared, eStatusResp, eSMRResponse, eSMRLeaderUpdated};

machine Participant : ParticipantInterface, SMRReplicatedMachineInterface
sends ePrepared, eNotPrepared, eStatusResp, eParticipantCommitted, eParticipantAborted, eSMRResponse;
{
	var myId : int;
	var preparedOp: (tid: int, op: OperationType);
	var coordinator: any<esCoordinatorEvents>;
	var accountBalance: int;
	var isReplicated: bool;
	start state Init {
		entry (payload: (any<esCoordinatorEvents>, int, bool)){

			myId = payload.1;
			coordinator = payload.0;
			isReplicated = payload.2;
			raise local;
		}

		//install common handler
		on eSMRReplicatedMachineOperation do (payload:SMROperationType){
			coordinator = payload.source;
			raise payload.operation, payload.val;
		}

		on local push WaitForPrepare;
	}
	
	fun SendToCoordinator(ev: event, payload: any)
	{
		if(isReplicated)
		{
			send coordinator as SMRClientInterface, eSMRResponse, (response = ev, val = (payload as data)); 
		}
		else
		{
			send coordinator as CoorParticipantInterface, ev, payload;
		}
	}
	state WaitForPrepare {
		on ePrepare goto WaitForCommitOrAbort with (payload: (tid: int, op: OperationType))
		{
			preparedOp = payload;
			if($)
			{
				SendToCoordinator(ePrepared, (tid = payload.tid, part = myId));
			}
			else
			{
				SendToCoordinator(eNotPrepared, (tid = payload.tid,));
			}
		}
		on eCommit do { 
			print "unexpected commit message";
			assert(false); 
		}
		on eStatusQuery do { SendToCoordinator(eStatusResp, (part = myId, val = accountBalance));}
		ignore eAbort;
	}
	
	state WaitForCommitOrAbort{
		on eCommit goto WaitForPrepare with (payload: (tid: int)){
			assert(preparedOp.tid == payload.tid);
			if(preparedOp.op.op == ADD_AMOUNT)
			{
				accountBalance = accountBalance + preparedOp.op.val;
			}
			else
			{
				accountBalance = accountBalance - preparedOp.op.val;
			}
			announce eParticipantCommitted, (part = myId, tid = payload.tid);
		}
		on eAbort goto WaitForPrepare with (payload: (tid: int)){ announce eParticipantAborted, (part = myId, tid = payload.tid); }
		on ePrepare do (payload: (tid: int, op: OperationType)){
			assert(preparedOp.tid == payload.tid);
			SendToCoordinator(ePrepared, (tid = payload.tid,));
		}
	}
	
}








