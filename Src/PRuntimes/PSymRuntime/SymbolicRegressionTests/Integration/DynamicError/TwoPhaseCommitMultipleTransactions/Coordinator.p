event local_write_event;
event local_read_event;

/*
The coordinator machine that communicates with the participants and
guarantees atomicity accross participants for write transactions
*/

machine Coordinator
{
	var participants: seq[machine];
	var pendingWrTrans: tWriteTransaction;
	var pendingRTrans: tReadTransaction;
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



        var idx : int;
        var picked : int;
	state WaitForTransactions {
		// when in this state it is fine to drop these messages
		ignore ePrepareSuccess, ePrepareFailed, eTimeOut, eCancelTimerSuccess, eCancelTimerFailed;

		on eWriteTransaction do (wTrans : tWriteTransaction) {
			pendingWrTrans = wTrans;
			currTransId = currTransId + 1;
			SendToAllParticipants(ePrepare, (transId = currTransId, key = pendingWrTrans.key, val = pendingWrTrans.val));

			//start timer while waiting for responses from all participants
			StartTimer(timer, 100);

			raise local_write_event;
		}

		on eReadTransaction do (rTrans : tReadTransaction) {
                        pendingRTrans = rTrans;
                        if (sizeof(participants) == 0) {
                            send pendingRTrans.client, eReadTransUnavailable;
                        } else {
		             //randomly choose a participant and read the value
                             idx = 0;
                             picked = 0;
                             while (idx < sizeof(participants)) {
                                if ($) {
                                    picked = idx;
                                }
                                idx = idx + 1;
                             }
                            send participants[picked], eRead, pendingRTrans.key;
                            raise local_read_event;
                        }
		}

                on local_read_event goto WaitForReadResponses;

		on local_write_event goto WaitForPrepareResponses;
	}

        state WaitForReadResponses {
            defer ePrepareSuccess, ePrepareFailed, eTimeOut, eReadTransaction, eWriteTransaction;
            on eReadSuccess do (result:int) {
                send pendingRTrans.client, eReadTransSuccess, result;
            }
            on eReadFailed do {
                send pendingRTrans.client, eReadTransFailed;
            }
        }

	fun DoGlobalAbort() {
		// ask all participants to abort and fail the transaction
		SendToAllParticipants(eGlobalAbort, currTransId);
		send pendingWrTrans.client, eWriteTransFailed;
	}

	var countPrepareResponses: int;
	state WaitForPrepareResponses {
		// we are going to process transactions sequentially
		defer eWriteTransaction, eReadTransaction;

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
				}
			}
		}

		on ePrepareFailed do (transId : int) {
			if (currTransId == transId) {
				DoGlobalAbort();
                                send timer, eCancelTimer;
			}
		}

		on eTimeOut do {
			DoGlobalAbort();
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

