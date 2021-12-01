event ping: machine;
event pong;

machine Main {
  var follower1: machine;
  var follower2: machine;

  start state Init {
    entry {
      follower1 = new Follower();
      follower2 = new Follower();
      goto Send;
    }
  }

  state Send {
    entry {
      send follower1, ping, this;
      send follower2, ping, this;
      goto WaitPong1;
    }
  }

  state WaitPong1 {
    on pong goto WaitPong2;
  }

  state WaitPong2 {
    on pong goto Done;
  }

  state Done {}
}

machine Follower {
  start state Init {
    on ping do (sender: machine) {
      send sender, pong;
    }
  }
}
