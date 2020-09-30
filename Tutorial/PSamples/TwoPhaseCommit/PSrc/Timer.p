/* 
This file implements the model for an asynchronous timer
*/

machine Timer 
receives eStartTimer, eCancelTimer;
sends eTimeOut, eCancelTimerFailed, eCancelTimerSuccess;
{
	var target: machine;
	start state Init {
		entry (payload : machine){
			target = payload;
			goto WaitForStartTimer;
		}
	}

	state WaitForStartTimer {
		on eStartTimer goto TimerStarted;
		on eCancelTimer do { send target, eCancelTimerFailed; }
	}

	state TimerStarted {
		entry (payload: int) {
			//non-deterministically choose to send a timeout or not
			if ($) {
				send target, eTimeOut;
				goto WaitForStartTimer;
			}
		}
		on eCancelTimer goto WaitForStartTimer with {
			if ($) {
				//the timeout can happen concurrently when the user calls cancel-timer
				send target, eCancelTimerFailed;
				send target, eTimeOut;
			} else {
				send target, eCancelTimerSuccess;
			}		
		}
	}
}



/* 
helper functions to interact with the timer machine 
*/

fun CreateTimer(client: machine) : machine
{
	return new Timer(client);
}

fun StartTimer(timer: machine, value: int)
{
	send timer, eStartTimer, value;
}

fun CancelTimer(timer: machine)
{
	send timer, eCancelTimer;
	receive {
		case eCancelTimerSuccess: { print "Timer Cancelled Successful"; }
		case eCancelTimerFailed: {
			receive {
				case eTimeOut: { print "Timer Cancelled Successful"; }
			}
		}
	}
}