/********************************************************
* ServerMachine receives a eRequest event from the clients and 
  performs some local computation.
* ServerMachine based on the local computation either sends that the 
  request succeeded or failed.
* ServerMachine responds to the requests in order in which they were
  received.
*********************************************************/

type ServerHelperInterface() = { eReqSuccessful, eReqFailed };
type HelperInterface(ServerHelperInterface) = { eProcessReq };

machine ServerMachine : ServerClientInterface
receives eReqSuccessful, eReqFailed, eRequest;
sends eResponse, eProcessReq;
creates HelperInterface;
{
  var helper: HelperInterface;

  start state Init {
    entry {
      helper = new HelperInterface(this as ServerHelperInterface);
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
* The helper machine performs some complex computation and either returns Successful or Failed
****************************************************************/
machine HelperMachine : HelperInterface
sends eReqSuccessful, eReqFailed;
creates;
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
