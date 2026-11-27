// The foreign function drops the awaitable returned by StateMachine.SendEventAsync instead of
// calling StateMachine.SendEvent. While foreign code runs, SendEventAsync is not a scheduling
// point either: it defers the send like SendEvent, so no event is lost and the scheduler is not
// corrupted.
event ePing;
event eDone: int;

fun ForeignSendUnawaited(target: Receiver);

machine Main {
  var r: Receiver;
  var i: int;
  start state Init {
    entry {
      r = new Receiver();
      i = 0;
      while (i < 5) {
        ForeignSendUnawaited(r);
        i = i + 1;
      }
      send r, eDone, 5;
    }
  }
}

machine Receiver {
  var count: int;
  start state Init {
    on ePing do {
      count = count + 1;
    }
    on eDone do (n: int) {
      assert count == n, format("expected {0} pings before eDone, got {1}", n, count);
    }
  }
}
