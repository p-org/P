//We implemented a fault-tolerant 2-PC protocol.
//There is a single co-ordinator and 2 participants. The two participants are two different bank accounts.

machine Coordinator : CoorClientInterface
receives eTransaction, eReadPartStatus, ePrepared, eNotPrepared, eStatusResp, eTimeOut, eCancelSuccess, eCancelFailure;
sends eCommit, eAbort, ePrepare, eStatusQuery, eTransactionFailed, eTransactionSuccess, eRespPartStatus, eStartTimer, eCancelTimer;
{

	var transId : int;
	var participants : map[int, ParticipantInterface];
	var timer : TimerPtr;
	var currentTransaction: TransactionType;
	start state Init {
		entry {
			var temp : ParticipantInterface;
			temp = new ParticipantInterface(this as CoorParticipantInterface, 0);
			participants[0] = temp;
			temp = new ParticipantInterface(this as CoorParticipantInterface, 1);
			participants[1] = temp;
			transId = 0;
			//create timer
			timer = CreateTimer(this as ITimerClient);

			goto WaitForReq;
		}
	}
	
	
	state WaitForReq {
		ignore eNotPrepared, ePrepared;
		on eTransaction goto ProcessTransaction with (payload : TransactionType){
			currentTransaction = payload;
			transId = transId + 1;
		}
		on eReadPartStatus do (clientS: (source: ClientInterface, part:int)){
			send participants[clientS.part], eStatusQuery;
			receive {
				case eStatusResp: (payload: ParticipantStatusType) {
					send clientS.source, eRespPartStatus, payload;
				}
			}
		}
	}
	
	fun SendToAllParticipants(ev: event, val: any)
	{
		var i : int;
		i = 0;
		while(i < sizeof(participants))
		{
			send participants[i], ev, val;
			i = i + 1;
		}
	}

	fun AbortCurrentTransaction() {
		
		SendToAllParticipants(eAbort, (tid = transId,));
		send currentTransaction.source, eTransactionFailed;
		CancelTimer(timer);
		
	}
	
	var prepareCount : int;
	state ProcessTransaction {
		defer eTransaction, eReadPartStatus;
		entry{
			prepareCount = 0;
			//to part1
			send participants[0], ePrepare, (tid = transId, op = currentTransaction.op1);
			//to part2
			send participants[1], ePrepare, (tid = transId, op = currentTransaction.op2);

			//start timer 
			StartTimer(timer, 100);
		}
		on eTimeOut do { AbortCurrentTransaction(); goto WaitForReq; }
		
		on eNotPrepared do (payload: (tid:int)){
			if(payload.tid != transId)
				return;
			else
				AbortCurrentTransaction();
		}
		
		on ePrepared do (payload: (tid: int)){
			var i : int;
			if(payload.tid == transId)
			{
				prepareCount = prepareCount + 1;
				if(prepareCount == 2)
				{
					SendToAllParticipants(eCommit, (tid = transId,));
					send currentTransaction.source, eTransactionSuccess;
					CancelTimer(timer);
					goto WaitForReq;
				}
			}
		}
	}
}


machine Participant : ParticipantInterface
receives ePrepare, eCommit, eAbort, eStatusQuery;
sends ePrepared, eNotPrepared, eStatusResp, eParticipantCommitted, eParticipantAborted;
{
	var myId : int;
	var preparedOp: (tid: int, op: OperationType);
	var coordinator: CoorParticipantInterface;
	var accountBalance: int;
	start state Init {
		entry (payload: (CoorParticipantInterface, int)){
			myId = payload.1;
			coordinator = payload.0;
			goto WaitForPrepare;
		}
		/*on SMR_RM_OPERATION do {
			client = payload.source;
			raise payload.command, payload.val;
		}
		on READ_QUERY do {
			SEND_REL(client, SMR_RESPONSE, (response = READ_RESPONSE, val = (tid = payload.tid, val = log[payload.tid])));
		}*/
	}
	
	state WaitForPrepare{
		on ePrepare goto WaitForCommitOrAbort with (payload: (tid: int, op: OperationType))
		{
			preparedOp = payload;
			if($)
				send coordinator, ePrepared, (tid = payload.tid,);
			else
				send coordinator, eNotPrepared, (tid = payload.tid,);
		}
		on eCommit do { 
			print "unexpected commit message";
			assert(false); 
		}
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
		on ePrepare do {
			print "unexpected prepare message";
			assert(false);
		}
	}
	
}








