machine Client {
  var coordinator: Coordinator;
  var currTransaction: tTrans;
  var count: int;

  start state Init {
    entry (payload : (coordinator: Coordinator, count: int)) {
      coordinator = payload.coordinator;
      count = payload.count;
      goto SendWriteTransaction;
    }
  }

  state SendWriteTransaction {
    entry {
      if ($) {
          currTransaction = CreateRandomTransaction();
          // subexpr     . entry  = rhs
          currTransaction.transId = (client = this, count = count);
          send coordinator, eWriteTransReq, (client = this, trans = currTransaction);
      } else {
        goto SendWriteTransaction;
      }
    }
    on eWriteTransResp do (writeResp: tWriteTransResp) {
      goto SendWriteTransaction;
    }
  }
}

fun CreateRandomTransaction(): tTrans;