/**************************************************************************
 * Fest scenario-coverage examples for the TwoPhaseCommit model.
 *
 * See ClientServer/PSpec/Scenarios.p for what a `scenario` is. These exercise
 * failure-dependent behavior (aborts/timeouts) and cross-participant ordering.
 *
 * Coordinator flow: eWriteTransReq -> ePrepareReq(broadcast) -> prepare votes
 *   -> either eCommitTrans(broadcast) + eWriteTransResp{SUCCESS}
 *      or     eAbortTrans(broadcast)  + eWriteTransResp{ERROR|TIMEOUT}.
 **************************************************************************/

// COMMON: a write request commits successfully.
scenario WriteCommitted observes eWriteTransReq, eWriteTransResp {
  start hot state Init {
    on eWriteTransReq goto SawReq;
    on eWriteTransResp do { }
  }
  hot state SawReq {
    on eWriteTransResp do (r: tWriteTransResp) { if (r.status == SUCCESS) { goto Done; } }
    on eWriteTransReq do { }
  }
  cold state Done {
    on eWriteTransReq do { }
    on eWriteTransResp do { }
  }
}

// FAILURE-DEPENDENT / PAYLOAD: a write is aborted (ERROR) or times out.
// Only reachable under test cases that inject participant failures.
scenario WriteAborted observes eWriteTransResp {
  start hot state Init {
    on eWriteTransResp do (r: tWriteTransResp) {
      if (r.status == ERROR || r.status == TIMEOUT) { goto Done; }
    }
  }
  cold state Done {
    on eWriteTransResp do { }
  }
}

// DEEP / RARE: a transaction aborts, then a later transaction commits (recovery).
// Needs a failure followed by a successful commit in the same schedule.
scenario AbortThenCommit observes eAbortTrans, eWriteTransResp {
  start hot state Init {
    on eAbortTrans goto SawAbort;
    on eWriteTransResp do { }
  }
  hot state SawAbort {
    on eWriteTransResp do (r: tWriteTransResp) { if (r.status == SUCCESS) { goto Done; } }
    on eAbortTrans do { }
  }
  cold state Done {
    on eAbortTrans do { }
    on eWriteTransResp do { }
  }
}

// ORDERING: two successful commits observed in one schedule.
scenario TwoCommits observes eWriteTransResp {
  start hot state Init {
    on eWriteTransResp do (r: tWriteTransResp) { if (r.status == SUCCESS) { goto One; } }
  }
  hot state One {
    on eWriteTransResp do (r: tWriteTransResp) { if (r.status == SUCCESS) { goto Done; } }
  }
  cold state Done {
    on eWriteTransResp do { }
  }
}

// IMPOSSIBLE / PARTIAL: a commit broadcast before ANY prepare broadcast.
// The coordinator always prepares before committing, so the first observed
// event is always a prepare -> Init transitions to Dead and Done is unreachable.
scenario ImpossibleCommitFirst observes ePrepareReq, eCommitTrans {
  start hot state Init {
    on eCommitTrans goto CommitFirst;   // (unreachable: prepare always precedes commit)
    on ePrepareReq goto Dead;
  }
  hot state CommitFirst {
    on eCommitTrans goto Done;
    on ePrepareReq do { }
  }
  cold state Done {
    on ePrepareReq do { }
    on eCommitTrans do { }
  }
  hot state Dead {
    on ePrepareReq do { }
    on eCommitTrans do { }
  }
}
