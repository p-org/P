event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

machine Main {
  var Follower0: machine;
  var Follower1: machine;
  var count: int;

  start state Init {
    entry {
      Follower0 = new Follower();
      Follower1 = new Follower();
      debug(1);
      debug(2);
      assert(count == 38), format ("count = {0}", count);
    }
  }

  fun debug(id: int) {
    send Follower0, ping1, this;
    print format ("{0}: ping1 sent", id);
    receive {
        case pong1: {
            count = (count + 1) * id;
            print format ("{0}: pong1 received", id);
            send Follower1, ping0, this;
            print format ("{0}: ping0 sent", id);
            receive {
                case pong0: {
                    count = (count + 1) * id;
                    print format ("{0}: pong0 received", id);
               }
            }
        }
    }
    count = (count + 1) * id;
    print format ("{0}: done", id);
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
