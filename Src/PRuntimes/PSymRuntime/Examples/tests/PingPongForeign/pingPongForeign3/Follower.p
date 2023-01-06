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
