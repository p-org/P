//Functions for interacting with the timer machine
fun CreateTimer(owner : ITimerClient): TimerPtr {
	var m: ITimer;
	m = new ITimer(owner);
	return m;
}

fun StartTimer(timer: TimerPtr, time: int) {
	send timer, eStartTimer, 100;
}

fun CancelTimer(timer: TimerPtr) {
	send timer, eCancelTimer;
	receive {
		case eCancelSuccess: (payload: TimerPtr){}
		case eCancelFailure: (payload: TimerPtr){}
	}
}

machine Timer
receives eStartTimer, eCancelTimer;
sends eTimeOut, eCancelSuccess, eCancelFailure;
{
	var client: ITimerClient;

	start state Init {
		entry (m: ITimerClient) {
			client = m;
			goto WaitForReq;
		}
	}

	state WaitForReq {
		on eCancelTimer goto WaitForReq with { 
			send client, eCancelSuccess, this;
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
			}
		}
	}
}