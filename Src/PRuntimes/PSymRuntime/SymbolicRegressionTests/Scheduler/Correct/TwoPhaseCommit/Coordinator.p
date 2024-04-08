event local_event;

/*
The coordinator machine that communicates with the participants and
guarantees atomicity accross participants for write transactions
*/

machine Coordinator
//this is list is not complete
{
	var participants: seq[machine];
	var pendingWrTrans: tWriteTransaction;
	var currTransId:int;
	var timer: machine;

	start state Init {
		entry (numParticipants : int){
			var i : int;
			//initialize variables
			i = 0; currTransId = 0;
			timer = CreateTimer(this);
			assert (numParticipants > 0);
			//create all the participants
			announce eMonitor_AtomicityInitialize, numParticipants;
			while (i < numParticipants) {
				participants += (i, new Participant(this));
				i = i + 1;
			}
			//wait for requests
			goto WaitForTransactions;
		}
	}



	state WaitForTransactions {
		// when in this state it is fine to drop these messages
		ignore ePrepareSuccess, ePrepareFailed, eTimeOut, eCancelTimerSuccess, eCancelTimerFailed;

		on eWriteTransaction do (wTrans : tWriteTransaction) {
			pendingWrTrans = wTrans;
			currTransId = currTransId + 1;
			SendToAllParticipants(ePrepare, (transId = currTransId, key = pendingWrTrans.key, val = pendingWrTrans.val));

			//start timer while waiting for responses from all participants
			StartTimer(timer, 100);

			raise local_event;
		}

		on eReadTransaction do (rTrans : tReadTransaction) {
			// todo: randomly choose a participant and read the value
		}

		on local_event push WaitForPrepareResponses;
	}

	

	fun DoGlobalAbort() {
		// ask all participants to abort and fail the transaction
		SendToAllParticipants(eGlobalAbort, currTransId);
		send pendingWrTrans.client, eWriteTransFailed;
	}

	var countPrepareResponses: int;
	state WaitForPrepareResponses {
		// we are going to process transactions sequentially
		defer eWriteTransaction;

		entry {
			countPrepareResponses = 0;
		}

		on ePrepareSuccess do (transId : int) {
			if (currTransId == transId) {
				countPrepareResponses = countPrepareResponses + 1;

				// check if we have received all responses
				if(countPrepareResponses == sizeof(participants))
				{
					//lets commit the transaction
					SendToAllParticipants(eGlobalCommit, currTransId);
					send pendingWrTrans.client, eWriteTransSuccess;
                                        send timer, eCancelTimer;
					//it is not safe to pop back to the parent state
					pop;
				}
			}
		}

		on ePrepareFailed do (transId : int) {
			if (currTransId == transId) {
				DoGlobalAbort();
                                send timer, eCancelTimer;
				pop;
			}
		}

		on eTimeOut do {
			DoGlobalAbort();
			pop;
		}

		exit {
			print "Going back to WaitForTransactions";
		}
	}

	//helper function to send messages to all replicas
	fun SendToAllParticipants(message: event, payload: any)
	{
		var i: int; i = 0;
		while (i < sizeof(participants)) {
			send participants[i], message, payload;
			i = i + 1;
		}
	}
}

