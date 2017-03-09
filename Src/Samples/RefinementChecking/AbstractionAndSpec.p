include "Header.p"


machine ServerAbstraction: ServerClientInterface
receives eReqSuccessful, eReqFailed, eRequest;
sends eResponse, eProcessReq;
creates HelperInterface;
{
  start state Init {

    on eRequest do (payload: requestType){
      send payload.source, eResponse, (id = payload.id, success = $);
    }
  }
}


/***************************************************************************
If the response is success then the value should always be greater than zero
***************************************************************************/
spec ReqIdsAreMonotonicallyIncreasing observes eResponse {
  var previousId : int;
  start state Init {
    on eRequest do (payload: requestType){
        assert(payload.id > previousId);
        previousId = payload.id;
    }
  }
}