// Run by Tst/UnitTests/BugTraceTest.cs, not by the regression suite.
// Each test finds a different kind of bug. The checker explores without tracing and replays the
// failing schedule to trace it, so the traces of each bug must be those of the failing schedule.

event ePing: int;
event eReq;
event eResp;
event eTick;
event eDone;

// The foreign counter is static, so a replay of a schedule sees another value than the schedule.
fun NextCounter(): int;

// Two senders race to a receiver that asserts on the order it receives them in.
machine Receiver {
  var received: seq[int];
  start state Wait {
    on ePing do (i: int) {
      received += (sizeof(received), i);
      print format("received {0} after {1}", i, received);
      if (sizeof(received) == 2) {
        assert received[0] < received[1], format("received {0} before {1}", received[0], received[1]);
      }
    }
  }
}

machine Sender {
  start state Send {
    entry (payload: (receiver: Receiver, i: int)) {
      if ($) { print format("sender {0} hesitates", payload.i); }
      send payload.receiver, ePing, payload.i;
    }
  }
}

machine Race {
  var x: int;
  fun Increment(): int {
    x = x + 1;
    return x;
  }
  start state Init {
    entry {
      var receiver: Receiver;
      x = 1;
      // The argument before a call is formatted with its value before the call.
      print format("{0} {1}", x, Increment());
      receiver = new Receiver();
      new Sender((receiver = receiver, i = 1));
      new Sender((receiver = receiver, i = 2));
    }
  }
}

// Never terminates, so it only fails if hitting the steps bound is a bug.
machine Forever {
  var n: int;
  start state Loop {
    entry { send this, eTick; }
    on eTick do {
      n = n + 1;
      if ($) { print format("tick {0}", n); }
      send this, eTick;
    }
  }
}

// Left in a hot state at the end of the schedule unless every request gets a response.
spec Responded observes eReq, eResp {
  start cold state Idle { on eReq goto Waiting; }
  hot state Waiting { on eResp goto Idle; ignore eReq; }
}

machine Requests {
  var n: int;
  start state Run {
    entry {
      while (n < 3) {
        announce eReq;
        if ($) { announce eResp; }
        print format("request {0}", n);
        n = n + 1;
      }
    }
  }
}

// Stays too long in a hot state of a monitor that never terminates.
spec Progress observes eTick, eDone {
  start cold state Idle { on eTick goto Busy; ignore eDone; }
  hot state Busy { on eDone goto Idle; ignore eTick; }
}

machine Ticker {
  start state Run {
    entry { announce eTick; send this, eTick; }
    on eTick do {
      if ($ && $ && $ && $ && $ && $ && $ && $) { announce eDone; }
      send this, eTick;
    }
  }
}

machine Counter {
  start state Run {
    entry {
      var c: int;
      c = NextCounter();
      assert c % 2 == 1, format("counter hit {0}", c);
    }
  }
}

// Fails on every schedule with the counter in its bug, so the replay finds another bug.
machine CounterInBug {
  start state Run {
    entry {
      var c: int;
      c = NextCounter();
      assert c < 0, format("counter is {0}", c);
    }
  }
}

test tcAssert [main=Race]: { Race, Receiver, Sender };
test tcMaxSteps [main=Forever]: { Forever };
test tcHotAtEnd [main=Requests]: assert Responded in { Requests };
test tcTemperature [main=Ticker]: assert Progress in { Ticker };
test tcCounter [main=Counter]: { Counter };
test tcCounterInBug [main=CounterInBug]: { CounterInBug };
