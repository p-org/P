type tTrans = (key: string, val: int, transId: int);
type tWriteTransReq = (client: Client, trans: tTrans);
type tWriteTransResp = (transId: int, status: tTransStatus);
type tReadTransReq = (client: Client, key: string);
type tReadTransResp = (key: string, val: int, status: tTransStatus);

enum tTransStatus {
  SUCCESS,
  ERROR,
  TIMEOUT
}

event eWriteTransReq : tWriteTransReq;
event eWriteTransResp : tWriteTransResp;
event eReadTransReq : tReadTransReq;
event eReadTransResp: tReadTransResp;

event ePrepareReq: tPrepareReq;
event ePrepareResp: tPrepareResp;
event eCommitTrans: int;
event eAbortTrans: int;

type tPrepareReq = tTrans;
type tPrepareResp = (participant: Participant, transId: int, status: tTransStatus);

event eInformCoordinator: Coordinator;

machine Coordinator
{
  var participants: set[Participant];
  var currentWriteTransReq: tWriteTransReq;
  var seenTransIds: set[int];
  var timer: Timer;

  start state Init {
    entry (payload: set[Participant]){
      participants = payload;
      timer = CreateTimer(this);
      BroadcastToAllParticipants(eInformCoordinator, this);
      goto WaitForTransactions;
    }
  }

  state WaitForTransactions {
    on eWriteTransReq do (wTrans : tWriteTransReq) {
      if(wTrans.trans.transId in seenTransIds) // transId have to be unique
      {
        send wTrans.client, eWriteTransResp, (transId = wTrans.trans.transId, status = TIMEOUT);
        return;
      }

      currentWriteTransReq = wTrans;
      BroadcastToAllParticipants(ePrepareReq, wTrans.trans);
      StartTimer(timer);
      goto WaitForPrepareResponses;
    }

    on eReadTransReq do (rTrans : tReadTransReq) {
      send choose(participants), eReadTransReq, rTrans;
    }

    ignore ePrepareResp, eTimeOut;
  }

  var countPrepareResponses: int;

  state WaitForPrepareResponses {
    defer eWriteTransReq;

    on ePrepareResp do (resp : tPrepareResp) {
      if (currentWriteTransReq.trans.transId == resp.transId) {
        if(resp.status == SUCCESS)
        {
          countPrepareResponses = countPrepareResponses + 1;
          if(countPrepareResponses == sizeof(participants))
          {
            DoGlobalCommit();
            goto WaitForTransactions;
          }
        }
        else
        {
          DoGlobalAbort(ERROR);
          goto WaitForTransactions;
        }
      }
    }

    on eTimeOut goto WaitForTransactions with { DoGlobalAbort(TIMEOUT); }

    on eReadTransReq do (rTrans : tReadTransReq) {
      send choose(participants), eReadTransReq, rTrans;
    }

    exit {
      countPrepareResponses = 0;
    }
  }

  fun DoGlobalAbort(respStatus: tTransStatus) {
    BroadcastToAllParticipants(eAbortTrans, currentWriteTransReq.trans.transId);
    send currentWriteTransReq.client, eWriteTransResp, (transId = currentWriteTransReq.trans.transId, status = respStatus);
    if(respStatus != TIMEOUT)
      CancelTimer(timer);
  }

  fun DoGlobalCommit() {
    BroadcastToAllParticipants(eCommitTrans, currentWriteTransReq.trans.transId);
    send currentWriteTransReq.client, eWriteTransResp,
      (transId = currentWriteTransReq.trans.transId, status = SUCCESS);
    CancelTimer(timer);
  }

  fun BroadcastToAllParticipants(message: event, payload: any)
  {
    var i: int;
    while (i < sizeof(participants)) {
      send participants[i], message, payload;
      i = i + 1;
    }
  }
}

