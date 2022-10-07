event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

machine Main {
  var Follower0: machine;
  var Follower1: machine;

  start state Init {
    entry {
      var id: int;
      Follower0 = new Follower();
      Follower1 = new Follower();
      id = 0;
      while(id < 3) {
        send Follower1, ping0, this;
        print format ("{0}: ping0 sent", id);
        receive {
            case pong0: {
                print format ("{0}: pong0 received", id);
           }
        }
        print format ("{0}: done", id);
        id = id + 1;
      }
    }
  }
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
