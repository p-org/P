event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

machine Main {
  var Follower0: machine;
  var Follower1: machine;
  var Controller: machine;
  var count: int;

  start state Init {
    entry {
      Follower0 = new Follower();
      Follower1 = new Follower();
      Controller = new Controller((_follower0 = Follower0, _follower1 = Follower1));
      debug();
      assert(count == 4), format ("count = {0}", count);
    }
  }

  fun debug() {
    var allFollowers: set[machine];
    var follower: machine;

    allFollowers += (Follower0);
    allFollowers += (Follower1);

    foreach(follower in allFollowers) {
        print format ("Follower {0}", follower);
        count = count + 1;
    }

    foreach(follower in allFollowers) {
        print format ("(repeat) Follower {0}", follower);
        count = count + 1;
    }
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
