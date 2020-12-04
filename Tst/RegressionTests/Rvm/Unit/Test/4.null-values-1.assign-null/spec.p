event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Start {
    on event1 do {
      var b: bool;
      var p:(a: int, b: int);
      b = null as bool;
      p = null as (a:int, b:int);
      goto Success;
    }
    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
