event ping: (sender: machine, id: int);
event pong: (id: int);

machine Main {
  var follower: machine;

  start state Init {
    entry {
      var id: int;
      follower = new Follower();
      id = 0;

      if (id == 0) {
        send follower, ping, (sender=this, id=id);
        print format ("(first) {0}: ping sent", id);
        receive {
          case pong: (rsp: (id: int)) {
              id = rsp.id*2;
              print format ("(first) {0}: pong received", id);
         }
        }
      }
      assert (id == 2), format("expected id to be 2, got {0}", id);

      while(id < 4) {
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
      assert (id == 4), format("expected id to be 4, got {0}", id);

      send follower, ping, (sender=this, id=id);
      print format ("(outside) {0}: ping sent", id);
      receive {
        case pong: (rsp: (id: int)) {
            id = rsp.id;
            print format ("(outside) {0}: pong received", id);
          }
      }

      assert (id != 5), format("expected id to be 5, got {0}", id); // error
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
