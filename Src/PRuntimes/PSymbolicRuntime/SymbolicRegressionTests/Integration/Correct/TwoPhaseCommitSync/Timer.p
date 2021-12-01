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
		on sync_eStartTimer do { if($) send client, eTimeOut; }
		on sync_eCancelTimer do {

		}
	}
}

/************************************************
Events used to interact with the timer machine
************************************************/
event sync_eStartTimer: int;
event sync_eCancelTimer;
event sync_eCancelTimerFailed;
event sync_eCancelTimerSuccess;
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
	send timer, sync_eStartTimer, timeout;
}

// cancel timer
fun CancelTimer(timer: Timer)
{
	send timer, sync_eCancelTimer;
}