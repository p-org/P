/***************************************************
User defined types
***************************************************/
// request payload type
type tRequest = (source: Client, rId:int);
// response payload type
type tResponse = (rId: int, status: tResponseStatus);
// response status type
enum tResponseStatus {
    SUCCESS,
    ERROR
}

// Events exchanged by client and server
event eRequest : tRequest;
event eResponse: tResponse;

/**************************************************************************
Client sends multiple eRequests events asynchronously to the server and waits for responses.
Server responds with eResponse event for each eRequest event.
The client asserts that the successful responses must be in the same order
as the requests being sent (monotonically increasing).
**************************************************************************/
machine Client
{
  var server : Server;
  var nextReqId : int;
  var lastSuccessfulRespId: int;

  start state Init {

    entry (payload : Server)
    {
      nextReqId = 1;
      lastSuccessfulRespId = -1;
      server = payload;
      goto StartPumpingRequests;
    }
  }

  state StartPumpingRequests {
    entry {
      var index : int;
      index = 0;
      //send 2 requests
      while(index < 2)
      {
          send server, eRequest, (source = this, rId = nextReqId);
          // request ids are monotonically increasing
          nextReqId = nextReqId + 1 + choose(5);
          index = index + 1;
      }
    }

    on eResponse do (resp: tResponse){
        // response id's are monotonically increasing
        if(resp.status == SUCCESS)
        {
            // local assertion
            assert resp.rId > lastSuccessfulRespId,
            format ("Response Ids not monotonically increasing, got {0}, previous Id was {1}", resp.rId, lastSuccessfulRespId);
            lastSuccessfulRespId = resp.rId;
        }
    }
  }
}