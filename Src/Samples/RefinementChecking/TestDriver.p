/*********************************************
Simple TestDriver machine that sets up the testing problem
***********************************************/
//test driver that creates 1 client and 1 server for testing the client || server
machine TestDriver_1Client1Server
receives;
sends;
creates ServerClientInterface, ClientInterface;
 {
  start state Init {
    entry {
      var server : ServerClientInterface;
      //create server
      server = new ServerClientInterface();
      //create client
      new ClientInterface(server);
    }
  }
}

machine TestDriver_Refinement : ClientInterface 
receives eResponse;
sends eRequest;
creates ServerClientInterface;
{
  var nextReqId : int;
  var server: ServerClientInterface;
  start state Init {
    entry {
      //create server
      nextReqId = 1;
      server = new ServerClientInterface();
      goto StartPumpingRequests;
    }
  }

  state StartPumpingRequests {
    on null do {
      send server, eRequest, (source = this, id = nextReqId);
      nextReqId = nextReqId + 1;
      if(nextReqId > 4)
        raise halt;
    }
    ignore eResponse;
  }
}
