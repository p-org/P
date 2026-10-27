// Run by Tst/UnitTests/ForeignCodeMisuseTest.cs, not by the regression suite.
// Foreign code saves the machine it is called on, and later sends through it while another
// machine is executing. This misuse of the runtime must stop the checker with an internal
// error before the send is announced: the NoPing monitor must never observe it.
event ePing;

fun SaveMachine();
fun SendThroughSavedMachine(target: Receiver);

machine Main {
  var r: Receiver;
  start state Init {
    entry {
      r = new Receiver();
      SaveMachine();
      new Other(r);
    }
  }
}

machine Other {
  start state Init {
    entry (r: Receiver) {
      SendThroughSavedMachine(r);
    }
  }
}

machine Receiver {
  start state Init {
    ignore ePing;
  }
}

spec NoPing observes ePing {
  start state Init {
    on ePing do {
      assert false, "the monitor observed a send that never happened";
    }
  }
}

test DefaultImpl [main=Main]: assert NoPing in { Main, Other, Receiver };
