event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

machine Main {
  var Follower0: machine;
  var Follower1: machine;

  start state Init {
    entry {
      Follower0 = new Follower();
      Follower1 = new Follower();
      goto Send;
    }
  }

  state Send {
    entry {
      if ($) {
          send Follower0, ping0, this;
          send Follower1, ping1, this;
      } else {
          send Follower1, ping1, this;
          send Follower0, ping0, this;
      }
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
