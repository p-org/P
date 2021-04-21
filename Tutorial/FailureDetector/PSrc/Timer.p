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
		    if ($) // or choose()
            {
                // the timeout can happen concurrently when the user calls cancel timer
                send client, eCancelTimerFailed;
                send client, eTimeOut;
            } else {
                send client, eCancelTimerSuccess;
            }
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
	receive {
		case eCancelTimerSuccess: { print "Timer Cancelled Successful"; }
		case eCancelTimerFailed: {
			receive {
				case eTimeOut: { print "Timer Cancelled Successful"; }
			}
		}
	}
}