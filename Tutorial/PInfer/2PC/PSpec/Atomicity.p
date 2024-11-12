// event: initialize the AtomicityInvariant spec monitor
event eMonitor_AtomicityInitialize: (numParticipants: int);

/**********************************
We would like to assert the atomicity property that:
if a transaction is committed by the coordinator then it was agreed on by all participants
***********************************/
spec AtomicityInvariant observes eMonitor_AtomicityInitialize, eWriteTransFailure, eWriteTransSuccess, ePrepareFailure, ePrepareSuccess
{
  // a map from transaction id to a map from responses status to number of participants with that response
  var participantsResponse: map[int, map[tTransStatus, TransactionId]];
  var numParticipants: int;
  start state Init {
    on eMonitor_AtomicityInitialize goto WaitForEvents with (n: (numParticipants: int)) {
      numParticipants = n.numParticipants;
    }
  }

  state WaitForEvents {

    on ePrepareFailure do (resp: (participant: Participant, transId: TransactionId)) {
        var transId: TransactionId;
        transId = resp.transId;

        if (!(transId in participantsResponse)) {
            participantsResponse[transId] = default(map[tTransStatus, TransactionId]);
            participantsResponse[transId][SUCCESS] = 0;
            participantsResponse[transId][ERROR] = 0;
        }
        participantsResponse[transId][ERROR] = participantsResponse[transId][ERROR] + 1;
    }

    on ePrepareSuccess do (resp: (participant: Participant, transId: TransactionId)) {
        var transId: TransactionId;
        transId = resp.transId;

        if (!(transId in participantsResponse)) {
            participantsResponse[transId] = default(map[tTransStatus, TransactionId]);
            participantsResponse[transId][SUCCESS] = 0;
            participantsResponse[transId][ERROR] = 0;
        }
        participantsResponse[transId][SUCCESS] = participantsResponse[transId][SUCCESS] + 1;
    }

    on eWriteTransSuccess do (resp: (transId: TransactionId)) {
        assert resp.transId in participantsResponse;
        assert participantsResponse[resp.transId][SUCCESS] == numParticipants;
        participantsResponse -= (resp.transId);
    }

    on eWriteTransFailure do (resp: (transId: TransactionId)) {
        assert resp.transId in participantsResponse;
        assert participantsResponse[resp.transId][ERROR] > 0;
        participantsResponse -= (resp.transId);
    }
  }
}

/**************************************************************************
Every received transaction from a client must be eventually responded back.
Note, the usage of hot and cold states.
***************************************************************************/
spec Progress observes eWriteTransTimeout, eWriteTransSuccess, eWriteTransFailure, eWriteTransReq {
  var pendingTransactions: int;
  start state Init {
    on eWriteTransReq goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
  }

  hot state WaitForResponses
  {
    on eWriteTransFailure do {
      pendingTransactions = pendingTransactions - 1;
      if(pendingTransactions == 0)
      {
        goto AllTransactionsFinished;
      }
    }

    on eWriteTransTimeout do {
      pendingTransactions = pendingTransactions - 1;
      if(pendingTransactions == 0)
      {
        goto AllTransactionsFinished;
      }
    }

    on eWriteTransSuccess do {
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
