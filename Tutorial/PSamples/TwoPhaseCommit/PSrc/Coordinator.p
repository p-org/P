event local_event;

/* 
The coordinator machine that communicates with the participants and 
guarantees atomicity accross participants for write transactions
*/

machine Coordinator
{
	var participants: seq[machine];
	var pendingWrTrans: tWriteTransaction;
	var currTransId:int;
	var timer: machine;

	start state Init {
		entry (payload: seq[machine]){
			var i : int; 
			//initialize variables
			participants = payload;
			i = 0; currTransId = 0;
			timer = CreateTimer(this);

			announce eMonitor_AtomicityInitialize, sizeof(payload);
			//wait for requests
			goto WaitForTransactions;
		}
	}

	state WaitForTransactions {

		on eWriteTransaction do (wTrans : tWriteTransaction) {
			pendingWrTrans = wTrans;
			currTransId = currTransId + 1;
			SendToAllParticipants(ePrepare, (coordinator = this, transId = currTransId, key = pendingWrTrans.key, val = pendingWrTrans.val));

			//start timer while waiting for responses from all participants
			StartTimer(timer, 100);

			raise local_event;
		}

		on eReadTransaction do (rTrans : tReadTransaction) {
			// non-deterministically pick a participant to read from.
			if($)
			{
				send participants[0], eReadTransaction, rTrans;
			}
			else
			{
				send participants[sizeof(participants) - 1], eReadTransaction, rTrans;
			}
		}

		on local_event push WaitForPrepareResponses;

		// when in this state it is fine to drop these messages
		ignore ePrepareSuccess, ePrepareFailed;
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
					CancelTimer(timer);
					//it is now safe to pop back to the parent state
					pop;
				}
			}
		}

		on ePrepareFailed do (transId : int) {
			if (currTransId == transId) {
				DoGlobalAbort();
				CancelTimer(timer);
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

	fun DoGlobalAbort() {
		// ask all participants to abort and fail the transaction
		SendToAllParticipants(eGlobalAbort, currTransId);
		send pendingWrTrans.client, eWriteTransFailed;
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

