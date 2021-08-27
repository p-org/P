type TimerPtr = machine;

// events from client to timer
event START: int;
event CANCEL;
// events from timer to client
event TIMEOUT: TimerPtr;
event CANCEL_SUCCESS: TimerPtr;
event CANCEL_FAILURE: TimerPtr;

//Functions for interacting with the timer machine
fun CreateTimer(owner : machine): TimerPtr {
	var m: Timer;
	m = new Timer(owner);
	return m;
}

fun StartTimer(timer: TimerPtr, time: int) {
	send timer, START, time;
}

fun CancelTimer(timer: TimerPtr) {
	send timer, CANCEL;
}


// local event for control transfer within timer
event UNIT; 

machine Timer
receives START, CANCEL;
sends TIMEOUT, CANCEL_SUCCESS, CANCEL_FAILURE;
{
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
      send client, CANCEL_FAILURE, this to Timer;
    } 
    on START goto WaitForCancel;
  }

  state WaitForCancel {
    ignore START;
    on null goto WaitForReq with { 
	  send client, TIMEOUT, this to Timer; 
	}
    on CANCEL goto WaitForReq with {
      if ($) {
        send client, CANCEL_SUCCESS, this to Timer;
      } else {
        send client, CANCEL_FAILURE, this to Timer;
        send client, TIMEOUT, this to Timer;
      }
    }
  }
}
