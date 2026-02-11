machine Coordinator {
    var participants: seq[machine];
    var numParticipants: int;
    var currentTransId: int;
    var currentKey: string;
    var currentValue: int;
    var currentClient: machine;
    var prepareResponses: int;
    var prepareSuccess: bool;
    var timer: machine;
    var pendingWriteRequests: seq[tWriteTransReq];
    var usedTransIds: set[int];
    
    start state Init {
        entry InitEntry;
        on eInformCoordinator goto WaitForRequests;
    }
    
    state WaitForRequests {
        on eWriteTransReq do HandleWriteTransReq;
        on eReadTransReq do HandleReadTransReq;
    }
    
    state ProcessingWriteTransaction {
        entry ProcessingWriteTransactionEntry;
        on ePrepareResp do HandlePrepareResp;
        on eTimeOut do HandleTimeout;
        defer eWriteTransReq, eReadTransReq;
    }
    
    state CommitTransaction {
        entry CommitTransactionEntry;
        defer eWriteTransReq, eReadTransReq;
        ignore ePrepareResp, eTimeOut;
    }
    
    state AbortTransaction {
        entry AbortTransactionEntry;
        defer eWriteTransReq, eReadTransReq;
        ignore ePrepareResp, eTimeOut;
    }
    
    fun InitEntry(payload: (parts: seq[machine], timerMachine: machine)) {
        var i: int;
        
        numParticipants = sizeof(payload.parts);
        i = 0;
        while (i < numParticipants) {
            participants += (i, payload.parts[i]);
            i = i + 1;
        }
        
        timer = payload.timerMachine;
        currentTransId = -1;
        prepareResponses = 0;
        prepareSuccess = true;
    }
    
    fun HandleWriteTransReq(req: tWriteTransReq) {
        var i: int;
        var prepareReq: tPrepareReq;
        
        if (req.trans.transId in usedTransIds) {
            send req.client, eWriteTransResp, (transId = req.trans.transId, status = TIMEOUT);
        } else {
            currentTransId = req.trans.transId;
            currentKey = req.trans.key;
            currentValue = req.trans.value;
            currentClient = req.client;
            
            i = 0;
            while (i < numParticipants) {
                prepareReq = (key = currentKey, value = currentValue, transId = currentTransId);
                send participants[i], ePrepareReq, prepareReq;
                i = i + 1;
            }
            
            send timer, eStartTimer;
            goto ProcessingWriteTransaction;
        }
    }
    
    fun HandleReadTransReq(req: tReadTransReq) {
        var selectedParticipant: machine;
        var participantIndex: int;
        
        participantIndex = choose(numParticipants);
        selectedParticipant = participants[participantIndex];
        send selectedParticipant, eReadTransReq, (client = req.client, key = req.key);
    }
    
    fun ProcessingWriteTransactionEntry() {
        prepareResponses = 0;
        prepareSuccess = true;
    }
    
    fun HandlePrepareResp(resp: tPrepareResp) {
        if (resp.transId == currentTransId) {
            prepareResponses = prepareResponses + 1;
            
            if (resp.status != SUCCESS) {
                prepareSuccess = false;
            }
            
            if (prepareResponses == numParticipants) {
                if (prepareSuccess) {
                    goto CommitTransaction;
                } else {
                    goto AbortTransaction;
                }
            }
        }
    }
    
    fun HandleTimeout() {
        send timer, eCancelTimer;
        goto AbortTransaction;
    }
    
    fun CommitTransactionEntry() {
        var i: int;
        var commitMsg: tCommitTrans;
        
        send timer, eCancelTimer;
        
        commitMsg = (transId = currentTransId,);
        i = 0;
        while (i < numParticipants) {
            send participants[i], eCommitTrans, commitMsg;
            i = i + 1;
        }
        
        usedTransIds += (currentTransId);
        send currentClient, eWriteTransResp, (transId = currentTransId, status = SUCCESS);
        
        if (sizeof(pendingWriteRequests) > 0) {
            HandleWriteTransReq(pendingWriteRequests[0]);
            pendingWriteRequests -= (0);
        } else {
            goto WaitForRequests;
        }
    }
    
    fun AbortTransactionEntry() {
        var i: int;
        var abortMsg: tAbortTrans;
        
        send timer, eCancelTimer;
        
        abortMsg = (transId = currentTransId,);
        i = 0;
        while (i < numParticipants) {
            send participants[i], eAbortTrans, abortMsg;
            i = i + 1;
        }
        
        send currentClient, eWriteTransResp, (transId = currentTransId, status = ERROR);
        
        if (sizeof(pendingWriteRequests) > 0) {
            HandleWriteTransReq(pendingWriteRequests[0]);
            pendingWriteRequests -= (0);
        } else {
            goto WaitForRequests;
        }
    }
}