// Regression: `scenario` (coverage) monitors.
//  - Scenarios are auto-attached to the test (no `assert` needed).
//  - A satisfied scenario reaches its accepting (cold) state.
//  - An UNSATISFIED scenario left in a hot state must NOT cause a liveness
//    failure (coverage monitors are exempt) — the run must still exit cleanly.

event eWriteReq: int;
event eReadReq: int;
event eNever: int;

machine Main {
  var server: Server;
  start state Init {
    entry {
      server = new Server();
      send server, eWriteReq, 1;
      send server, eReadReq, 1;
    }
  }
}

machine Server {
  start state Serving {
    on eWriteReq do (key: int) { }
    on eReadReq do (key: int) { }
  }
}

// Satisfied on every schedule: a write followed by a read.
scenario ReadAfterWrite observes eWriteReq, eReadReq {
  start hot state WaitWrite {
    on eWriteReq goto WaitRead;
  }
  hot state WaitRead {
    on eReadReq goto Satisfied;
  }
  cold state Satisfied { }
}

// Never satisfied (observes an event that is never sent). Must not fail the
// test even though it ends in a hot state.
scenario NeverCovered observes eNever {
  start hot state Waiting {
    on eNever goto Done;
  }
  cold state Done { }
}
