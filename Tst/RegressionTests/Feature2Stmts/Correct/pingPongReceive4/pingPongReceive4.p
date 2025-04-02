event ping: (sender: machine, id: int);
event pong: (id: int);

machine Main {
  var follower: machine;

  start state Init {
    entry {
      var id: int;
      follower = new Follower();
      id = 0;

      while(id < 2) {
        send follower, ping, (sender=this, id=id);
        print format ("(inside) {0}: ping sent", id);
        receive {
            case pong: (rsp: (id: int)) {
                id = rsp.id;
                print format ("(inside) {0}: pong received", id);
           }
        }
        print format ("(inside) {0}: done", id);
      }
      assert (id == 2), format("expected id to be 2, got {0}", id);

      send follower, ping, (sender=this, id=id);
      print format ("(outside) {0}: ping sent", id);
      receive {
        case pong: (rsp: (id: int)) {
            id = rsp.id;
            print format ("(outside) {0}: pong received", id);
          }
      }

      assert (id == 3), format("expected id to be 3, got {0}", id);
    }
  }
}

machine Follower {
  start state Init {
    on ping do (payload: (sender: machine, id: int)) {
      send payload.sender, pong, (id = payload.id+1,);
    }
  }
}
