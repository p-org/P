/*****************************************************************************************
The timer state machine models the non-deterministic behavior of an OS timer
******************************************************************************************/
machine Timer
{
	var client: machine;
	start state Init {
		entry (_client : machine){
			client = _client;
			goto WaitForTimerRequests;
		}
	}

	state WaitForTimerRequests {
		on eStartTimer do { if($) send client, eTimeOut; }
		on eCancelTimer do {

		}
	}
}

/************************************************
Events used to interact with the timer machine
************************************************/
event eStartTimer: int;
event eCancelTimer;
event eCancelTimerFailed;
event eCancelTimerSuccess;
event eTimeOut;
/************************************************
Functions or API's to interact with the OS Timer
*************************************************/
// create timer
fun CreateTimer(client: machine) : Timer
{
	return new Timer(client);
}

// start timer
fun StartTimer(timer: Timer, timeout: int)
{
	send timer, eStartTimer, timeout;
}

// cancel timer
fun CancelTimer(timer: Timer)
{
	send timer, eCancelTimer;
}