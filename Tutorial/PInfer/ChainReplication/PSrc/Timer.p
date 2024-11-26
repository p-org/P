/*****************************************************************************************
The timer state machine models the non-deterministic behavior of an OS timer
******************************************************************************************/

/************************************************
Events used to interact with the timer machine
************************************************/
event eStartTimer;
event eCancelTimer;
event eTimeOut;
event eTick;

machine Timer {
  // user/client of the timer
  var client: machine;
  var numTicks: int;
  var curTicks: int;

  start state Init {
    entry (input: (client : machine, numTicks : int)) {
      client = input.client;
      numTicks = input.numTicks;
      goto WaitForTimerRequests;
    }
  }

  state WaitForTimerRequests {
    on eStartTimer goto TimerStarted;

    ignore eCancelTimer, eTick;
  }

  state TimerStarted {
    entry {
      Progress();
    }

    on eTick do {
      Progress();
    }
    on eCancelTimer goto WaitForTimerRequests;
    defer eStartTimer;
  }

  fun Progress() {
    curTicks = curTicks + 1;
    if(curTicks >= numTicks) {
      send client, eTimeOut;
      curTicks = 0;
      goto WaitForTimerRequests;
    } else {
      send this, eTick;
    }
  }
}

/************************************************
Functions or API's to interact with the OS Timer
*************************************************/
// create timer
fun CreateTimer(client: machine, numTicks: int) : Timer {
  return new Timer((client = client, numTicks = numTicks));
}

// start timer
fun StartTimer(timer: Timer) {
  send timer, eStartTimer;
}

// cancel timer
fun CancelTimer(timer: Timer) {
  send timer, eCancelTimer;
}
