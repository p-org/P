// Events used to communicate between the server and helper
event eHelperReq: int;
event eHelperResp: tResponseStatus;

/*************************************************************
Server receives eRequest event from the client and performs local computation.
Based on the local computation using a helper, server responds with either eReqSuccessful or eReqFailed.
Server responds to requests in the order in which they were received.
*************************************************************/
machine BankServer
{
  var database: BackEndDatabase;
  start state Init {
    entry {
      helper = new BackEndDatabase((server = this, initialBalance:);
      goto WaitForRequests;
    }
  }

  state WaitForRequests {
    on eRequest do (req: tRequest){
      send helper, eHelperReq, req.rId;
      receive {
        case eHelperResp: (respStatus: tResponseStatus){
          send req.source, eResponse, (rId = req.rId, status = respStatus);
        }
      }
    }
  }
}

/***************************************************************
The helper machine performs some random computation and returns
either eReqSuccessful or eReqFailed.
****************************************************************/
machine BackEndDatabase
{
  var server: BankServer;
  var bankBalance: map[]
  start state Init {
    entry(payload : Server){
      server = payload;
    }
    on eHelperReq do (reqId: int){
      // helper machine is a gambler, it does a random choice between 0-50
      // if the received reqId is less than the picked value it lets it pass
      // else fails it
      if(reqId < choose(50))
        send server, eHelperResp, SUCCESS;
      else
        send server, eHelperResp, ERROR;
    }
  }
}
