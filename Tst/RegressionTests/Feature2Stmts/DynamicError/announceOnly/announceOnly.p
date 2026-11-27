// A handler that only announces (compiled as a synchronous method) fails after announcing; the
// checker must report it. Tst/UnitTests/AnnounceOnlyFailureTest.cs also runs this program, to
// check that the failure reaches OnFailure before the stack of the handler unwinds, which only a
// synchronous handler does.
event eTick: int;
event eGo;

spec Increasing observes eTick {
  var last: int;
  start state Watching {
    on eTick do (n: int) {
      assert n > last, format("announced {0} after {1}", n, last);
      last = n;
    }
  }
}

machine Main {
  var ticks: seq[int];
  start state Init {
    entry { send this, eGo; }
    on eGo do AnnounceTwice;
  }

  fun AnnounceTwice() {
    announce eTick, 1;
    AnnounceAgain();
  }

  // Fails indexing the empty sequence.
  fun AnnounceAgain() { announce eTick, ticks[0]; }
}

test DefaultImpl [main=Main]: assert Increasing in { Main };
