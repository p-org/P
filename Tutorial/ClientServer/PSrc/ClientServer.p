/***************************************************
User defined types
***************************************************/
// request payload
type tRequest = (source: Client, rId:int);
// response payload
type tResponse = (rId: int, status: tResponseStatus);
// response status
enum tResponseStatus {
    SUCCESS,
    ERROR
}

// Events used to communicate between the client and server
event eRequest : tRequest;
event eResponse: tResponse;

// Events used to communicate between the server and helper
event eHelperReq: int;
event eHelperResp: tResponseStatus;

/**************************************************************************
Client sends multiple eRequest events in a loop to the server and waits for responses.
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

/*************************************************************
Server receives eRequest event from the client and performs local computation.
Based on the local computation using a helper, server responds with either eReqSuccessful or eReqFailed.
Server responds to requests in the order in which they were received.
*************************************************************/
machine Server
{
  var helper: Helper;
  start state Init {
    entry {
      helper = new Helper(this);
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
machine Helper
{
  var server: Server;
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
