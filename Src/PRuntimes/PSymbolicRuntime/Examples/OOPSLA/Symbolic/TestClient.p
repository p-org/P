// User defined types
type tRecord = (key: tPreds, val: tPreds);
type tWriteTransReq = (client: TestClient, rec: tRecord);
type tWriteTransResp = (transId: tPreds, status: tTransStatus);
type tReadTransReq = (client: TestClient, key: tPreds);
type tReadTransResp = (rec: tRecord, status: tTransStatus);

pred enum tPreds {
    EQKEY,
    EQVAL,
    NEQKEY,
    NEQVAL
}

enum tTransStatus {
    SUCCESS,
    ERROR,
    TIMEOUT
}

// Events used by client machine to communicate with the two phase commit coordinator
event eWriteTransReq : tWriteTransReq;
event eWriteTransResp : tWriteTransResp;
event eReadTransReq : tReadTransReq;
event eReadTransResp: tReadTransResp;

/*****************************************************************************************
The client machine below implements the client of the two-phase-commit transaction service.
Each client issues N non-deterministic write-transaction of (key, value),
if the transaction succeeds then it performs a read-transaction on the same key and asserts the value.
******************************************************************************************/
machine TestClient {
    // the coordinator machine
    var coordinator: Coordinator;
    // current transaction issued by the client
    var currTransaction : tRecord;
    // number of transactions issued
    var N: int;
    // current write transaction response
    var currWriteResponse: tWriteTransResp;

    start state Init {
         entry (payload : Coordinator) {
             coordinator = payload;
             goto ChoosePre;
         }
    }

    state ChoosePre {
        entry {
            if ($) {
                goto SendPreWrites;
            } else {
                goto SendSelectWrite;
            }
        }
    }

    state SendPreWrites {
        entry {
            currTransaction = ChooseTransaction();
            send coordinator, eWriteTransReq, (client = this, rec = currTransaction);
        }
        on eWriteTransResp goto ChoosePre;
    }

    state SendSelectWrite {
        entry {
            currTransaction = (key = EQKEY, val = EQVAL);
            send coordinator, eWriteTransReq, (client = this, rec = currTransaction);
            goto SendPost;
        }
    }

    state SendPost {
        on eWriteTransResp do {
            if ($) {
               currTransaction = (key = NEQKEY, val = EQVAL);
               if ($) {
                   currTransaction = (key = NEQKEY, val = NEQVAL);
               }
               send coordinator, eWriteTransReq, (client = this, rec = currTransaction);
            }
        }
    }

}


/* 
Randomly choose a transaction
*/
fun ChooseTransaction(): tRecord
{
    //var choices : seq[tPreds];
    //choices += (0, EQKEY);
    //choices += (1, NEQKEY);
    //choices += (2, EQVAL);
    //choices += (3, NEQVAL);
    var choices : tPreds;
    choices = EQKEY;
    choices = NEQKEY;
    choices = EQVAL;
    choices = NEQVAL;

//    return (key = choose(choices), val = choose(choices));
    return (key = choices, val = choices);
}

