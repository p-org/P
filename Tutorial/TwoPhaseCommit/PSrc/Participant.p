machine Participant {
    var kvStore: map[int,int];
	var pendingWrTrans: tPrepareForTrans;
	var lastTransId: int;

    start state Init {
	    entry {
			lastTransId = 0;
			goto WaitForRequests;
		}
	}

	state WaitForRequests {
		on eGlobalAbort do (transId: int) {
			assert (pendingWrTrans.transId == transId);
			if (pendingWrTrans.transId == transId) {
				lastTransId = transId;
			}
		}
		
		on eGlobalCommit do (transId:int) {
			assert (pendingWrTrans.transId == transId);
			if (pendingWrTrans.transId == transId) {
				kvStore[pendingWrTrans.key] = pendingWrTrans.val;
				lastTransId = transId;
			}
		}
		
		on ePrepare do (prepareReq :tPrepareForTrans) { 
			pendingWrTrans = prepareReq;
			assert (pendingWrTrans.transId > lastTransId);
			if ($) {
				announce eMonitor_LocalCommit, (participant = this, transId = pendingWrTrans.transId);
				send prepareReq.coordinator, ePrepareSuccess, pendingWrTrans.transId;
			} else {
				send prepareReq.coordinator, ePrepareFailed, pendingWrTrans.transId;
			}
		}

		on eReadTransaction do (payload: tReadTransaction) {
			if(payload.key in kvStore)
			{
				send payload.client, eReadTransSuccess, kvStore[payload.key];
			}
			else
			{
				send payload.client, eReadTransFailed;
			}
		}
	}
}

