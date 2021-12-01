/*****************************************************************************************
The participant machine below maintains a key value store which is updated based on the
transactions accepted by the coordinator. On receiving a prepare-for-transaction request
from the coordinator, the participant non-deterministically chooses to allow or disallow the
transaction.
******************************************************************************************/

machine Participant {
    // local key value store
    var kvStore: map[int,int];
    // pending write transactions that have not been committed yet.
	var pendingWriteTrans: map[int, tRecord];

    start state Init {
	    entry {
			goto WaitForRequests;
		}
	}

	state WaitForRequests {
		on sync_eAbortTrans do (transId: int) {
			// remove the transaction from the pending set
			assert transId in pendingWriteTrans,
			format ("Abort request for a non-pending transaction, transId: {0}, pendingTrans: {1}", transId, pendingWriteTrans);
			pendingWriteTrans -= transId;
		}
		on sync_eCommitTrans do (transId:int) {
            var transaction: tRecord;
            assert transId in pendingWriteTrans,
            format ("Commit request for a non-pending transaction, transId: {0}, pendingTrans: {1}", transId, pendingWriteTrans);
            transaction = pendingWriteTrans[transId];
            kvStore[transaction.key] = transaction.val;
        }

		on sync_ePrepareReq do (prepareReq :tPrepareReq) {
			// add the transaction to the pending set
			assert !(prepareReq.transId in pendingWriteTrans),
			format ("Duplicate transaction ids not allowed!, received transId: {0}, pending transactions: {1}", prepareReq.transId, pendingWriteTrans);
			pendingWriteTrans[prepareReq.transId] = prepareReq.rec;

			// non-deterministically
			if ($) {
				send prepareReq.coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = SUCCESS);
			} else {
				send prepareReq.coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = ERROR);
			}
		}

		on eReadTransReq do (req: tReadTransReq) {
			if(req.key in kvStore)
			{
				send req.client, sync_eReadTransResp, (rec = (key = req.key, val = kvStore[req.key]), status = SUCCESS);
			}
			else
			{
				send req.client, sync_eReadTransResp, (rec = default(tRecord), status = ERROR);
			}
		}
	}
}

