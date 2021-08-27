/*****************************************************************************************
The participant machine below maintains a local key value store which is updated based on the
transactions accepted by the coordinator. On receiving a prepare-for-transaction request
from the coordinator, the participant non-deterministically chooses to allow or disallow the
transaction.
******************************************************************************************/

machine Participant {
    // local key value store
    var kvStore: map[string, int];
    // pending write transactions that have not been committed or aborted.
	var pendingWriteTrans: map[int, tRecord];

    start state Init {
	    entry {
			goto WaitForRequests;
		}
	}

	state WaitForRequests {
		on eAbortTrans do (transId: int) {
			// check the precondition that all abort transaction requests are received for
			// pending transactions only.
			assert transId in pendingWriteTrans,
			format ("Abort request for a non-pending transaction, transId: {0}, pendingTrans set: {1}", transId, pendingWriteTrans);

			// remove the transaction from the pending transactions set
			pendingWriteTrans -= transId;
		}
		
		on eCommitTrans do (transId:int) {
		    // check the precondition that all commit transaction requests are received for
            // pending transactions only.
            assert transId in pendingWriteTrans,
            format ("Commit request for a non-pending transaction, transId: {0}, pendingTrans set: {1}", transId, pendingWriteTrans);

            // commit the transaction locally
            kvStore[pendingWriteTrans[transId].key] = pendingWriteTrans[transId].val;

            // remove the transaction from the pending transactions set
            pendingWriteTrans -= transId;
        }

		on ePrepareReq do (prepareReq :tPrepareReq) {
			// add the transaction to the pending transactions set
			assert !(prepareReq.transId in pendingWriteTrans),
			format ("Duplicate transaction ids not allowed!, received transId: {0}, pending transactions: {1}", prepareReq.transId, pendingWriteTrans);
			pendingWriteTrans[prepareReq.transId] = prepareReq.rec;

			// non-deterministically pick whether to accept or reject the transaction
			if ($) {
				send prepareReq.coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = SUCCESS);
			} else {
				send prepareReq.coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = ERROR);
			}
		}

		on eReadTransReq do (req: tReadTransReq) {
			if(req.key in kvStore)
			{
			    // read successful as the key exists
				send req.client, eReadTransResp, (rec = (key = req.key, val = kvStore[req.key]), status = SUCCESS);
			}
			else
			{
			    // read failed as the key does not exist
				send req.client, eReadTransResp, (rec = default(tRecord), status = ERROR);
			}
		}
	}
}

