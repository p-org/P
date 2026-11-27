// Foreign code sends events through the machine it is called on (StateMachine.SendEvent).
// The sends must be delivered in order with the sends of the P code around them, and must
// not corrupt the scheduler: other machines keep running while the sender is suspended at
// its own (P) scheduling points.
event ePing: int;
event eDone: int;
event eNoise;

fun ForeignSend(target: Receiver, v: int);

machine Main {
  var r: Receiver;
  var o1: Other;
  var o2: Other;
  var i: int;
  start state Init {
    entry {
      r = new Receiver();
      o1 = new Other();
      o2 = new Other();
      i = 0;
      while (i < 5) {
        ForeignSend(r, i);
        send o1, eNoise;
        i = i + 1;
      }
      send o2, eNoise;
      send r, eDone, 5;
    }
  }
}

machine Receiver {
  var count: int;
  start state Init {
    on ePing do (v: int) {
      assert v == count, format("expected ping {0}, got ping {1}", count, v);
      count = count + 1;
    }
    on eDone do (n: int) {
      assert count == n, format("expected {0} pings before eDone, got {1}", n, count);
    }
  }
}

machine Other {
  start state Init {
    ignore eNoise;
  }
}
