// Run by Tst/UnitTests/ForeignCodeMisuseTest.cs, not by the regression suite.
// Foreign code calls back into a P function that receives. Foreign code cannot be suspended
// until an event arrives, so the checker must stop with an internal error instead of
// reporting a bug in the program.
event ePing;

fun ForeignCallsReceiver();

fun WaitForPing() {
  receive {
    case ePing: { }
  }
}

machine Main {
  start state Init {
    entry {
      send this, ePing;
      ForeignCallsReceiver();
    }
  }
}
