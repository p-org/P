/**************************************************************************
 * Fest scenario-coverage examples for the ClientServer (Bank) model.
 *
 * A `scenario` is a coverage monitor: it lowers to a P spec monitor that is
 * auto-attached to EVERY test case (no `assert` needed), is EXEMPT from the
 * liveness check (an unsatisfied scenario is "uncovered", not a bug), and
 * whose accepting (cold) state means "this behavior was exercised".
 *
 * These five span the feature's dimensions:
 *   - common            : satisfied in almost every schedule
 *   - payload-dependent : satisfaction gated on an event field (status enum)
 *   - rare / ordering    : needs a specific multi-event interleaving
 *   - impossible/partial : structurally unsatisfiable -> exercises partial
 *                          coverage tracking + the liveness exemption
 **************************************************************************/

// ---------------------------------------------------------------------------
// COMMON: every withdraw request is eventually followed by a response.
// Expected: satisfied in ~all schedules (high trigger count, many timelines).
// ---------------------------------------------------------------------------
scenario WithdrawThenResponse observes eWithDrawReq, eWithDrawResp {
  start hot state Init {
    on eWithDrawReq goto SawReq;
    on eWithDrawResp do { }          // stray response before we saw a request: ignore
  }
  hot state SawReq {
    on eWithDrawResp goto Done;
    on eWithDrawReq do { }           // another request: keep waiting for a response
  }
  cold state Done {
    on eWithDrawReq do { }
    on eWithDrawResp do { }
  }
}

// ---------------------------------------------------------------------------
// PAYLOAD-DEPENDENT: a withdrawal is rejected (insufficient funds).
// The server returns WITHDRAW_ERROR when the amount would drop balance < 10.
// Expected: rarer than the common case; needs a large-enough requested amount.
// ---------------------------------------------------------------------------
scenario WithdrawError observes eWithDrawResp {
  start hot state Init {
    on eWithDrawResp do (r: tWithDrawResp) {
      if (r.status == WITHDRAW_ERROR) { goto Done; }
    }
  }
  cold state Done {
    on eWithDrawResp do { }
  }
}

// ---------------------------------------------------------------------------
// RARE / ORDERING: two SUCCESSFUL withdrawals complete in one schedule.
// Needs a run where withdrawals are small enough to succeed at least twice.
// This is the classic "diversity payoff" scenario for feedback search.
// ---------------------------------------------------------------------------
scenario TwoSuccessfulWithdrawals observes eWithDrawResp {
  start hot state Init {
    on eWithDrawResp do (r: tWithDrawResp) {
      if (r.status == WITHDRAW_SUCCESS) { goto One; }
    }
  }
  hot state One {
    on eWithDrawResp do (r: tWithDrawResp) {
      if (r.status == WITHDRAW_SUCCESS) { goto Done; }
    }
  }
  cold state Done {
    on eWithDrawResp do { }
  }
}

// ---------------------------------------------------------------------------
// ORDERING: a failed withdrawal, then later a successful one (recovery order).
// Needs an ERROR response to precede a SUCCESS response in the same schedule.
// ---------------------------------------------------------------------------
scenario ErrorThenSuccess observes eWithDrawResp {
  start hot state Init {
    on eWithDrawResp do (r: tWithDrawResp) {
      if (r.status == WITHDRAW_ERROR) { goto SawError; }
    }
  }
  hot state SawError {
    on eWithDrawResp do (r: tWithDrawResp) {
      if (r.status == WITHDRAW_SUCCESS) { goto Done; }
    }
  }
  cold state Done {
    on eWithDrawResp do { }
  }
}

// ---------------------------------------------------------------------------
// IMPOSSIBLE / PARTIAL: a response observed before ANY request.
// The protocol never produces a response without a preceding request, so the
// very first withdraw event is always a request -> Init transitions to the
// absorbing Dead state and the cold Done is never reached. Exercises:
//   (a) partial-coverage tracking  -> "best partial progress: X/Y states"
//   (b) the liveness exemption      -> a hot state at termination is NOT a bug.
// ---------------------------------------------------------------------------
scenario ImpossibleRespFirst observes eWithDrawReq, eWithDrawResp {
  start hot state Init {
    on eWithDrawResp goto GotRespFirst;   // (unreachable: resp never precedes req)
    on eWithDrawReq goto Dead;            // a request came first -> can never satisfy
  }
  hot state GotRespFirst {
    on eWithDrawResp goto Done;
    on eWithDrawReq do { }
  }
  cold state Done {
    on eWithDrawReq do { }
    on eWithDrawResp do { }
  }
  hot state Dead {                         // absorbing; scenario can no longer be met
    on eWithDrawReq do { }
    on eWithDrawResp do { }
  }
}
