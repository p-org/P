machine ServerAbstraction: ServerClientInterface
receives eRequest;
sends eResponse;
creates;
{
  start state Init {
    on eRequest do (payload: requestType){
      var successful : bool;
      successful = $;
      send payload.source, eResponse, (id = payload.id, success = successful);
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
