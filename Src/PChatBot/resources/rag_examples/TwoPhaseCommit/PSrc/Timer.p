machine Timer {
    var client: machine;
    var timerActive: bool;

    start state Init {
        entry InitEntry;
    }

    state WaitForTimerRequests {
        on eStartTimer goto TimerStarted;
        ignore eCancelTimer, eTimeOut;
    }

    state TimerStarted {
        entry TimerStartedEntry;
        on eTimeOut goto TimerStarted;
        on eCancelTimer goto WaitForTimerRequests;
        defer eStartTimer;
    }

    fun InitEntry(payload: machine) {
        client = payload;
        goto WaitForTimerRequests;
    }

    fun TimerStartedEntry() {
        var shouldFire: bool;
        
        shouldFire = choose();
        
        if (shouldFire) {
            send client, eTimeOut;
            goto WaitForTimerRequests;
        } else {
            send this, eTimeOut;
        }
    }
}