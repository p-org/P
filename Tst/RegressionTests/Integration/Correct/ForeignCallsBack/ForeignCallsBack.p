// Foreign code calls back into P functions that send and create. These functions are async in
// the generated code, but while foreign code runs they cannot be suspended: their sends and
// creates take their scheduling points once the outermost foreign call returns, in order with
// the sends of the foreign code itself. Dropping (or blocking on) the task they return is safe.
event ePing: int;
event eDone: int;

fun ForeignCallsBack(r: Receiver, v: int): Receiver;

fun SendPing(r: Receiver, v: int) {
  send r, ePing, v;
}

fun CreateReceiver(): Receiver {
  var r: Receiver;
  r = new Receiver();
  return r;
}

machine Main {
  var r: Receiver;
  var created: Receiver;
  var i: int;
  start state Init {
    entry {
      r = new Receiver();
      new Other();
      while (i < 3) {
        // Sends 3 * i + 1 (callback), 3 * i + 2 (foreign) and 3 * i + 3 (callback).
        created = ForeignCallsBack(r, 3 * i);
        send created, eDone, 1;
        i = i + 1;
      }
      send r, eDone, 9;
    }
  }
}

machine Receiver {
  var count: int;
  start state Init {
    on ePing do (v: int) {
      count = count + 1;
      assert v == count, format("expected ping {0}, got ping {1}", count, v);
    }
    on eDone do (n: int) {
      assert count == n, format("expected {0} pings before eDone, got {1}", n, count);
    }
  }
}

machine Other {
  start state Init {
    entry {
      var j: int;
      while (j < 3) {
        j = j + 1;
      }
    }
  }
}
