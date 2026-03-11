machine Participant {
    var coordinator: machine;
    var dataStore: map[string, int];
    var pendingTransactions: map[int, (key: string, value: int)];
    
    start state Init {
        on eInformCoordinator do HandleInformCoordinator;
    }
    
    state WaitForRequests {
        on ePrepareReq do HandlePrepareReq;
        on eCommitTrans do HandleCommitTrans;
        on eAbortTrans do HandleAbortTrans;
        on eReadTransReq do HandleReadTransReq;
    }
    
    fun HandleInformCoordinator(payload: tInformCoordinator) {
        coordinator = payload.coordinator;
        goto WaitForRequests;
    }
    
    fun HandlePrepareReq(req: tPrepareReq) {
        var status: tTransStatus;
        var resp: tPrepareResp;
        
        if (!(req.transId in pendingTransactions)) {
            pendingTransactions[req.transId] = (key = req.key, value = req.value);
        }
        
        if (!(req.key in dataStore)) {
            status = SUCCESS;
        } else {
            if (req.transId > dataStore[req.key]) {
                status = SUCCESS;
            } else {
                status = ERROR;
            }
        }
        
        resp = (participant = this, transId = req.transId, status = status);
        send coordinator, ePrepareResp, resp;
    }
    
    fun HandleCommitTrans(msg: tCommitTrans) {
        var trans: (key: string, value: int);
        
        assert msg.transId in pendingTransactions,
            format("Commit request received for non-pending transaction ID {0}", msg.transId);
        
        trans = pendingTransactions[msg.transId];
        dataStore[trans.key] = trans.value;
        pendingTransactions -= (msg.transId);
    }
    
    fun HandleAbortTrans(msg: tAbortTrans) {
        assert msg.transId in pendingTransactions,
            format("Abort request received for non-pending transaction ID {0}", msg.transId);
        
        pendingTransactions -= (msg.transId);
    }
    
    fun HandleReadTransReq(req: tReadTransReq) {
        var readResp: tReadTransResp;
        
        if (req.key in dataStore) {
            readResp = (key = req.key, value = dataStore[req.key], status = READ_SUCCESS);
            send req.client, eReadTransResp, readResp;
        } else {
            readResp = (key = req.key, value = 0, status = READ_ERROR);
            send req.client, eReadTransResp, readResp;
        }
    }
}