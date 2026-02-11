// Safety Specification: Atomicity
// If a transaction is committed by the coordinator, then it was agreed on by all participants.
// If the transaction is aborted, then at least one participant must have rejected the transaction.

spec Atomicity observes ePrepareResp, eCommitTrans, eAbortTrans {
    var currentTransaction: int;
    var prepareResponses: map[int, map[machine, tTransStatus]];
    var transactionDecision: map[int, tTransStatus];
    
    start state WaitingForEvents {
        on ePrepareResp do HandlePrepareResp;
        on eCommitTrans do HandleCommit;
        on eAbortTrans do HandleAbort;
    }
    
    fun HandlePrepareResp(resp: tPrepareResp) {
        var participantResponses: map[machine, tTransStatus];
        
        if (resp.transId in prepareResponses) {
            participantResponses = prepareResponses[resp.transId];
            participantResponses[resp.participant] = resp.status;
            prepareResponses[resp.transId] = participantResponses;
        } else {
            participantResponses[resp.participant] = resp.status;
            prepareResponses[resp.transId] = participantResponses;
        }
    }
    
    fun HandleCommit(msg: tCommitTrans) {
        var participantResponses: map[machine, tTransStatus];
        var participant: machine;
        var allSuccess: bool;
        
        assert msg.transId in prepareResponses,
            format("Commit decision for transaction {0} without any prepare responses", msg.transId);
        
        participantResponses = prepareResponses[msg.transId];
        allSuccess = true;
        
        foreach (participant in keys(participantResponses)) {
            if (participantResponses[participant] != SUCCESS) {
                allSuccess = false;
            }
        }
        
        assert allSuccess,
            format("Transaction {0} committed but not all participants agreed", msg.transId);
        
        transactionDecision[msg.transId] = SUCCESS;
    }
    
    fun HandleAbort(msg: tAbortTrans) {
        var participantResponses: map[machine, tTransStatus];
        var participant: machine;
        var someRejected: bool;
        
        if (msg.transId in prepareResponses) {
            participantResponses = prepareResponses[msg.transId];
            someRejected = false;
            
            foreach (participant in keys(participantResponses)) {
                if (participantResponses[participant] != SUCCESS) {
                    someRejected = true;
                }
            }
        }
        
        transactionDecision[msg.transId] = ERROR;
    }
}

// Liveness Specification: Progress
// Every transaction request from a client must be eventually responded to.

spec Progress observes eWriteTransReq, eWriteTransResp {
    var pendingTransactions: set[int];
    
    start state NoPendingTransactions {
        on eWriteTransReq goto PendingTransactions with HandleWriteRequest;
    }
    
    hot state PendingTransactions {
        on eWriteTransReq goto PendingTransactions with HandleWriteRequest;
        on eWriteTransResp do HandleWriteResponse;
    }
    
    fun HandleWriteRequest(req: tWriteTransReq) {
        pendingTransactions += (req.trans.transId);
    }
    
    fun HandleWriteResponse(resp: tWriteTransResp) {
        assert resp.transId in pendingTransactions,
            format("Response for transaction {0} which was not requested", resp.transId);
        
        pendingTransactions -= (resp.transId);
        
        if (sizeof(pendingTransactions) == 0) {
            goto NoPendingTransactions;
        }
    }
}