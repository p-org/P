event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

type tFollower = (follower: machine, id: int);

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
    var followerName: map[tFollower, int];
    var tF: tFollower;
    var id: int;

    tF = (follower=Follower0, id=0);
    followerName += (tF, choose(5));
    print format ("pushing key1 {0} with value {1}", tF, followerName[tF]);

    tF = (follower=Follower1, id=1);
    followerName += (tF, 2);
    print format ("pushing key2 {0} with value {1}", tF, followerName[tF]);

    if (tF in followerName) {
        print format ("key {0} is present with value={1}", tF, followerName[tF]);
    } else {
        assert false, format ("key {0} is absent", tF);
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
