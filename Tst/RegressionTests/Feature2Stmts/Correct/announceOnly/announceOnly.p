// Announcing is not a scheduling point, so functions that only announce are compiled as
// synchronous methods, while functions that also send (directly or through callees) are
// awaited. Monitors must observe every announcement in program order either way, so this
// program passes whether or not those functions are async: SchedulingPointCodegenTest and
// AnnounceOnlyFailureTest in Tst/UnitTests check that they are synchronous.
event eTick: int;
event eGo;
event eDone;

spec Order observes eTick, eDone {
  var expected: int;
  start state Watching {
    entry { expected = 1; }
    on eTick do (n: int) {
      assert n == expected, format("announced {0}, expected {1}", n, expected);
      expected = expected + 1;
    }
    on eDone do {
      assert expected == 6, format("{0} ticks announced, expected 5", expected - 1);
    }
  }
}

fun AnnounceTick(n: int) { announce eTick, n; }

machine Main {
  var helper: Helper;
  start state Init {
    entry {
      Tick(1);
      helper = new Helper(this);
      AnnounceTick(2);
      AnnounceThenSend(3);
    }
    on eGo do OnEcho;
  }

  fun Tick(n: int) { announce eTick, n; }
  fun CallsTick(n: int) { Tick(n); }
  fun AnnounceThenSend(n: int) { Tick(n); send helper, eGo; }

  // Only announces.
  fun OnEcho() {
    announce eTick, 4;
    CallsTick(5);
    announce eDone;
  }
}

machine Helper {
  var owner: Main;
  start state Init {
    entry (m: Main) { owner = m; }
    on eGo do { send owner, eGo; }
  }
}

test DefaultImpl [main=Main]: assert Order in { Main, Helper };
