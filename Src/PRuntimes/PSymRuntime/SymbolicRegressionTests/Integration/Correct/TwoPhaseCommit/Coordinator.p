/*****************************************************************************************
The coordinator machine coordinates between all the participants and
based on responses received from each participant decided to commit or abort the transaction.
It receives write and read transactions from the clients and services these transactions sequentially one by one.
******************************************************************************************/

// Events used by coordinator to communicate with the participants
event ePrepareReq: tPrepareReq;
event ePrepareResp: tPrepareResp;
event eCommitTrans: int;
event eAbortTrans: int;

type tPrepareReq = (coordinator: Coordinator, transId: int, rec: tRecord);
type tPrepareResp = (participant: Participant, transId: int, status: tTransStatus);

machine Coordinator
{
	var participants: seq[machine];
	var pendingWrTrans: tWriteTransReq;
	var currTransId:int;
	var timer: Timer;
    var countPrepareResponses: int;

	start state Init {
		entry (payload: seq[machine]) {
			var i : int;
			//initialize variables
			participants = payload;
			i = 0; currTransId = 0; timer = CreateTimer(this);
			goto WaitForTransactions;
		}

		exit {}
	}

	state WaitForTransactions {
		on eWriteTransReq do (wTrans : tWriteTransReq) {
			pendingWrTrans = wTrans;
			currTransId = currTransId + 1;
			BroadcastToAllParticipants(ePrepareReq, (coordinator = this, transId = currTransId, rec = wTrans.rec));

			//start timer while waiting for responses from all participants
			StartTimer(timer, 100);

			goto WaitForPrepareResponses;
		}

		on eReadTransReq do (rTrans : tReadTransReq) {
			// non-deterministically pick a participant to read from.
			send choose(participants), eReadTransReq, rTrans;
		}

		// when in this state it is fine to drop these messages as
		// they are from the previous transaction
		ignore ePrepareResp, eTimeOut;
	}

	state WaitForPrepareResponses {
		// we are going to process transactions sequentially
		defer eWriteTransReq;

		on ePrepareResp do (resp : tPrepareResp) {
		    // check if the response is for the current transaction else ignore it
			if (currTransId == resp.transId) {
			    if(resp.status == SUCCESS)
			    {
			        countPrepareResponses = countPrepareResponses + 1;
                    // check if we have received all responses
                    if(countPrepareResponses == sizeof(participants))
                    {
                        // lets commit the transaction
                        DoGlobalCommit();
                        // safe to go back and service the next transaction
                        goto WaitForTransactions;
                    }
			    }
			    else
			    {
			        DoGlobalAbort(ERROR);
                    // safe to go back and service the next transaction
                    goto WaitForTransactions;
			    }

			}
		}

		on eTimeOut goto WaitForTransactions with { DoGlobalAbort(TIMEOUT); }

        on eReadTransReq do (rTrans : tReadTransReq) {
            // non-deterministically pick a participant to read from.
            send choose(participants), eReadTransReq, rTrans;
        }
		exit {
			countPrepareResponses = 0;
		}
	}

	fun DoGlobalAbort(respStatus: tTransStatus) {
		// ask all participants to abort and fail the transaction
		BroadcastToAllParticipants(eAbortTrans, currTransId);
		send pendingWrTrans.client, eWriteTransResp, (transId = currTransId, status = respStatus);
		CancelTimer(timer);
	}
	fun DoGlobalCommit() {
        // ask all participants to commit and respond to client
        BroadcastToAllParticipants(eCommitTrans, currTransId);
        send pendingWrTrans.client, eWriteTransResp, (transId = currTransId, status = SUCCESS);
        CancelTimer(timer);
    }
	fun BroadcastToAllParticipants(message: event, payload: any) {
		var i: int; i = 0;
		while (i < sizeof(participants)) {
			send participants[i], message, payload;
			i = i + 1;
		}
	}
}

