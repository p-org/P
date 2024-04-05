// User defined types
type tRecord = (key: int, val: int);
type tWriteTransReq = (client: Client, rec: tRecord);
type tWriteTransResp = (transId: int, status: tTransStatus);
type tReadTransReq = (client: Client, key: int);
type tReadTransResp = (rec: tRecord, status: tTransStatus);

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
machine Client {
    // the coordinator machine
    var coordinator: Coordinator;
    // current transaction issued by the client
    var currTransaction : tRecord;
    // number of transactions issued
    var N: int;
    // current write transaction response
    var currWriteResponse: tWriteTransResp;

    start state Init {
	    entry (payload : (coor: Coordinator, n : int)) {
	        coordinator = payload.coor;
	        N = payload.n;
			goto SendWriteTransaction;
		}
	}

	state SendWriteTransaction {
	    entry {
	    	currTransaction = ChooseTransaction();
			send coordinator, eWriteTransReq, (client = this, rec = currTransaction);
		}
		on eWriteTransResp goto ConfirmTransaction;
	}

	state ConfirmTransaction {
	    entry (writeResp: tWriteTransResp) {
	        // if the write was a time out lets not confirm it
	        if(writeResp.status == TIMEOUT)
	            return;
	        currWriteResponse = writeResp;
			send coordinator, eReadTransReq, (client= this, key = currTransaction.key);
		}

		on eReadTransResp do (readResp: tReadTransResp) {
	        // assert that if write transaction failed then read must fail as well and vice-versa
	        if(currWriteResponse.status == SUCCESS)
	        {
	            assert readResp.status == currWriteResponse.status, format ("Inconsistency!");
	            // assert readResp.rec == currTransaction,
	            // format ("Record read is not same as what was written by the client:: read - {0}, written - {1}",
	            // readResp.rec, currTransaction);
	        }

	        if(N > 0)
			{
			    N = N -1;
			    goto SendWriteTransaction;
			}
	    }
	}
}


/*
This is an external function (implemented in C#) to randomly choose transaction values
In P, function declarations without body are considered as foreign functions.
*/
fun ChooseTransaction(): tRecord
{
    return (key = choose(10), val = choose(10));
}

