//Functions for interacting with the timer machine
model fun CreateTimer(owner : machine): TimerPtr {
	var m: machine;
	m = new Timer(owner);
	return m;
}

model fun StartTimer(timer: TimerPtr, time: int) {
	send timer, eStartTimer, 100;
}

model fun CancelTimer(timer: TimerPtr) {
	send timer, eCancelTimer;
	receive {
		case eCancelSuccess: (payload: TimerPtr){}
		case eCancelFailure: (payload: TimerPtr){
			receive {
				case eTimeOut: (payload1: TimerPtr){}
			}
		}
	}
}

model Timer
receives eStartTimer, eCancelTimer;
sends eTimeOut, eCancelSuccess, eCancelFailure;
{
	var client: machine;

	start state Init {
		entry (m: machine) {
			client = m;
			goto WaitForReq;
		}
	}

	state WaitForReq {
		on eCancelTimer goto WaitForReq with { 
			send client, eCancelFailure, this;
		} 
		on eStartTimer goto WaitForCancel;
	}

	state WaitForCancel {
		ignore eStartTimer;
		on null goto WaitForReq with { 
			send client, eTimeOut, this; 
		}
		on eCancelTimer goto WaitForReq with {
			if ($) {
				send client, eCancelSuccess, this;
			} else {
				send client, eCancelFailure, this;
				send client, eTimeOut, this;
			}
		}
	}
}