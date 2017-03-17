include "Header.p"


machine ServerAbstraction: ServerClientInterface
receives eRequest;
sends eResponse;
creates;
{
  start state Init {
<<<<<<< HEAD
=======

>>>>>>> 67e07c7449eeb61e3ee19fa58dbe9632d2861fb7
    on eRequest do (payload: requestType){
      var successful : bool;
      successful = $;
      send payload.source, eResponse, (id = payload.id, success = successful);
    }
  }
}


/***************************************************************************
<<<<<<< HEAD
Request Ids must be monotonically Increasing
***************************************************************************/
spec ReqIdsAreMonotonicallyIncreasing observes eRequest {
=======
If the response is success then the value should always be greater than zero
***************************************************************************/
spec ReqIdsAreMonotonicallyIncreasing observes eResponse {
>>>>>>> 67e07c7449eeb61e3ee19fa58dbe9632d2861fb7
  var previousId : int;
  start state Init {
    on eRequest do (payload: requestType){
        assert(payload.id > previousId);
        previousId = payload.id;
    }
  }
<<<<<<< HEAD
}
=======
}
>>>>>>> 67e07c7449eeb61e3ee19fa58dbe9632d2861fb7
