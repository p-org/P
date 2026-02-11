/******************************************************************
* A Timer machine that timeouts non-deterministically.
*******************************************************************/

event eStartTimer;
event eElectionTimeout;
event eHeartbeatTimeout;
event eCancelTimer;
event eTick;

machine Timer {
    var holder: machine;
    var timeoutEvent: event;

    start state Init {
        entry (setup: (user: machine, timeoutEvent: event)) {
            holder = setup.user;
            timeoutEvent = setup.timeoutEvent;
            goto TimerIdle;
        }
        ignore eCancelTimer, eTick, eStartTimer;
    }

    state TimerIdle {
        on eStartTimer do {
            goto TimerTick;
        }
        on eShutdown goto TimerShutdown;
        ignore eCancelTimer, eTick;
    }

    state TimerTick {
        entry {
            checkTick();
        }
        on eTick do {
            checkTick();
        }
        on eShutdown goto TimerShutdown;
        on eCancelTimer goto TimerIdle;
        ignore eStartTimer;
    }

    state TimerShutdown {
        ignore eStartTimer, eCancelTimer, eTick;
    }

    fun checkTick() {
        if ($) {
            // non-deterministic timeout
            send holder, timeoutEvent;
            goto TimerIdle;
        } else {
            send this, eTick;
        }
    }
}