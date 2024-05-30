event eStartTimer;
event eCancelTimer;
event eTimeOut;
event eDelayedTimeOut;

machine Timer {
  var client: machine;

  start state Init {
    entry (_client : machine) {
      client = _client;
      goto WaitForTimerRequests;
    }
  }

  state WaitForTimerRequests {
    on eStartTimer goto TimerStarted;

    ignore eCancelTimer, eDelayedTimeOut;
  }

  state TimerStarted {
    entry {
      if($) {
        send client, eTimeOut;
        goto WaitForTimerRequests;
      } else {
        send this, eDelayedTimeOut;
      }
    }

    on eDelayedTimeOut goto TimerDelayed;
    on eCancelTimer goto WaitForTimerRequests;
    defer eStartTimer;
  }

  state TimerDelayed {
    entry {
      if($) {
        send client, eTimeOut;
        goto WaitForTimerRequests;
      } else {
        // do nothing, wait for eCancelTimer and ignore any old eDelayedTimeOut
      }
    }

    on eCancelTimer goto WaitForTimerRequests;
    defer eStartTimer;
    ignore eDelayedTimeOut;
  }
}

fun CreateTimer(client: machine) : Timer {
  return new Timer(client);
}

fun StartTimer(timer: Timer) {
  send timer, eStartTimer;
}

fun CancelTimer(timer: Timer) {
  send timer, eCancelTimer;
}
