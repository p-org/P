/*****************************************************************************************
The timer state machine models the non-deterministic behavior of an OS timer
******************************************************************************************/
machine Timer
{
    // user of the timer
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
		    if ($)
            {
                send client, eCancelTimerFailed;
            } else {
                send client, eCancelTimerSuccess;
            }
		}
	}
}

/************************************************
Events used to interact with the timer machine
************************************************/
event eStartTimer;
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
fun StartTimer(timer: Timer)
{
	send timer, eStartTimer;
}

// cancel timer
fun CancelTimer(timer: Timer)
{
	send timer, eCancelTimer;
	// wait for cancel response, nothing different is done if cancel failed or succeeded.
	receive {
		case eCancelTimerSuccess: { print "Timer Cancelled Successful"; }
		case eCancelTimerFailed: { print "Timer Cancel Failed!"; }
	}
}