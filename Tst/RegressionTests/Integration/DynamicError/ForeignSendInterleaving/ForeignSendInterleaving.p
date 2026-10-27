// A send performed by foreign code (StateMachine.SendEvent) is a scheduling point, like a P send:
// other machines can run right before its event is enqueued. Here the bug needs Other to send
// eB right after it is created, before the first (foreign) eA reaches Server. Without the
// scheduling point of the foreign send, nothing could run between the creation of Other and
// the enqueue of eA(1), and the checker would miss the bug.
event eA: int;
event eB: int;

fun ForeignSend(target: machine, v: int);

machine Main {
  var s: machine;
  start state Init {
    entry {
      s = new Server();
      new Other(s);
      ForeignSend(s, 1);
      send s, eA, 2;
      ForeignSend(s, 3);
    }
  }
}

machine Other {
  start state Init {
    entry (s: machine) {
      send s, eB, 10;
    }
  }
}

machine Server {
  var first: bool;
  start state Init {
    on eA do (x: int) { first = true; }
    on eB do (x: int) {
      assert first, "eB arrived before the first (foreign) eA";
    }
  }
}
