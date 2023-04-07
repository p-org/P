event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

/* foreign types */
type CustomInt;

/* foreign functions */
fun printInt(i: int);
fun changeInt(i: int): int;
fun convertToCustomInt(val: int): CustomInt;
fun convertFromCustomInt(val: CustomInt): int;
fun changeCustomInt(val: CustomInt): CustomInt;

machine Main {
  var Follower0: machine;
  var Follower1: machine;
  var Controller: machine;

  start state Init {
    entry {
      Follower0 = new Follower();
      Follower1 = new Follower();
      Controller = new Controller((_follower0 = Follower0, _follower1 = Follower1));
      debug();
    }
  }

  fun debug() {
    var i: int;
    var j: int;
    var k: int;

    i = choose(5);
    printInt(i);
    j = changeInt(i);
    k = convertFromCustomInt(changeCustomInt(convertToCustomInt(j)));
    print format("i={0}, j={1}, k={2}", i, j, k);
  }
}

machine Controller {
  var Follower0: machine;
  var Follower1: machine;

  start state Init {
    entry (payload: (_follower0: machine, _follower1: machine)) {
      Follower0 = payload._follower0;
      Follower1 = payload._follower1;
      goto Send;
    }
  }
  state Send {
    entry {
      send Follower0, ping0, this;
      send Follower1, ping1, this;
      goto WaitPong1;
    }
  }
  state WaitPong1 {
    on pong0 goto WaitPong2;
    on pong1 goto WaitPong2;
  }
  state WaitPong2 {
    on pong0 goto Done;
    on pong1 goto Done;
  }
  state Done {}
}

machine Follower {
  start state Init {
    on ping0 do (sender: machine) {
      send sender, pong0;
    }
    on ping1 do (sender: machine) {
      send sender, pong1;
    }
  }
}
