/*************************************************************
Server receives eRequest event from the client and performs
local computation.  Based on the local computation, server
responds with either eReqSuccessful or eReqFailed.  Server
responds to requests in the order in which they were received.
*************************************************************/

machine ServerMachine
sends eResponse, eProcessReq;
{
  var helper: HelperInterface;

  start state Init {
    entry {
      helper = new HelperInterface(this to ServerHelperInterface);
      goto WaitForRequests;
    }
  }

  state WaitForRequests {
    on eRequest do (payload: requestType){
      send helper, eProcessReq, payload.id;
      receive {
        case eReqSuccessful: {
          send payload.source, eResponse, (id = payload.id, success = true);
        }
        case eReqFailed: {
          send payload.source, eResponse, (id = payload.id, success = false);
        }
      }
    }
  }
}

/***************************************************************
The helper machine performs some complex computation and returns
either eReqSuccessful or eReqFailed.
****************************************************************/
machine HelperMachine
sends eReqSuccessful, eReqFailed;
{
  var server: ServerHelperInterface;
  
  start state Init {
    entry(payload : ServerHelperInterface){
      server = payload;
    }
    on eProcessReq do {
      if($)
      {
        send server, eReqSuccessful;
      }
      else
      {
        send server, eReqFailed;
      }
    }
  }
}
