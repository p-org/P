machine AbstractServerMachine
sends eResponse;
{
  start state Init {
    on eRequest do (payload: requestType){
      var successful : bool;
      successful = $;
      send payload.source, eResponse, (id = payload.id, success = successful);
    }
  }
}


spec ReqIdsAreMonotonicallyIncreasing observes eRequest {
  var previousId : int;
  start state Init {
    on eRequest do (payload: requestType){
        assert(payload.id == previousId + 1);
        previousId = payload.id;
    }
  }
}

spec RespIdsAreMonotonicallyIncreasing observes eResponse {
  var previousId : int;
  start state Init {
    on eResponse do (payload: responseType){
        assert(payload.id == previousId + 1);
        previousId = payload.id;
    }
  }
}
