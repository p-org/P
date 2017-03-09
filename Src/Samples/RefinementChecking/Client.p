include Header.p

/********************************************
CliegntMachine Declaration:
* ClientMachine sends multiple eRequest events to the server and waits for response.
* ClientMachine makes an assumption that the responses are always 
  in the order as the requests being sent.
********************************************/

machine ClientMachine : ClientInterface
sends eRequest;
creates;
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
  }

  state StartPumpingRequests {
    entry {
      var index : int;
      //send 2 requests
      index = 0;
      while(index < 2)
      {
          send server, eRequest, (source = this, id = nextReqId);
          nextReqId = nextReqId + 1;
          index = index + 1;
      }
    }

    on eResponse do (payload: responseType){
        assert(payload.id > lastRecvSuccessfulReqId);
        lastRecvSuccessfulReqId = id;
    }
  }

}


