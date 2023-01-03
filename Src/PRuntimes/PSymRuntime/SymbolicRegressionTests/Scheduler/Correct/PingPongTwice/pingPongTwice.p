event ping: machine;
event pong;

machine Main {
  var follower1: machine;
  var follower2: machine;
  var count: int;

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
    on pong do {
      if (count == 1) {
        goto Done;
      } else {
        count = count + 1;
        goto Send;
      }
    }
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
