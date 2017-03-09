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
Request Ids must be monotonically Increasing
***************************************************************************/
spec ReqIdsAreMonotonicallyIncreasing observes eRequest {
  var previousId : int;
  start state Init {
    on eRequest do (payload: requestType){
        assert(payload.id > previousId);
        previousId = payload.id;
    }
  }
}
