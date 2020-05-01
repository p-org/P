
//We implemented a fault-tolerant 2-PC protocol.
//There is a single co-ordinator and 2 participants. The two participants are two different bank accounts.
enum TPCConfig {
	dummy = 0,
	NumOfParticipants = 2
}
machine Coordinator
sends eCommit, eAbort, ePrepare, eStatusQuery, eTransactionFailed, eTransactionSuccess, eMonitorTransactionFailed, eMonitorTransactionSuccess, eMonitorCoordinatorTimeOut, eRespPartStatus, eStartTimer, eCancelTimer, eSMROperation;
{

	var transId : int;
	var participants : map[int, machine];
	var timer : TimerPtr;
	var currentTransaction: TransactionType;
	var isFaultTolerant : bool;
	var smrOpId: int;
	start state Init {
		entry (payload: (isfaultTolerant: bool)){
			var temp : machine;
			var index : int;
			isFaultTolerant = payload.isfaultTolerant;
			smrOpId = 0;
			if(isFaultTolerant)
			{
				index = 0;
				
				while(index < NumOfParticipants)
				{
					temp = new SMRServerInterface((client = this to SMRClientInterface, reorder = false, isRoot = true, ft = FT1, id = index));
					participants[index] = temp;
					index = index + 1;
				}
			}
			
			transId = 0;
			//create timer
			timer = CreateTimer(this to ITimerClient);

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
			SendSMROperation(smrOpId, part, ev, payload, this to SMRClientInterface);
			smrOpId = smrOpId + 1;
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
		ignore eNotPrepared, ePrepared, eTimeOut;
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
		announce eMonitorTransactionFailed;
		send currentTransaction.source, eTransactionFailed;
		CancelTimer(timer);	
		goto WaitForTransactionReq;
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
			announce eMonitorCoordinatorTimeOut;
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
					announce eMonitorTransactionSuccess;
					send currentTransaction.source, eTransactionSuccess;
					CancelTimer(timer);
					goto WaitForTransactionReq;
				}
			}
		}
	}
}

eventset esCoordinatorEvents = { ePrepared, eNotPrepared, eStatusResp, eSMRResponse, eSMRLeaderUpdated};

machine Participant
sends ePrepared, eNotPrepared, eStatusResp, eParticipantCommitted, eParticipantAborted, eSMRResponse;
{
	var myId : int;
	var preparedOp: (tid: int, op: OperationType);
	var coordinator: any<esCoordinatorEvents>;
	var repData: data;
	var isReplicated: bool;
	var isLeader: bool;
	var currRespId: int;
	var currOpId : int;
	start state Init {
		entry (payload: (client:SMRClientInterface, val: data)){
			var payVal: (int, bool);
			payVal = payload.val as (int, bool);
			myId = payVal.0;
			coordinator = payload.client;
			isReplicated = payVal.1;
			isLeader = false;
			raise local;
		}

		//install common handler
		on eSMRReplicatedMachineOperation do (payload:SMRRepMachOperationType){
			currOpId = payload.smrop.clientOpId;
			currRespId = payload.respId;
			coordinator = payload.smrop.source;
			raise payload.smrop.operation, payload.smrop.val;
		}

		on eSMRReplicatedLeader do {
			isLeader = true;
		}
		on local push WaitForPrepare;
	}
	
	fun SendToCoordinator(ev: event, payload: data)
	{
		if(isReplicated)
		{
			SendSMRResponse(coordinator, ev, payload as data, currOpId, currRespId, isLeader); 
		}
		else
		{
			send coordinator to CoorParticipantInterface, ev, payload;
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
		on eStatusQuery do { SendToCoordinator(eStatusResp, (part = myId, val = repData));}
		ignore eAbort;
	}
	
	state WaitForCommitOrAbort{
		on eCommit goto WaitForPrepare with (payload: (tid: int)){
			var tempVal : data;
			assert(preparedOp.tid == payload.tid);
			repData = tempVal swap;
			PerformParticipantOp(preparedOp.op, tempVal swap);
			repData = tempVal swap;
			announce eParticipantCommitted, (part = myId, tid = payload.tid);
		}
		on eAbort goto WaitForPrepare with (payload: (tid: int)){ announce eParticipantAborted, (part = myId, tid = payload.tid); }
		on ePrepare do (payload: (tid: int, op: OperationType)){
			assert(preparedOp.tid == payload.tid);
			SendToCoordinator(ePrepared, (tid = payload.tid,));
		}
	}
}








