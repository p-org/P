// Foreign code sends, announces, and calls back a P function that announces. Its sends are
// performed (and announced) when it returns to P code, so its announces are deferred as well:
// the InOrder spec must observe every event in program order, as if foreign code ran each of
// them right away.
event ePing: int;
event eStep: int;

fun ForeignSteps(r: Receiver);

fun AnnounceStep(v: int) {
  announce eStep, v;
}

machine Main {
  var r: Receiver;
  start state Init {
    entry {
      r = new Receiver();
      // Pings 1 and 4, steps 2 (callback) and 3 (foreign).
      ForeignSteps(r);
      announce eStep, 5;
      send r, ePing, 6;
    }
  }
}

machine Receiver {
  start state Init {
    ignore ePing;
  }
}

spec InOrder observes ePing, eStep {
  var last: int;
  start hot state Waiting {
    on ePing do (v: int) { Next(v); }
    on eStep do (v: int) { Next(v); }
  }
  cold state Done {
    ignore ePing, eStep;
  }
  fun Next(v: int) {
    assert v == last + 1, format("expected event {0}, observed event {1}", last + 1, v);
    last = v;
    if (last == 6) {
      goto Done;
    }
  }
}

test DefaultImpl [main=Main]: assert InOrder in { Main, Receiver };
