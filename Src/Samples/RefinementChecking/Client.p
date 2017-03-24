/********************************************
<<<<<<< HEAD
ClientMachine Declaration:
=======
CliegntMachine Declaration:
>>>>>>> 67e07c7449eeb61e3ee19fa58dbe9632d2861fb7
* ClientMachine sends multiple eRequest events to the server and waits for response.
* ClientMachine makes an assumption that the responses are always 
  in the order as the requests being sent.
********************************************/

machine ClientMachine : ClientInterface
receives eResponse;
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
        lastRecvSuccessfulReqId = payload.id;
    }
  }

}


