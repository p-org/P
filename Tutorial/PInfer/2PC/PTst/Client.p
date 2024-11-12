/*****************************************************************************************
The client machine below implements the client of the two-phase-commit transaction service.
Each client issues N non-deterministic write-transactions,
if the transaction succeeds then it performs a read-transaction on the same key and asserts the value.
******************************************************************************************/
machine Client {
  // the coordinator machine
  var coordinator: Coordinator;
  // current transaction issued by the client
  var currTransaction : tTrans;
  // number of transactions to be issued
  var N: int;
  // uniqie client Id
  var id: int;

  start state Init {
    entry (payload : (coordinator: Coordinator, n : int, id: int)) {
      coordinator = payload.coordinator;
      N = payload.n;
      id = payload.id;
      goto SendWriteTransaction;
    }
  }

  state SendWriteTransaction {
    entry {
      currTransaction = ChooseRandomTransaction(id * 100 + N /* hack for creating unique transaction id*/);
      send coordinator, eWriteTransReq, (client = this, trans = currTransaction);
      goto ConfirmTransaction;
    }
  }

  state ConfirmTransaction {
    on eWriteTransFailure do (resp: (transId: TransactionId)) {
      if(N > 0)
      {
        N = N - 1;
        goto SendWriteTransaction;
      }
    }

    on eWriteTransTimeout do (resp: (transId: TransactionId)) {
      if(N > 0)
      {
        N = N - 1;
        goto SendWriteTransaction;
      }
    }

    on eWriteTransSuccess do (resp: (transId: TransactionId)) {
      // assert that if write transaction was successful then value read is the value written.
      send coordinator, eReadTransReq, (client= this, key = currTransaction.key);
      // await response from the participant
      receive {
        case eReadTransSuccess: (readResp: (transId: TransactionId, key: Key, val: Value)) {
          assert readResp.key == currTransaction.key && readResp.val == currTransaction.val || readResp.transId > currTransaction.transId,
            format ("Record read is not same as what was written by the client:: read - {0}, written - {1}",
          readResp.val, currTransaction.val);
        }
        case eReadTransFailure: (readResp: (key: Key)) {
          assert false, "Read transaction failed";
        }
      }
      // has more work to do?
      if(N > 0)
      {
        N = N - 1;
        goto SendWriteTransaction;
      }
    }
  }
}

/*  This is an external function (implemented in C#) to randomly choose transaction values
In P, function declarations without body are considered as foreign functions. */
fun ChooseRandomTransaction(uniqueId: int): tTrans;

// two phase commit client module
module TwoPCClient = { Client };
