machine Participant {
  var kvStore: map[string, tTrans];
  var pendingWriteTrans: map[tTransId, tTrans];
  var coordinator: Coordinator;

  start state Init {
    entry (coor: Coordinator) {
      coordinator = coor;
    }
  }
  state WaitForRequests {

    on eAbortTrans do (transId: tTransId) {
      assert transId in pendingWriteTrans;
      // remove the transaction from the pending transactions set
      pendingWriteTrans -= (transId);
    }

    on eCommitTrans do (transId: tTransId) {
      assert transId in pendingWriteTrans;
      kvStore[pendingWriteTrans[transId].key] = pendingWriteTrans[transId];
      // remove the transaction from the pending transactions set
      pendingWriteTrans -= (transId);
    }

    on ePrepareReq do (prepareReq :tPrepareReq) {
      assert !(prepareReq.transId in pendingWriteTrans);
      pendingWriteTrans[prepareReq.transId] = prepareReq;
      if (!(prepareReq.key in kvStore) || (prepareReq.key in kvStore && prepareReq.transId.count > kvStore[prepareReq.key].transId.count)) {
        send coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = SUCCESS);
      } else {
        send coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = ERROR);
      }
    }
  }
}

