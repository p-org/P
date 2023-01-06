event ePing;
event ePong;
event eFollowerWake;

machine Leader {
  var follower1: machine;
  var follower2: machine;

  start state Init {
    entry {
      follower1 = new Follower(this);
      follower2 = new Follower(this);

      new Waker(follower1);
      new Waker(follower2);

      send follower1, ePing;
      send follower2, ePing;
    }

    on ePong goto Wait1;
  }

  state Wait1 {
    on ePong goto Wait2;
  }

  state Wait2 {
    on ePong goto Done;
  }

  state Done {}
}

machine Follower {
  var leader: machine;

  start state Init {
    entry (payload: machine) {
      leader = payload;
    }

    defer ePing;
    on eFollowerWake goto Send;
  }

  state Send {
    on ePing do {
      send leader, ePong;
    }
  }
}

machine Waker {
  start state Init {
    entry (target: machine) {
      send target, eFollowerWake;
    }
  }
}

machine Main {
  start state Init {
    entry {
      new Leader();
    }
  }
}
