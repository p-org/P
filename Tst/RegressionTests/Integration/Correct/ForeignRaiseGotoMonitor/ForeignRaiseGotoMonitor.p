// A raise or goto performed inside foreign code called from a spec monitor ends the monitor
// handler that called it.
event eStart;
event eMonStep;

fun MonitorForeignRaise();
fun MonitorForeignRaiseInt(): int;

machine Main {
  start state Init {
    entry {
      announce eStart;
      announce eStart;
    }
  }
}

spec Watcher observes eStart {
  var x: int;
  start state Init {
    on eStart do {
      MonitorForeignRaise();
      assert false, "monitor continued after a foreign raise";
    }
    on eMonStep goto Second;
  }

  state Second {
    on eStart do {
      x = MonitorForeignRaiseInt();
      assert false, "monitor continued after a foreign raise in an assignment";
    }
    on eMonStep goto Done;
  }

  state Done {
    entry {
      assert x == 0, "the value of a call that raised was assigned";
    }
    ignore eStart;
  }
}

test DefaultImpl [main=Main]: assert Watcher in { Main };
