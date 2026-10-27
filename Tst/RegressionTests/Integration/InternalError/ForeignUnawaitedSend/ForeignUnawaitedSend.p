// Run by Tst/UnitTests/ForeignUnawaitedSchedulingPointTest.cs, not by the regression suite.
// The foreign function drops the awaitable returned by StateMachine.SendEventAsync instead of
// calling StateMachine.SendEvent. The checker must stop with an internal error instead of
// losing the event or reporting a bug in this (correct) program.
event ePing;
event eDone;

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
      send r, eDone;
    }
  }
}

machine Receiver {
  start state Init {
    ignore ePing, eDone;
  }
}
