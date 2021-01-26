/* This file defines two P specification monitors */


/*******************************************************************
ReqIdsAreMonotonicallyIncreasing observes the eRequest event and
checks that the payload (Id) associated with the requests issued by
the client is always monotonically increasing by 1
*******************************************************************/
spec ReqIdsAreMonotonicallyIncreasing observes eRequest {
  // keep track of the Id in the previous request
    var previousId : int;
    start state Init {
        on eRequest do (req: tRequest) {
            assert req.rId > previousId,
            format ("Request Ids not monotonically increasing, got {0}, previous Id was {1}", req.rId, previousId);
            previousId = req.rId;
        }
    }
}

/********************************************************************
RespIdsAreMonotonicallyIncreasing observes the eResponse event and
checks that the Id associated with the responses issued by the
server is always monotonically increasing by 1
*********************************************************************/
spec RespIdsAreMonotonicallyIncreasing observes eResponse {
    // keep track of the Id in the previous response
    var previousId : int;
    start state Init {
        on eResponse do (resp: tResponse) {
            assert resp.rId > previousId,
            format ("Response Ids not monotonically increasing, got {0}, previous Id was {1}", resp.rId, previousId);
            previousId = resp.rId;
        }
    }
}

/**************************************************************************
GuaranteedProgress observes the eRequest and eResponse events, it asserts that
every request is always responded by a successful response.
***************************************************************************/
spec GuaranteedProgress observes eRequest, eResponse {
    // keep track of the pending requests
    var pendingReqs: set[int];

    start state NopendingRequests {
        on eRequest goto PendingReqs with (req: tRequest){
            pendingReqs += (req.rId);
        }
    }

    hot state PendingReqs {
        on eResponse do (resp: tResponse) {
            assert resp.rId in pendingReqs, format ("unexpected rId: {0} received, expected one of {1}", resp.rId, pendingReqs);
            if(resp.status == SUCCESS)
            {
                pendingReqs -= (resp.rId);
                if(sizeof(pendingReqs) == 0) // requests already responded
                    goto NopendingRequests;
            }
        }

        on eRequest goto PendingReqs with (req: tRequest){
            pendingReqs += (req.rId);
        }
    }
}