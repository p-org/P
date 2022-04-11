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
The client machine below implements client behaviors of the two-phase-commit transaction service.
The client issues a nondeterministic number of writes, the last of which is a distinguished write of a key and value to whom equality should be tracked via the EQKEY and EQVAL predicates.
The client then continues issuing writes to other keys (that are not equal to the distinguished key) and issues reads for the distinguished key, checking that the result of the read has the same distinguished value as written.
******************************************************************************************/
machine TestClient {
    // the coordinator machine
    var coordinator: Coordinator;
    // current transaction issued by the client
    var currTransaction : tRecord;
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
            send coordinator, eReadTransReq, (client = this, key = EQKEY);
        }
        on eReadTransResp do (resp : tReadTransResp) {
            var choices : seq[tPreds];
            if (resp.status == SUCCESS) {
                assert (resp.rec == (key = EQKEY, val = EQVAL)), format("value not equal {0} %s, {1} %s", resp.rec.key, resp.rec.val);
            }
            choices += (0, EQVAL);
            choices += (1, NEQVAL);
            currTransaction = (key = NEQKEY, val = choose(choices));
            send coordinator, eWriteTransReq, (client = this, rec = currTransaction);
        }
    }

}


/* 
Randomly choose a transaction
*/
fun ChooseTransaction(): tRecord
{
    var keyChoices : seq[tPreds];
    var valChoices : seq[tPreds];
    keyChoices += (0, EQKEY);
    keyChoices += (1, NEQKEY);
    valChoices += (0, EQVAL);
    valChoices += (1, NEQVAL);

    return (key = choose(keyChoices), val = choose(valChoices));
}

