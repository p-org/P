event ping;
event pong;

machine Main {
  var follower1: machine;
  var follower2: machine;

  start state Init {
    entry {
      follower1 = new Follower(this);
      follower2 = new Follower(this);
      goto Send;
    }
  }

  state Send {
    entry {
      send follower1, ping;
      send follower2, ping;
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
  var leader: machine;

  start state Init {
    entry(creator: machine) {
      leader = creator;
    }

    on ping do {
      send leader, pong;
    }
  }
}
