// events from client to timer
event START: int;
event CANCEL;
// events from timer to client
event TIMEOUT: machine;
event CANCEL_SUCCESS: machine;
event CANCEL_FAILURE: machine;
// local event for control transfer within timer
event UNIT;
machine Timer {
  var client: machine;
  start state Init {
    entry (payload: machine) {
      client = payload as machine;
      raise UNIT;  // goto handler of UNIT
    }
    on UNIT goto WaitForReq;
  }

  state WaitForReq {
    on CANCEL goto WaitForReq with {
      send client, CANCEL_FAILURE, this;
    }
    on START goto WaitForCancel;
  }

  state WaitForCancel {
    ignore START;
    on null goto WaitForReq with {
	   send client, TIMEOUT, this;
	}
    on CANCEL goto WaitForReq with {
      if ($) {
        send client, CANCEL_SUCCESS, this;
      } else {
        send client, CANCEL_FAILURE, this;
        send client, TIMEOUT, this;
      }
    }
  }
}
