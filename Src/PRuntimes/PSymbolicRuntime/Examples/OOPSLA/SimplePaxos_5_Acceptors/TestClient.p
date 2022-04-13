
/*****************************************************************************************
The client machine below implements client behaviors of the Paxos transaction service.
The client issues a nondeterministic number of writes, the last of which is a distinguished write of a key and value to whom equality should be tracked via the EQKEY and EQVAL predicates.
The client then continues issuing writes to other keys (that are not equal to the distinguished key) and issues reads for the distinguished key, checking that the result of the read has the same distinguished value as written.
******************************************************************************************/
machine TestClient {
    // the proposer machine
    var proposer: machine;
    // current transaction issued by the client
    var currTransaction : tRecord;

    start state Init {
         entry (payload : machine) {
             proposer = payload;
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
            send proposer, write, (client = this, rec = currTransaction);
        }
        on writeResp goto ChoosePre;
    }

    state SendSelectWrite {
        entry {
            currTransaction = (key = EQKEY, val = EQVAL);
            send proposer, write, (client = this, rec = currTransaction);
            goto SendPost;
        }
    }

    state SendPost {
        on writeResp do {
            send proposer, read, (client = this, key = EQKEY);
        }
        on readResp do (resp : tRecord) {
            var choices : seq[tPreds];
            assert (resp == (key = EQKEY, val = EQVAL)), format("value not equal {0} %s, {1} %s", resp.key, resp.val);
            choices += (0, EQVAL);
            choices += (1, NEQVAL);
            currTransaction = (key = NEQKEY, val = choose(choices));
            send proposer, write, (client = this, rec = currTransaction);
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

