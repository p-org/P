// Client Machine
machine Client {
    var coordinator: machine;
    var numTransactions: int;
    var transactionsSent: int;
    var currentTransId: int;
    var currentKey: string;
    var currentValue: int;
    var pendingWrite: bool;

    start state Init {
        entry InitializeClient;
    }

    state SendWriteTransaction {
        entry SendWriteTransactionEntry;
        on eWriteTransResp do HandleWriteTransResp;
    }

    state SendReadTransaction {
        entry SendReadTransactionEntry;
        on eReadTransResp do HandleReadTransResp;
    }

    state Done {
        entry DoneEntry;
    }

    fun InitializeClient(payload: (coord: machine, numTrans: int)) {
        coordinator = payload.coord;
        numTransactions = payload.numTrans;
        transactionsSent = 0;
        goto SendWriteTransaction;
    }

    fun SendWriteTransactionEntry() {
        var trans: tWriteTransaction;
        
        if (transactionsSent >= numTransactions) {
            goto Done;
        }
        
        currentTransId = transactionsSent;
        currentKey = format("key{0}", currentTransId);
        currentValue = currentTransId * 100;
        
        trans = (key = currentKey, value = currentValue, transId = currentTransId);
        
        send coordinator, eWriteTransReq, (client = this, trans = trans);
        
        pendingWrite = true;
        transactionsSent = transactionsSent + 1;
    }

    fun HandleWriteTransResp(resp: tWriteTransResp) {
        assert resp.transId == currentTransId, 
            format("Response transaction ID {0} does not match current transaction ID {1}", 
                resp.transId, currentTransId);
        
        pendingWrite = false;
        
        if (resp.status == SUCCESS) {
            goto SendReadTransaction;
        } else {
            goto SendWriteTransaction;
        }
    }

    fun SendReadTransactionEntry() {
        send coordinator, eReadTransReq, (client = this, key = currentKey);
    }

    fun HandleReadTransResp(resp: tReadTransResp) {
        assert resp.key == currentKey, 
            format("Response key {0} does not match current key {1}", resp.key, currentKey);
        
        if (resp.status == READ_SUCCESS) {
            assert resp.value == currentValue, 
                format("Response value {0} does not match current value {1}", resp.value, currentValue);
        }
        
        goto SendWriteTransaction;
    }

    fun DoneEntry() {
        print format("Client completed all {0} transactions", numTransactions);
    }
}