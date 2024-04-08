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
                ignore eCancelTimer;
		on eStartTimer goto TimerStarted;
	}

	state TimerStarted {
		entry (payload: int) {
			send target, eTimeOut;
			goto WaitForStartTimer;
		}
		on eCancelTimer goto WaitForStartTimer;
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
