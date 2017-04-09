type ITimer(machine) = { START, CANCEL };
model type TimerPtr = ITimer;

// events from client to timer
event START: int;
event CANCEL;
// events from timer to client
event TIMEOUT: TimerPtr;
event CANCEL_SUCCESS: TimerPtr;
event CANCEL_FAILURE: TimerPtr;

//Functions for interacting with the timer machine
model fun CreateTimer(owner : machine): TimerPtr {
	var m: ITimer;
	m = new Timer(owner);
	return m;
}

model fun StartTimer(timer: TimerPtr, time: int) {
	send timer, START, 100;
}

model fun CancelTimer(timer: TimerPtr) {
	send timer, CANCEL; 
}

model Timer
receives START, CANCEL;
sends TIMEOUT, CANCEL_SUCCESS, CANCEL_FAILURE;
{
	var client: machine;

	start state Init {
		entry (m: machine) {
			client = m;
			goto WaitForReq;
		}
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