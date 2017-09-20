/**************************************************************************
Client sends multiple eRequest events to the server and waits for response.
Server responds with eResponse event for each eRequest event.
The responses must be in the same order as the requests being sent.
**************************************************************************/

machine ClientMachine
sends eRequest;
{
  var server : ServerClientInterface;
  var nextReqId : int;
  var lastRecvSuccessfulReqId: int;

  start state Init {
    entry (payload : ServerClientInterface)
    {
      nextReqId = 1;
      server = payload;
      goto StartPumpingRequests;
    }
    exit {

    }
  }

  state StartPumpingRequests {
    entry {
      var index : int;
      //send 2 requests
      index = 0;
      while(index < 2)
      {
          send server, eRequest, (source = this to ClientInterface, id = nextReqId);
          nextReqId = nextReqId + 1;
          index = index + 1;
      }
    }
    
    on eResponse do (payload: responseType){
        assert(payload.id > lastRecvSuccessfulReqId);
        lastRecvSuccessfulReqId = payload.id;
    }
  }
}
