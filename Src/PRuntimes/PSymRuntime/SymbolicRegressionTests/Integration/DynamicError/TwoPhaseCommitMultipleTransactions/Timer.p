/*
This file implements the model machine for a asynchronous timer
*/

machine Timer
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
			if ($) {
				send target, eTimeOut;
				goto WaitForStartTimer;
			}
		}
		on eCancelTimer goto WaitForStartTimer with {
			if ($) {
				send target, eCancelTimerFailed;
				//send target, eTimeOut;
			} else {
				send target, eCancelTimerSuccess;
			}		
		}
	}
}

fun CreateTimer(client: machine) : machine
{
	return new Timer(client);
}

fun StartTimer(timer: machine, value: int)
{
	send timer, eStartTimer, value;
}
