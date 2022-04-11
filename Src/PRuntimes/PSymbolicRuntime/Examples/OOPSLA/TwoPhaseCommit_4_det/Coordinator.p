/*****************************************************************************************
The coordinator machine coordinates between all the participants and
based on responses received from each participant decided to commit or abort the transaction.
It receives write and read transactions from the clients and services these transactions sequentially one by one.
******************************************************************************************/

// Events used by coordinator to communicate with the participants
event ePrepareReq: tPrepareReq;
event ePrepareResp: tPrepareResp;
event eCommitTrans: tPreds;

type tPrepareReq = (coordinator: Coordinator, transId: tPreds, rec: tRecord);
type tPrepareResp = (participant: Participant, transId: tPreds, status: tTransStatus);

machine Coordinator
{
	var participants: seq[machine];
	var pendingWrTrans: tWriteTransReq;
	var currTransId: tPreds;
        var countPrepareResponses: int;
        var choices : seq[tPreds];

	start state Init {
		entry (payload: seq[machine]) {
			//initialize variables
			participants = payload;
                        choices += (0, EQKEY);
                        choices += (1, NEQKEY);
                        choices += (2, EQVAL);
                        choices += (3, NEQVAL);
			currTransId = choose(choices);
			goto WaitForTransactions;
		}

		exit {}
	}

	state WaitForTransactions {
		on eWriteTransReq do (wTrans : tWriteTransReq) {
			pendingWrTrans = wTrans;
			currTransId = choose(choices);
			BroadcastToAllParticipants(ePrepareReq, (coordinator = this, transId = currTransId, rec = wTrans.rec));

			goto WaitForPrepareResponses;
		}

		on eReadTransReq do (rTrans : tReadTransReq) {
			// non-deterministically pick a participant to read from.
			send choose(participants), eReadTransReq, rTrans;
		}

		// when in this state it is fine to drop these messages as
		// they are from the previous transaction
		ignore ePrepareResp;
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
			    //else
			   // {
			    //    DoGlobalAbort(ERROR);
              // safe to go back and service the next transaction
           //   goto WaitForTransactions;
	//		    }

			}
		}

        on eReadTransReq do (rTrans : tReadTransReq) {
            // non-deterministically pick a participant to read from.
            send choose(participants), eReadTransReq, rTrans;
        }
		exit {
			countPrepareResponses = 0;
		}
	}

	fun DoGlobalCommit() {
        // ask all participants to commit and respond to client
        BroadcastToAllParticipants(eCommitTrans, currTransId);
        send pendingWrTrans.client, eWriteTransResp, (transId = currTransId, status = SUCCESS);
    }
	fun BroadcastToAllParticipants(message: event, payload: any) {
		var i: int; i = 0;
		while (i < sizeof(participants)) {
			send participants[i], message, payload;
			i = i + 1;
		}
	}
}

