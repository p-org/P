
//We implemented a fault-tolerant 2-PC protocol.
//There is a single co-ordinator and 2 participants. The two participants are two different bank accounts.

machine Coordinator
{

	var transId : int;
	var participants : map[int, machine];
	var currentTransaction: TransactionType;
	var isFaultTolerant : bool;
	start state Init {
		entry (payload: (isfaultTolerant: bool)){
			var temp : machine;
			isFaultTolerant = payload.isfaultTolerant;
			temp = new Participant(this, 0, false);
			participants[0] = temp;
			temp = new Participant(this, 1, false);
			participants[1] = temp;
			transId = 0;

			raise local;
		}

		on local push WaitForReq;
	}
	
	fun SendToParticipant(part: machine, ev: event, payload: any) {
		send part, ev, payload;
	}
	
	state WaitForReq {
		ignore eNotPrepared, ePrepared;
		on eTransaction goto ProcessTransaction with (payload : TransactionType){
			currentTransaction = payload;
			transId = transId + 1;
		}
		on eReadPartStatus do (clientS: (source: machine, part:int)){
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
			SendToParticipant(participants[i], ev, val);
			i = i + 1;
		}
	}

	fun AbortCurrentTransaction() {
		
		SendToAllParticipants(eAbort, (tid = transId,));
		send currentTransaction.source, eTransactionFailed;		
	}
	
	var prepareCount : int;
	state ProcessTransaction {
		defer eTransaction, eReadPartStatus;
		entry{
			prepareCount = 0;
			//to part1
			SendToParticipant(participants[0], ePrepare, (tid = transId, op = currentTransaction.op1));
			//to part2
			SendToParticipant(participants[1], ePrepare, (tid = transId, op = currentTransaction.op2));

		}
		
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
					goto WaitForReq;
				}
			}
		}
	}
}


machine Participant
{
	var myId : int;
	var preparedOp: (tid: int, op: OperationType);
	var coordinator: machine;
	var accountBalance: int;
	var isReplicated: bool;
	start state Init {
		entry (payload: (machine, int, bool)){

			myId = payload.1;
			coordinator = payload.0;
			isReplicated = payload.2;
			raise local;
		}

		on local push WaitForPrepare;
	}
	
	fun SendToCoordinator(ev: event, payload: any)
	{
		send coordinator, ev, payload;
	}
	state WaitForPrepare {
		on ePrepare goto WaitForCommitOrAbort with (payload: (tid: int, op: OperationType))
		{
			preparedOp = payload;
			if($$)
				SendToCoordinator(ePrepared, (tid = payload.tid,));
			else
				SendToCoordinator(eNotPrepared, (tid = payload.tid,));

			if($)
			{
				if($)
				{
					if($)
						send this, halt;
				}
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
		on ePrepare do {
			print "unexpected prepare message";
			assert(false);
		}
	}
	
}








