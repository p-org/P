/*****************************************************************************************
The timer state machine models the non-deterministic behavior of an OS timer
******************************************************************************************/

/************************************************
Events used to interact with the timer machine
************************************************/
event eStartTimer;
event eCancelTimer;
event eTimeOut;
event eDelayedTimeOut;

machine Timer {
  // user/client of the timer
  var client: machine;
  // Bound the maximum number of delays that are possible
  var maxDelays: int;
  var remainingDelays: int;

  start state Init {
    entry (_client : machine) {
      client = _client;
      maxDelays = 50;
      remainingDelays = maxDelays;
      goto WaitForTimerRequests;
    }
  }

  state WaitForTimerRequests {
    entry {
      remainingDelays = maxDelays;
    }

    on eStartTimer goto TimerStarted;

    ignore eCancelTimer, eDelayedTimeOut;
  }

  state TimerStarted {
    entry {
      // Only fire the timer with a chance of 1/maxDelays to avoid livelocks
      // Also bound the maximum number of times we can delay
      if(choose(maxDelays) == 0 || remainingDelays == 0) {
        send client, eTimeOut;
        goto WaitForTimerRequests;
      } else {
        remainingDelays = remainingDelays - 1;
        send this, eDelayedTimeOut;
      }
    }

    on eDelayedTimeOut goto TimerStarted;
    on eCancelTimer goto WaitForTimerRequests;
    defer eStartTimer;
  }
}

/************************************************
Functions or API's to interact with the OS Timer
*************************************************/
// create timer
fun CreateTimer(client: machine) : Timer {
  return new Timer(client);
}

// start timer
fun StartTimer(timer: Timer) {
  send timer, eStartTimer;
}

// cancel timer
fun CancelTimer(timer: Timer) {
  send timer, eCancelTimer;
}
