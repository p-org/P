type tLockReq = (clientId: tClientId, lockId: tLockId, rId: int);
type tLockResp = (status: tLockRespStatus, clientId: tClientId, lockId: tLockId, lockStatus: tLockStatus, rId: int);
type tReleaseReq = (clientId: tClientId, lockId: tLockId, rId: int);
type tReleaseResp = (status: tReleaseRespStatus, clientId: tClientId, lockId: tLockId, lockStatus: tLockStatus, rId: int);
type tLockId = int;
type tClientId = int;

enum tLockStatus {
  LOCKED,
  FREE
}

enum tLockRespStatus {
  LOCK_SUCCESS,
  LOCK_ERROR
}

enum tReleaseRespStatus {
  RELEASE_SUCCESS,
  RELEASE_ERROR
}


// event: write lock request (client to lock server)
event eLockReq : tLockReq;
// event: write lock response (lock to client)
event eLockResp : tLockResp;
// event: write release request (client to lock server)
event eReleaseReq : tReleaseReq;
// event: write release response (lock to client)
event eReleaseResp : tReleaseResp;

/*
  MutualExclusion specification asserts the following:
  1. A lock is acquired only if it is free at that given time
  2. A lock is released only when the release request is made by the client that acquires the lock
*/
spec MutualExclusion observes eLockResp, eReleaseResp {
  var lockClientPair: map[tLockId, tClientId];
  start state WaitForReqAndResp {
    on eLockResp do (resp: tLockResp) {
      // assert only see lock success if nobody acquired lock at that time
      if (resp.lockId in keys(lockClientPair)) {
        assert resp.status == LOCK_ERROR, format ("Lock {0} is already acquired, expects lock error but received lock success.", resp.lockId);
      }
      else {
        assert resp.status == LOCK_SUCCESS, format ("Expect success lock lockId: {0} in lock response, but received failed lock.", resp.lockId);
        lockClientPair += (resp.lockId, resp.clientId);
      }
    }
    // assert only see release success if the lock is being aquired by the client
     on eReleaseResp do (resp: tReleaseResp) {
      if (resp.lockId in lockClientPair) {
        if (resp.clientId == lockClientPair[resp.lockId]) {
            assert resp.status == RELEASE_SUCCESS, format ("Expect success release locId: {0} in release response, but received failed release. rId: {1}", resp.lockId, resp.rId);
            lockClientPair -= (resp.lockId);
        }
        else {
        assert resp.status == RELEASE_ERROR, format ("Lock {0} is being acquired by other clients in release request, should have returned release error. rId: {1}", resp.lockId, resp.rId);
        }
      } else {
        assert resp.status == RELEASE_ERROR, format ("Lock {0} in the release request hasn't being acquired yet, should have returned release error. rId: {1}", resp.lockId, resp.rId);
      }
    }
  }
}

/*
  ResponseOnlyOnRequest specification asserts the following:
  There should be no lock/release response without a corresponding lock/release request
*/
spec ResponseOnlyOnRequest observes eLockReq, eLockResp, eReleaseReq, eReleaseResp {
  var pendingLockRequests: map[tLockId, tLockReq];
  var pendingReleaseRequests: map[tLockId, tReleaseReq];

  start state checking {
    on eLockReq do (req: tLockReq) {
      pendingLockRequests[req.rId] = req;
    }

    on eReleaseReq do (req: tReleaseReq) {
      pendingReleaseRequests[req.rId] = req;
    }

    on eReleaseResp do (resp: tReleaseResp) {
	    assert resp.rId in keys(pendingReleaseRequests), format("rId: {0} was not requested.", resp.rId);
      pendingReleaseRequests -= (resp.rId);
    }

    on eLockResp do (resp: tLockResp) {
      assert resp.rId in keys(pendingLockRequests), format("rId: {0} was not requested.", resp.rId);
      pendingLockRequests -= (resp.rId);
    }
  }
}
