model type TimerPtr = machine;

//Functions for interacting with the timer machine
model fun CreateTimer(owner : machine): TimerPtr {
	var m: machine;
	m = new Timer(owner);
	return m;
}

model fun StartTimer(timer: TimerPtr, time: int) {
	send timer, START, 100;
}

model fun CancelTimer(timer: TimerPtr) {
	send timer, CANCEL;
}

// events from client to timer
event START: int;
event CANCEL;
// events from timer to client
event TIMEOUT: TimerPtr;
event CANCEL_SUCCESS: TimerPtr;
event CANCEL_FAILURE: TimerPtr;
// local event for control transfer within timer
event UNIT; 

model Timer {
  var client: machine;

  start state Init {
    entry (payload: machine) {
      client = payload;
	  // goto WaitForReq
      raise UNIT;
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
