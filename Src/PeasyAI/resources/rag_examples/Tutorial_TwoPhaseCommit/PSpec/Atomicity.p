// event: initialize the AtomicityInvariant spec monitor
event eMonitor_AtomicityInitialize: int;

/**********************************
We would like to assert the atomicity property that:
if a transaction is committed by the coordinator then it was agreed on by all participants
***********************************/
spec AtomicityInvariant observes eWriteTransResp, ePrepareResp, eMonitor_AtomicityInitialize
{
  // a map from transaction id to a map from responses status to number of participants with that response
  var participantsResponse: map[int, map[tTransStatus, int]];
  var numParticipants: int;
  start state Init {
    on eMonitor_AtomicityInitialize goto WaitForEvents with (n: int) {
      numParticipants = n;
    }
  }

  state WaitForEvents {
    on ePrepareResp do (resp: tPrepareResp){
      var transId: int;
      transId = resp.transId;

      if(!(transId in participantsResponse))
      {
        participantsResponse[transId] = default(map[tTransStatus, int]);
        participantsResponse[transId][SUCCESS] = 0;
        participantsResponse[transId][ERROR] = 0;
      }
      participantsResponse[transId][resp.status] = participantsResponse[transId][resp.status] + 1;
    }

    on eWriteTransResp do (resp: tWriteTransResp) {
      assert (resp.transId in participantsResponse || resp.status == TIMEOUT),
      format ("Write transaction was responded to the client without receiving any responses from the participants!");

      if(resp.status == SUCCESS)
      {
        assert participantsResponse[resp.transId][SUCCESS] == numParticipants,
        format ("Write transaction was responded as committed before receiving success from all participants. ") +
        format ("participants sent success: {0}, participants sent error: {1}", participantsResponse[resp.transId][SUCCESS],
        participantsResponse[resp.transId][ERROR]);
      }
      else if(resp.status == ERROR)
      {
        assert participantsResponse[resp.transId][ERROR] > 0,
          format ("Write transaction {0} was responded as failed before receiving error from atleast one participant.", resp.transId) +
          format ("participants sent success: {0}, participants sent error: {1}", participantsResponse[resp.transId][SUCCESS],
            participantsResponse[resp.transId][ERROR]);
      }
      // remove the transaction information
      participantsResponse -= (resp.transId);
    }
  }
}

/**************************************************************************
Every received transaction from a client must be eventually responded back.
Note, the usage of hot and cold states.
***************************************************************************/
spec Progress observes eWriteTransReq, eWriteTransResp {
  var pendingTransactions: int;
  start state Init {
    on eWriteTransReq goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
  }

  hot state WaitForResponses
  {
    on eWriteTransResp do {
      pendingTransactions = pendingTransactions - 1;
      if(pendingTransactions == 0)
      {
        goto AllTransactionsFinished;
      }
    }

    on eWriteTransReq do { pendingTransactions = pendingTransactions + 1; }
  }

  cold state AllTransactionsFinished {
    on eWriteTransReq goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
  }
}
