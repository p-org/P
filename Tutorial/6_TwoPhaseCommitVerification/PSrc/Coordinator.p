type tTransId = (client: Client, count: int);
type tTrans = (key: string, val: int, transId: tTransId);
type tWriteTransReq = (client: Client, trans: tTrans);
type tWriteTransResp = (transId: tTransId, status: tTransStatus);

enum tTransStatus {
  SUCCESS,
  ERROR,
  TIMEOUT
}

event eWriteTransReq : tWriteTransReq;
event eWriteTransResp : tWriteTransResp;

event ePrepareReq: tPrepareReq;
event ePrepareResp: tPrepareResp;
event eCommitTrans: tTransId;
event eAbortTrans: tTransId;

type tPrepareReq = tTrans;
type tPrepareResp = (participant: Participant, transId: tTransId, status: tTransStatus);

event eInformCoordinator: Coordinator;

machine Coordinator
{
  var participants: set[Participant];
  var agreed: set[Participant];
  var currentWriteTransReq: tWriteTransReq;
  var seenTransIds: set[tTransId];

  start state Init {
    entry (payload: set[Participant]) {
      var p: Participant;
      
      participants = payload;
      
      foreach (p in participants) {
        send p, eInformCoordinator,  this;
 
      }
      
      goto WaitForTransactions;
    }
  }

  state WaitForTransactions {
    on eWriteTransReq do (wTrans : tWriteTransReq) {
      var p: Participant;
      
      // transId have to be unique
      if(wTrans.trans.transId in seenTransIds) {
        send wTrans.client, eWriteTransResp, (transId = wTrans.trans.transId, status = TIMEOUT);
      } else {
          currentWriteTransReq = wTrans;
          
          foreach (p in participants) {
            send p, ePrepareReq,  wTrans.trans;
     
          }
          
          goto WaitForPrepareResponses;
      }
    }
  }

  state WaitForPrepareResponses {

    on ePrepareResp do (resp : tPrepareResp) {
      if (currentWriteTransReq.trans.transId == resp.transId) {
        if(resp.status == SUCCESS)
        {
          agreed += (resp.participant);
          if(agreed == participants)
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

    exit {
      agreed = default(set[Participant]);
    }
  }

  fun DoGlobalAbort(respStatus: tTransStatus) {
    var p: Participant;
    foreach (p in participants) {
      send p, eAbortTrans, currentWriteTransReq.trans.transId;
    }
    
    send currentWriteTransReq.client, eWriteTransResp, (transId = currentWriteTransReq.trans.transId, status = respStatus);
  }

  fun DoGlobalCommit() {
    var p: Participant;
    foreach (p in participants) {
      send p, eCommitTrans, currentWriteTransReq.trans.transId;
    }
    
    send currentWriteTransReq.client, eWriteTransResp,
      (transId = currentWriteTransReq.trans.transId, status = SUCCESS);
  }
}