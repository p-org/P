machine Participant {
  var kvStore: map[string, tTrans];
  var pendingWriteTrans: map[int, tTrans];
  var coordinator: Coordinator;

  start state Init {
    on eInformCoordinator goto WaitForRequests with (coor: Coordinator) {
      coordinator = coor;
    }
  }

  state WaitForRequests {
    on eAbortTrans do (transId: int) {
      assert transId in pendingWriteTrans;
      // remove the transaction from the pending transactions set
      pendingWriteTrans -= (transId);
    }

    on eCommitTrans do (transId:int) {
      assert transId in pendingWriteTrans;
      kvStore[pendingWriteTrans[transId].key] = pendingWriteTrans[transId];
      // remove the transaction from the pending transactions set
      pendingWriteTrans -= (transId);
    }

    on ePrepareReq do (prepareReq :tPrepareReq) {
      assert !(prepareReq.transId in pendingWriteTrans);
      pendingWriteTrans[prepareReq.transId] = prepareReq;
      if (!(prepareReq.key in kvStore) || (prepareReq.key in kvStore && prepareReq.transId > kvStore[prepareReq.key].transId)) {
        send coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = SUCCESS);
      } else {
        send coordinator, ePrepareResp, (participant = this, transId = prepareReq.transId, status = ERROR);
      }
    }

    on eReadTransReq do (req: tReadTransReq) {
      if(req.key in kvStore)
      {
        send req.client, eReadTransResp, (key = req.key, val = kvStore[req.key].val, status = SUCCESS);
      }
      else
      {
        send req.client, eReadTransResp, (key = "", val = -1, status = ERROR);
      }
    }
  }
}

