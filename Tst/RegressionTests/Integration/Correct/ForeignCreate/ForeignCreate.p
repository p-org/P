// Foreign code creates a machine through the machine it is called on
// (StateMachine.CreateInterface) and returns it to P code, which then talks to it.
event ePing: Main;
event ePong: int;

fun ForeignCreate(v: int): Worker;

machine Main {
  var w1: Worker;
  var w2: Worker;
  var sum: int;
  start state Init {
    entry {
      w1 = new Worker(1);
      w2 = ForeignCreate(2);
      send w1, ePing, this;
      send w2, ePing, this;
    }
    on ePong do (v: int) {
      sum = sum + v;
      if (sum == 3) {
        goto Done;
      }
      assert sum == 1 || sum == 2, format("unexpected sum {0}", sum);
    }
  }
  state Done {
    ignore ePong;
  }
}

machine Worker {
  var id: int;
  start state Init {
    entry (v: int) {
      id = v;
    }
    on ePing do (m: Main) {
      send m, ePong, id;
    }
  }
}
