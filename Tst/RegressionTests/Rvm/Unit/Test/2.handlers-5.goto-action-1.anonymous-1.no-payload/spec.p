event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var j: int;

  start state Start {
    on event1 goto Middle with { j = 13; }
    on getState do {}
  }

  state Middle {
    entry {
      assert j == 13, "Expecting j to be set.";
      goto Success;
    }
    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
