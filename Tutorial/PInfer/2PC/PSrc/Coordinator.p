/* User Defined Types */
// type def aiding verification
type TransactionId = int;
type Key = string;
type Value = int;

enum tTransStatus {
  SUCCESS,
  ERROR,
  TIMEOUT
}
// a transaction consisting of the key, value, and the unique transaction id.
type tTrans = (key: string, val: Value, transId: TransactionId);
// payload type associated with the `eWriteTransReq` event where `client`: client sending the
// transaction, `trans`: transaction to be committed.
type tWriteTransReq = (client: Client, trans: tTrans);
// payload type associated with the `eWriteTransResp` event where `transId` is the transaction Id
// and `status` is the return status of the transaction request.
// type tWriteTransResp = (transId: TransactionId, status: tTransStatus);
// payload type associated with the `eReadTransReq` event where `client` is the Client machine sending
// the read request and `key` is the key whose value the client wants to read.
type tReadTransReq = (client: Client, key: string);
// payload type associated with the `eReadTransResp` event where `val` is the value corresponding to
// the `key` in the read request and `status` is the read status (e.g., success or failure)
// type tReadTransResp = (transId: TransactionId, key: string, val: Value, status: tTransStatus);

/* Events used by the 2PC clients to communicate with the 2PC coordinator */
// event: write transaction request (client to coordinator)
event eWriteTransReq : tWriteTransReq;
// event: write transaction response (coordinator to client)
// event eWriteTransResp : tWriteTransResp;
event eWriteTransSuccess: (transId: TransactionId);
event eWriteTransFailure: (transId: TransactionId);
event eWriteTransTimeout: (transId: TransactionId);
// event: read transaction request (client to coordinator)
event eReadTransReq : tReadTransReq;
// event: read transaction response (participant to client)
// event eReadTransResp: tReadTransResp;
event eReadTransFailure: (key: Key);
event eReadTransSuccess: (transId: TransactionId, key: string, val: Value);

/* Events used for communication between the coordinator and the participants */
// event: prepare request for a transaction (coordinator to participant)
event ePrepareReq: tPrepareReq;
// event: prepare response for a transaction (participant to coodinator)
event ePrepareSuccess: (participant: Participant, transId: TransactionId);
event ePrepareFailure: (participant: Participant, transId: TransactionId);
// event: commit transaction (coordinator to participant)
event eCommitTrans: TransactionId;
// event: abort transaction (coordinator to participant)
event eAbortTrans: TransactionId;

/* User Defined Types */
// payload type associated with the `ePrepareReq` event
type tPrepareReq = tTrans;
// payload type assocated with the `ePrepareResp` event where `participant` is the participant machine
// sending the response, `transId` is the transaction id, and `status` is the status of the prepare
// request for that transaction.
// type tPrepareResp = (participant: Participant, transId: TransactionId, status: tTransStatus);

// event: inform participant about the coordinator
event eInformCoordinator: Coordinator;

/*****************************************************************************************
The Coordinator machine receives write and read transactions from the clients. The coordinator machine
services these transactions one by one in the order in which they were received. On receiving a write
transaction the coordinator sends prepare request to all the participants and waits for prepare
responses from all the participants. Based on the responses, the coordinator either commits or aborts
the transaction. If the coordinator fails to receive agreement from participants in time, then it
timesout and aborts the transaction. On receiving a read transaction, the coordinator randomly selects
a participant and  forwards the read request to that participant.
******************************************************************************************/
machine Coordinator
{
  // set of participants
  var participants: set[Participant];
  // current write transaction being handled
  var currentWriteTransReq: tWriteTransReq;
  // previously seen transaction ids
  var seenTransIds: set[int];
  var timer: Timer;

  start state Init {
    entry (payload: set[Participant]){
      participants = payload;
      timer = CreateTimer(this);
      // inform all participants that I am the coordinator
      BroadcastToAllParticipants(eInformCoordinator, this);
      goto WaitForTransactions;
    }
  }

  state WaitForTransactions {
    on eWriteTransReq do (wTrans : tWriteTransReq) {
      var i: int;
      if(wTrans.trans.transId in seenTransIds) // transId have to be unique
      {
        send wTrans.client, eWriteTransTimeout, (transId = wTrans.trans.transId,);
        return;
      }

      currentWriteTransReq = wTrans;
      // BroadcastToAllParticipants(ePrepareReq, wTrans.trans);
      i = 0;
      while (i < sizeof(participants)) {
        send participants[i], ePrepareReq, wTrans.trans;
        i = i + 1;
      }
      //start timer while waiting for responses from all participants
      StartTimer(timer);
      goto WaitForPrepareResponses;
    }

    on eReadTransReq do (rTrans : tReadTransReq) {
      // non-deterministically pick a participant to read from.
      send choose(participants), eReadTransReq, rTrans;
    }

    // when in this state it is fine to drop these messages as they are from the previous transaction
    ignore ePrepareFailure, ePrepareSuccess, eTimeOut;
  }

  var countPrepareResponses: int;

  state WaitForPrepareResponses {
    // defer requests, we are going to process transactions sequentially
    defer eWriteTransReq;

    on ePrepareSuccess do (resp: (participant: Participant, transId: TransactionId)) {
      // check if the response is for the current transaction else ignore it
      if (currentWriteTransReq.trans.transId == resp.transId) {
        countPrepareResponses = countPrepareResponses + 1;
        // check if we have received all responses
        if(countPrepareResponses == sizeof(participants))
        {
          DoAction(SUCCESS);
          // safe to go back and service the next transaction
          goto WaitForTransactions;
        }
      }
    }

    on ePrepareFailure do (resp: (participant: Participant, transId: TransactionId)) {
      // check if the response is for the current transaction else ignore it
      if (currentWriteTransReq.trans.transId == resp.transId) {
        DoAction(ERROR);
        // safe to go back and service the next transaction
        goto WaitForTransactions;
      }
    }

    // on timeout abort the transaction
    on eTimeOut goto WaitForTransactions with { DoAction(TIMEOUT); }

    on eReadTransReq do (rTrans : tReadTransReq) {
      // non-deterministically pick a participant to read from.
      send choose(participants), eReadTransReq, rTrans;
    }

    exit {
      countPrepareResponses = 0;
    }
  }

  fun DoAction(status: tTransStatus) {
    if(status == SUCCESS) {
      BroadcastCommit(currentWriteTransReq.trans.transId);
      send currentWriteTransReq.client, eWriteTransSuccess, (transId = currentWriteTransReq.trans.transId,);
      CancelTimer(timer);
    } else {
      BroadcastAbort(currentWriteTransReq.trans.transId);
      if(status != TIMEOUT) {
        CancelTimer(timer);
        send currentWriteTransReq.client, eWriteTransFailure, (transId = currentWriteTransReq.trans.transId,);
      } else {
        send currentWriteTransReq.client, eWriteTransTimeout, (transId = currentWriteTransReq.trans.transId,);
      }
    }
  }

  fun BroadcastAbort(transId: TransactionId) {
    var i: int;
    i = 0;
    while (i < sizeof(participants)) {
      send participants[i], eAbortTrans, transId;
      i = i + 1;
    }
  }

  fun BroadcastCommit(transId: TransactionId) {
    var i: int;
    i = 0;
    while (i < sizeof(participants)) {
      send participants[i], eCommitTrans, transId;
      i = i + 1;
    }
  }

  //function to broadcast messages to all participants
  fun BroadcastToAllParticipants(message: event, payload: any)
  {
    var i: int;
    while (i < sizeof(participants)) {
      send participants[i], message, payload;
      i = i + 1;
    }
  }
}

