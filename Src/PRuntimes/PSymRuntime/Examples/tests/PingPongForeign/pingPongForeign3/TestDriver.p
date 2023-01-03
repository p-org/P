event ping0: machine;
event ping1: machine;
event pong0;
event pong1;

machine Main {
  var Follower0: machine;
  var Follower1: machine;
  var Controller: machine;

  start state Init {
    entry {
      Follower0 = new Follower();
      Follower1 = new Follower();
      Controller = new Controller((_follower0 = Follower0, _follower1 = Follower1));
    }
  }
}
