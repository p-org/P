model type TimerPtr = machine;

// from client to timer
event START: int;
// from timer to client
event TIMEOUT: TimerPtr;

//Functions for interacting with the timer machine
model fun CreateTimer(owner : machine): TimerPtr {
	var m: machine;
	m = new Timer(owner);
	return m;
}

model fun StartTimer(timer: TimerPtr, time: int) {
	send timer, START, time;
}

// local event for control transfer within timer
event UNIT; 

model Timer
receives START;
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
    on START goto WaitForTimeout;
  }

  state WaitForTimeout {
    ignore START;
    on null goto WaitForReq with { 
	  send client, TIMEOUT, this; 
	}
  }
}
