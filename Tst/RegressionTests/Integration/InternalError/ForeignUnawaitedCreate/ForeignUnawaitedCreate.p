// Run by Tst/UnitTests/ForeignCodeMisuseTest.cs, not by the regression suite.
// A local foreign function calls the protected CreateStateMachineAsync, which takes the Create
// scheduling point, and drops its awaitable instead of awaiting it. When the scheduler switches
// to Other there, the foreign code keeps running outside of the scheduler: its next call into the
// runtime must stop the checker with an internal error.
event ePing;

machine Main {
  var r: Receiver;
  start state Init {
    entry {
      r = new Receiver();
      new Other();
      CreateWithoutAwaiting(r);
      // Puts Worker in the creates set and ePing in the sends set of Main, for the foreign code.
      new Worker();
      send r, ePing;
    }
  }

  fun CreateWithoutAwaiting(r: Receiver);
}

machine Other {
  start state Init {
    entry {
    }
  }
}

machine Worker {
  start state Init {
    entry {
    }
  }
}

machine Receiver {
  start state Init {
    ignore ePing;
  }
}

test DefaultImpl [main=Main]: { Main, Other, Worker, Receiver };
