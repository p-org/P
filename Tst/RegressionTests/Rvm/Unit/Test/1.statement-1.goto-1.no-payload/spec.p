event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Failure {
    on event1 do { goto Success; }
    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
