// A handler that only announces (compiled as a synchronous method) violates the monitor's
// assertion; the checker must report it.
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
  start state Init {
    entry { send this, eGo; }
    on eGo do AnnounceTwice;
  }

  fun AnnounceTwice() {
    announce eTick, 1;
    AnnounceAgain();
  }

  fun AnnounceAgain() { announce eTick, 1; }
}

test DefaultImpl [main=Main]: assert Increasing in { Main };
